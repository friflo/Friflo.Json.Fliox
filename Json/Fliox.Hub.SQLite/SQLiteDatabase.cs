// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLITE

using System;
using System.IO;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using SQLitePCL;

namespace Friflo.Json.Fliox.Hub.SQLite
{
    public sealed class SQLiteDatabase : EntityDatabase, ISQLDatabase
    {
        public              bool        Pretty      { get; init; } = false;
        public              TableType   TableType   { get; init; } = TableType.Relational;
        
        /// <summary>Set the execution mode of SQLite database commands.<br/>
        /// If true database commands are executed synchronously otherwise asynchronously.<br/>
        /// <br/>
        /// The use cases of sync vs async execution are explained in detail at:<br/>
        /// <a href="https://devblogs.microsoft.com/pfxteam/should-i-expose-asynchronous-wrappers-for-synchronous-methods/">
        /// Should I expose asynchronous wrappers for synchronous methods? - .NET Parallel Programming</a>
        /// </summary>
        public              bool            Synchronous             { get; init; }
        
        /// <summary>
        /// could use a single SyncConnection - an sqlite3 handle - for all database operation
        /// but read throughput is significant higher if using a handle per thread 
        /// </summary>
        private  readonly   ConnectionPool<SyncConnection> connectionPool; 
        private  readonly   object          writeLock = new object();
        private  readonly   string          filePath;
        
        public   override   string          StorageType             => "SQLite - " + TableType;
        
        /// <summary>
        /// Open or create a database with the given <paramref name="connectionString"/>.<br/>
        /// Create an Im-Memory <paramref name="connectionString"/> with <c>"Data Source=:memory:"</c><br/>
        /// See: <a href="https://www.sqlite.org/inmemorydb.html">SQLite - In-Memory Databases</a>
        /// </summary>
        /// <returns></returns>
        public SQLiteDatabase(string dbName, string connectionString, DatabaseSchema schema, DatabaseService service = null)
            : base(dbName, AssertSchema<SQLiteDatabase>(schema), service)
        {
            var builder     = new SQLiteConnectionStringBuilder(connectionString);
            connectionPool  = new ConnectionPool<SyncConnection>();
            filePath        = builder.DataSource;
        }

        public override void Dispose() {
            base.Dispose();
            connectionPool.ClearAll();
        }

        public override EntityContainer CreateContainer(in ShortString name, EntityDatabase database) {
            return new SQLiteContainer(name.AsString(), this, Pretty);
        }
        
        public override bool IsSyncTask(SyncRequestTask task, in PreExecute execute) {
            if (!execute.executeSync) {
                return false;
            }
            switch (task.TaskType) {
                case TaskType.read:
                case TaskType.query:
                case TaskType.create:
                case TaskType.upsert:
                case TaskType.delete:
                case TaskType.aggregate:
                case TaskType.closeCursors:
                    return true;
            }
            return false;
        }
        
        protected override  Task<ISyncConnection>   GetConnectionAsync() {
            var result = GetConnectionSync();
            return Task.FromResult(result);
        }

        protected override ISyncConnection GetConnectionSync() {
            if (connectionPool.TryPop(out var syncConnection)) {
                return syncConnection;
            }
            const int flags = raw.SQLITE_OPEN_READWRITE;
            var rc = raw.sqlite3_open_v2(filePath, out sqlite3 sqliteDB, flags, null);
            if (rc == raw.SQLITE_CANTOPEN) {
                return SyncConnectionError.DatabaseDoesNotExist(name);
            }
            if (rc != raw.SQLITE_OK) {
                var msg = SQLiteUtils.GetErrorMsg("open failed.", sqliteDB, rc);
                return new SyncConnectionError(msg);
            }
            return new SyncConnection(sqliteDB, writeLock);
        }
        
        protected  override void ReturnConnection(ISyncConnection syncConnection) {
            connectionPool.Push(syncConnection);
        }

        static SQLiteDatabase() {
            raw.SetProvider(new SQLite3Provider_e_sqlite3());
        }
        
        protected override Task<TransResult> TransactionAsync(SyncContext syncContext, TransCommand command) {
            var result = Transaction(syncContext, command);
            return Task.FromResult(result);
        }
        
        protected override TransResult Transaction(SyncContext syncContext, TransCommand command)
        {
            var syncConnection = syncContext.GetConnectionSync();
            if (syncConnection is not SyncConnection connection) {
                return new TransResult(syncConnection.Error.message);
            }
            switch (command) {
                case TransCommand.Begin: {
                    connection.BeginTransaction(out var error);
                    if (error != null) {
                        return new TransResult(error.message);
                    }
                    return new TransResult(command);
                }
                case TransCommand.Commit: {
                    if (!connection.EndTransaction("COMMIT", out var error)) {
                        return new TransResult(error.message);
                    }
                    return new TransResult(command);
                }
                case TransCommand.Rollback: {
                    if (!connection.EndTransaction("ROLLBACK", out var error)) {
                        return new TransResult(error.message);
                    }
                    return new TransResult(command);
                }
                default:
                    return new TransResult($"invalid transaction command {command}");
            }
        }
        
        protected override Task CreateDatabaseAsync() {
            const int flags = raw.SQLITE_OPEN_READWRITE | raw.SQLITE_OPEN_CREATE;
            var rc = raw.sqlite3_open_v2(filePath, out sqlite3 sqliteDB, flags, null);
            if (rc != raw.SQLITE_OK) {
                var msg = SQLiteUtils.GetErrorMsg("create / open database failed.", sqliteDB, rc);
                throw new InvalidOperationException(msg);
            }
            var connection = new SyncConnection(sqliteDB, writeLock);
            if (!SQLiteUtils.Exec(connection, "PRAGMA journal_mode = WAL;", out var error)) {
                throw new InvalidOperationException($"PRAGMA journal_mode = WAL failed. error: {error}");
            }
            connection.Dispose();
            return Task.CompletedTask;
        }
        
        public override Task DropDatabaseAsync() {
            File.Delete(filePath);
            return Task.CompletedTask;
        }

        protected override Task DropContainerAsync(ISyncConnection syncConnection, string name) {
            if (syncConnection is not SyncConnection connection) {
                throw new InvalidOperationException(syncConnection.Error.message); 
            }
            var sql = $"DROP TABLE IF EXISTS `{name}`;";
            var result = SQLiteUtils.Execute(connection, sql);
            if (result.error != null) {
                throw new InvalidOperationException(result.error);
            }
            return Task.CompletedTask;
        }
        
        public Task CreateFunctions(ISyncConnection connection) => Task.CompletedTask;
        
        
        public override Result<RawSqlResult> ExecuteRawSQL(RawSql sql, SyncContext syncContext) {
            var syncConnection = syncContext.GetConnectionSync();
            if (syncConnection is not SyncConnection connection) {
                return Result.Error(syncConnection.Error.message);
            }
            using var stmt = SQLiteUtils.Prepare(connection, sql.command, out var error);
            if (error != null) {
                return Result.Error(error.message);
            }
            try {
                var result = SQLiteUtils.GetRawSqlResult(connection, stmt.instance, out var errorMsg);
                if (errorMsg != null) {
                    return Result.Error(errorMsg); 
                }
                return result;
            }
            catch (Exception e) {
                return Result.Error(e.Message);
            }
        }
        
        public override Task<Result<RawSqlResult>> ExecuteRawSQLAsync(RawSql sql, SyncContext syncContext) {
            var result = ExecuteRawSQL(sql, syncContext);
            return Task.FromResult(result);
        }
    }

    
    internal sealed class SQLiteQueryEnumerator : QueryEnumerator
    {
        internal readonly   sqlite3_stmt    stmt;
        internal readonly   string          sql;
        
        public   override   JsonKey         Current     => throw new NotImplementedException("not applicable");
        public   override   bool            MoveNext()  => throw new NotImplementedException("not applicable");
        
        internal SQLiteQueryEnumerator(sqlite3_stmt stmt, string sql) {
            this.stmt   = stmt;
            this.sql    = sql;
        }
        
        protected override void DisposeEnumerator() {
            raw.sqlite3_finalize(stmt);
        }
    }
}

namespace System.Runtime.CompilerServices
{
    // This is needed to enable following features in .NET framework and .NET core <= 3.1 projects:
    // - init only setter properties. See [Init only setters - C# 9.0 draft specifications | Microsoft Learn] https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/init
    // - record types
    internal static class IsExternalInit { }
}

#endif

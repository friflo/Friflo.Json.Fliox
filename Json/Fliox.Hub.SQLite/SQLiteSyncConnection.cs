// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLITE

using System;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using SQLitePCL;

namespace Friflo.Json.Fliox.Hub.SQLite
{
    internal sealed class SyncConnection : ISyncConnection
    {
        internal readonly   sqlite3 sqliteDB;
        internal readonly   object  writeLock;
        private             int     transactionDepth;
        
        public  TaskExecuteError    Error       => throw new InvalidOperationException();
        public  bool                IsOpen      => true;
        public  void                ClearPool() { }
        
        internal SyncConnection(sqlite3 sqliteDB, object writeLock) {
            this.sqliteDB   = sqliteDB;
            this.writeLock  = writeLock;
        }

        public void Dispose() {
            if (sqliteDB.IsClosed) {
                return;
            }
            var rc = raw.sqlite3_close_v2(sqliteDB);
            if (rc !=  raw.SQLITE_OK) {
                var msg = SQLiteUtils.GetErrorMsg("close failed.", sqliteDB, rc);
                Console.Error.WriteLine(msg);
            }
        }

        /// <summary>
        /// BEGIN TRANSACTION.<br/>
        /// Need to be called in a using statement to COMMIT TRANSACTION by calling
        /// <see cref="TransactionScope.Dispose"/> when leaving the scope.
        /// </summary>
        internal TransactionScope BeginTransaction(out TaskExecuteError error) {
            transactionDepth++;
            if (transactionDepth == 1) {
                SQLiteUtils.Exec(this, "BEGIN TRANSACTION", out error);
            } else {
                error = null;
            }
            return new TransactionScope(this);
        }
        
        internal bool EndTransaction(string sql, out TaskExecuteError error) {
            if (transactionDepth <= 0) throw new InvalidOperationException("expect transactionDepth > 0");
            transactionDepth--;
            if (transactionDepth == 0) {
                return SQLiteUtils.Exec(this, sql, out error);
            }
            error = null;
            return true;
        }
    }
    
    internal struct TransactionScope : IDisposable
    {
        private SyncConnection connection;
            
        internal TransactionScope(SyncConnection connection) {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }
        
        internal bool EndTransaction(string sql, out TaskExecuteError error) {
            var c = connection;
            connection = null;
            return c.EndTransaction(sql, out error);
        } 

        public void Dispose() {
            if (connection == null) {
                return;
            }
            connection.EndTransaction("COMMIT TRANSACTION", out _);
            connection = null;
        }
    }
}

#endif
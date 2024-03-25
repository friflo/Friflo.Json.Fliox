// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLSERVER

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    public abstract partial class SyncDbConnection : ISyncConnection
    {
        private  readonly   DbConnection                    instance;
        
        /// <summary>
        /// Each prepared statements is bound to a specific connection<br/>
        /// See: java - Why are prepared statements kept at a connection level by the JDBC drivers? - Stack Overflow
        /// https://stackoverflow.com/questions/30034594/why-are-prepared-statements-kept-at-a-connection-level-by-the-jdbc-drivers
        /// </summary>
        private  readonly   Dictionary<string, DbCommand>   preparedReadOne     = new ();
        private  readonly   Dictionary<string, DbCommand>   preparedReadMany    = new ();
        
        public  TaskExecuteError    Error       => throw new InvalidOperationException();
        public  void                Dispose()   => instance.Dispose();
        public  bool                IsOpen      => instance.State == ConnectionState.Open;
        public  abstract void       ClearPool();
        
        protected virtual   DbCommand   ReadRelational (TableInfo tableInfo, ReadEntities read) => throw new NotImplementedException();
        protected virtual   DbCommand   PrepareReadOne (TableInfo tableInfo)                    => throw new NotImplementedException();
        protected virtual   DbCommand   PrepareReadMany(TableInfo tableInfo)                    => throw new NotImplementedException();
        
        protected SyncDbConnection (DbConnection instance) {
            this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }
        
        private static void SetParameter(DbParameter idParam, ColumnType type, in JsonKey id)
        {
            switch (type) {
                case ColumnType.Uint8:  idParam.Value = (byte)  id.AsLong();    break;
                case ColumnType.Int16:  idParam.Value = (short) id.AsLong();    break;
                case ColumnType.Int32:  idParam.Value = (int)   id.AsLong();    break;
                case ColumnType.Int64:  idParam.Value =         id.AsLong();    break;
                case ColumnType.String: idParam.Value =         id.AsString();  break;
                case ColumnType.Guid:   idParam.Value =         id.AsGuid();    break;
                default: throw new InvalidOperationException($"invalid type: {type}");
            }
        }
        
        // --------------------------------------- sync / async  --------------------------------------- 
        /// <summary>async version of <see cref="ExecuteNonQuerySync"/></summary>
        public async Task ExecuteNonQueryAsync (string sql, DbParameter parameter = null) {
            using var command = instance.CreateCommand();
            command.CommandText = sql;
            if (parameter != null) {
                command.Parameters.Add(parameter);
            }
            int tryCount = 0;
            while (true) {
                tryCount++;
                try {
                    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                    return;
                }
                catch (DbException) {
                    if (instance.State != ConnectionState.Open && tryCount == 1) {
                        await instance.OpenAsync().ConfigureAwait(false);
                        continue;
                    }
                    throw;
                }
            }
        }
        
        /// <summary>Counterpart of <see cref="ExecuteSync"/></summary>
        public async Task<SQLResult> ExecuteAsync(string sql) {
            using var command = instance.CreateCommand();
            command.CommandText = sql;
            try {
                using var reader = await ExecuteReaderAsync(sql).ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false)) {
                    var value = reader.GetValue(0);
                    return SQLResult.Success(value); 
                }
                return default;
            }
            catch (DbException e) {
                return SQLResult.CreateError(e);
            }
        }
        
        /// <summary>
        /// Using asynchronous execution for SQL Server is significant slower.<br/>
        /// <see cref="DbCommand.ExecuteReaderAsync()"/> ~7x slower than <see cref="DbCommand.ExecuteReader()"/>.
        /// <summary>Counterpart of <see cref="ExecuteReaderSync"/></summary>
        /// </summary>
        public async Task<DbDataReader> ExecuteReaderAsync(string sql, DbParameter parameter = null) {
            using var command = instance.CreateCommand();
            command.CommandText = sql;
            if (parameter != null) {
                command.Parameters.Add(parameter);
            }
            int tryCount = 0;
            while (true) {
                tryCount++;
                try {
                    return await command.ExecuteReaderAsync().ConfigureAwait(false);
                }
                catch (DbException) {
                    if (instance.State != ConnectionState.Open && tryCount == 1) {
                        await instance.OpenAsync().ConfigureAwait(false);
                        continue;
                    }
                    throw;
                }
            }
        }
        
        /// <summary>Counterpart of <see cref="ExecuteReaderCommandSync"/></summary>
        private async Task<DbDataReader> ExecuteReaderCommandAsync(DbCommand command) {
            int tryCount = 0;
            while (true) {
                tryCount++;
                try {
                    // TODO check performance hit caused by many SqlBuffer instances
                    // [Reading large data (binary, text) asynchronously is extremely slow · Issue #593 · dotnet/SqlClient]
                    // https://github.com/dotnet/SqlClient/issues/593#issuecomment-1645441459
                    return await command.ExecuteReaderAsync().ConfigureAwait(false); // CommandBehavior.SingleResult | CommandBehavior.SingleRow | CommandBehavior.SequentialAccess);
                }
                catch (DbException) {
                    if (instance.State != ConnectionState.Open && tryCount == 1) {
                        await instance.OpenAsync().ConfigureAwait(false);
                        continue;
                    }
                    throw;
                }
            }
        }
        
        /// <summary>Counterpart of <see cref="Prepare"/></summary>
        private async Task PrepareAsync(DbCommand command) {
            int tryCount = 0;
            while (true) {
                tryCount++;
                try {
#if NETSTANDARD2_0
                    // ReSharper disable once MethodHasAsyncOverload
                    command.Prepare();
#else
                    await command.PrepareAsync().ConfigureAwait(false);
#endif
                    return;
                }
                catch (DbException) {
                    if (instance.State != ConnectionState.Open && tryCount == 1) {
                        await instance.OpenAsync().ConfigureAwait(false);
                        continue;
                    }
                    throw;
                }
            }
        }
        
        /// <summary>counterpart of <see cref="ReadRelationalReader"/></summary>
        public async Task<DbDataReader> ReadRelationalReaderAsync(TableInfo tableInfo, ReadEntities read, SyncContext syncContext)
        {
            if (read.typeMapper == null) {
                using var command = ReadRelational(tableInfo, read);
                return await ExecuteReaderCommandAsync(command).ConfigureAwait(false);
            }
            if (read.ids.Count == 1) {
                if (!preparedReadOne.TryGetValue(tableInfo.container, out var readOne)) {
                    readOne = PrepareReadOne(tableInfo);
                    await PrepareAsync(readOne).ConfigureAwait(false);
                    preparedReadOne.Add(tableInfo.container, readOne);
                }
                DbParameter idParam = readOne.Parameters[0];
                SetParameter(idParam, tableInfo.keyColumn.type, read.ids[0]);
                return await ExecuteReaderCommandAsync(readOne).ConfigureAwait(false);
            }
            if (!preparedReadMany.TryGetValue(tableInfo.container, out var readMany)) {
                readMany = PrepareReadMany(tableInfo);
                await PrepareAsync(readMany).ConfigureAwait(false);
                preparedReadMany.Add(tableInfo.container, readMany);
            }
            using var pooledMapper = syncContext.ObjectMapper.Get();
            readMany.Parameters[0].Value = pooledMapper.instance.writer.Write(read.ids);
            return await ExecuteReaderCommandAsync(readMany).ConfigureAwait(false);
        }
    }
}

#endif
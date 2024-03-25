// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLSERVER

using System;
using System.Data;
using System.Data.Common;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    public partial class SyncDbConnection
    {
        // --------------------------------------- sync / async  ---------------------------------------
        /// <summary>sync version of <see cref="ExecuteNonQueryAsync"/></summary>
        public void ExecuteNonQuerySync (string sql, DbParameter parameter = null) {
            using var command = instance.CreateCommand();
            command.CommandText = sql;
            if (parameter != null) {
                command.Parameters.Add(parameter);
            }
            int tryCount = 0;
            while (true) {
                tryCount++;
                try {
                    command.ExecuteNonQuery();
                    return;
                }
                catch (DbException) {
                    if (instance.State != ConnectionState.Open && tryCount == 1) {
                        instance.Open();
                        continue;
                    }
                    throw;
                }
            }
        }
        
        /// <summary>Counterpart of <see cref="ExecuteAsync"/></summary>
        public SQLResult ExecuteSync(string sql) {
            using var command = instance.CreateCommand();
            command.CommandText = sql;
            try {
                using var reader = ExecuteReaderSync(sql);
                while (reader.Read()) {
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
        /// <summary>Counterpart of <see cref="ExecuteReaderAsync"/></summary>
        /// </summary>
        public DbDataReader ExecuteReaderSync(string sql, DbParameter parameter = null) {
            using var command = instance.CreateCommand();
            command.CommandText = sql;
            if (parameter != null) {
                command.Parameters.Add(parameter);
            }
            int tryCount = 0;
            while (true) {
                tryCount++;
                try {
                    return command.ExecuteReader();
                }
                catch (DbException) {
                    if (instance.State != ConnectionState.Open && tryCount == 1) {
                        instance.Open();
                        continue;
                    }
                    throw;
                }
            }
        }
        
        /// <summary>Counterpart of <see cref="ExecuteReaderCommandAsync"/></summary>
        private DbDataReader ExecuteReaderCommandSync(DbCommand command) {
            int tryCount = 0;
            while (true) {
                tryCount++;
                try {
                    // TODO check performance hit caused by many SqlBuffer instances
                    // [Reading large data (binary, text) asynchronously is extremely slow · Issue #593 · dotnet/SqlClient]
                    // https://github.com/dotnet/SqlClient/issues/593#issuecomment-1645441459
                    return command.ExecuteReader(); // CommandBehavior.SingleResult | CommandBehavior.SingleRow | CommandBehavior.SequentialAccess);
                }
                catch (DbException) {
                    if (instance.State != ConnectionState.Open && tryCount == 1) {
                        instance.Open();
                        continue;
                    }
                    throw;
                }
            }
        }
        
        /// <summary>Counterpart of <see cref="PrepareAsync"/></summary>
        private void Prepare(DbCommand command) {
            int tryCount = 0;
            while (true) {
                tryCount++;
                try {
                    command.Prepare();
                    return;
                }
                catch (DbException) {
                    if (instance.State != ConnectionState.Open && tryCount == 1) {
                        instance.Open();
                        continue;
                    }
                    throw;
                }
            }
        }
        
        /// <summary>counterpart of <see cref="ReadRelationalReaderAsync"/></summary>
        public DbDataReader ReadRelationalReader(TableInfo tableInfo, ReadEntities read, SyncContext syncContext)
        {
            if (read.typeMapper == null) {
                using var command = ReadRelational(tableInfo, read);
                return ExecuteReaderCommandSync(command);
            }
            if (read.ids.Count == 1) {
                if (!preparedReadOne.TryGetValue(tableInfo.container, out var readOne)) {
                    readOne = PrepareReadOne(tableInfo);
                    Prepare(readOne);
                    preparedReadOne.Add(tableInfo.container, readOne);
                }
                DbParameter idParam = readOne.Parameters[0];
                SetParameter(idParam, tableInfo.keyColumn.type, read.ids[0]);
                return ExecuteReaderCommandSync(readOne);
            }
            if (!preparedReadMany.TryGetValue(tableInfo.container, out var readMany)) {
                readMany = PrepareReadMany(tableInfo);
                Prepare(readMany);
                preparedReadMany.Add(tableInfo.container, readMany);
            }
            using var pooledMapper = syncContext.ObjectMapper.Get();
            readMany.Parameters[0].Value = pooledMapper.instance.writer.Write(read.ids);
            return ExecuteReaderCommandSync(readMany);
        }
    }
}

#endif
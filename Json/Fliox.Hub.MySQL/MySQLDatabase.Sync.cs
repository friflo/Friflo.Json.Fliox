// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || MYSQL

using System;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.SQL;
using MySqlConnector;
using static Friflo.Json.Fliox.Hub.MySQL.MySQLUtils;

// ReSharper disable UseAwaitUsing
namespace Friflo.Json.Fliox.Hub.MySQL
{
    public partial class MySQLDatabase
    {
        // ------------------------------------------ sync / async ------------------------------------------
        protected override ISyncConnection GetConnectionSync()
        {
            if (connectionPool.TryPop(out var syncConnection)) {
                return syncConnection;
            }
            try {
                var connection = new MySqlConnection(connectionString);
                connection.Open();
                return new SyncConnection(connection);                
            }
            catch(MySqlException e) {
                return OpenError(e);
            }
            catch (Exception e) {
                return new SyncConnectionError(e);
            }
        }
        
        protected override TransResult Transaction(SyncContext syncContext, TransCommand command)
        {
            var syncConnection = syncContext.GetConnectionSync();
            if (syncConnection is not SyncConnection connection) {
                return new TransResult(syncConnection.Error.message);
            }
            var sql = GetTransactionCommand(command);
            if (sql == null) return new TransResult($"invalid transaction command {command}");
            try {
                connection.ExecuteNonQuerySync(sql);
                return new TransResult(command);
            }
            catch (MySqlException e) {
                return new TransResult(GetErrMsg(e));
            }
        }
        
        public override Result<RawSqlResult> ExecuteRawSQL(RawSql sql, SyncContext syncContext)
        {
            var syncConnection = syncContext.GetConnectionSync();
            if (syncConnection is not SyncConnection connection) {
                return Result.Error(syncConnection.Error.message);
            }
            try {
                using var reader = connection.ExecuteReaderSync(sql.command);
                return SQLTable.ReadRowsSync(reader);
            }
            catch (MySqlException e) {
                var msg = GetErrMsg(e);
                return Result.Error(msg);
            }
        }
    }
}

#endif

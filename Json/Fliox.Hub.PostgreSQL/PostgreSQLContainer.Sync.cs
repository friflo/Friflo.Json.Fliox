// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLSERVER

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Npgsql;


namespace Friflo.Json.Fliox.Hub.PostgreSQL
{
    internal sealed partial class PostgreSQLContainer
    {
        
        /// <summary>sync version of <see cref="ReadEntitiesAsync"/></summary>
        public override ReadEntitiesResult ReadEntities(ReadEntities command, SyncContext syncContext) {
            var syncConnection = syncContext.GetConnectionSync();
            if (syncConnection is not SyncConnection connection) {
                return new ReadEntitiesResult { Error = syncConnection.Error };
            }
            try {
                if (tableType == TableType.Relational) {
                    var sql             = SQL.ReadRelational(this, command);
                    using var reader    = connection.ExecuteReaderSync(sql);
                    var mapper          = new PostgresSQL2Json(reader);
                    return SQLTable.ReadEntitiesSync(reader, mapper, command, tableInfo, syncContext);
                } else {
                    var sql = SQL.ReadJsonColumn(this,command);
                    using var reader = connection.ExecuteReaderSync(sql);
                    return SQLUtils.ReadJsonColumnSync(reader, command);
                }
            }
            catch (PostgresException e) {
                return new ReadEntitiesResult { Error = new TaskExecuteError(e.MessageText) };
            }
        }
        
        /// <summary>sync version of <see cref="QueryEntitiesAsync"/></summary>
        public override QueryEntitiesResult QueryEntities(QueryEntities command, SyncContext syncContext) {
            var syncConnection = syncContext.GetConnectionSync();
            if (syncConnection is not SyncConnection connection) {
                return new QueryEntitiesResult { Error = syncConnection.Error };
            }
            var sql = SQL.Query(this, command);
            try {
                List<EntityValue> entities;
                using var reader = connection.ExecuteReaderSync(sql);
                if (tableType == TableType.Relational) {
                    entities = SQLTable.QueryEntitiesSync(reader, tableInfo, syncContext);
                } else {
                    entities = SQLUtils.QueryJsonColumnSync(reader);
                }
                return SQLUtils.CreateQueryEntitiesResult(entities, command, sql);
            }
            catch (PostgresException e) {
                return new QueryEntitiesResult { Error = new TaskExecuteError(e.MessageText), sql = sql };
            }
        }
    }
}

#endif
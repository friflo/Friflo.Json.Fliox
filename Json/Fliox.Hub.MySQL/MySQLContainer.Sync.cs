// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLSERVER

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using MySqlConnector;


namespace Friflo.Json.Fliox.Hub.MySQL
{
    internal sealed partial class MySQLContainer
    {
        /// <summary>sync version of <see cref="CreateEntitiesAsync"/> </summary>
        public override CreateEntitiesResult CreateEntities(CreateEntities command, SyncContext syncContext)
        {
            var syncConnection = syncContext.GetConnectionSync();
            if (syncConnection is not SyncConnection connection) {
                return new CreateEntitiesResult { Error = syncConnection.Error };
            }
            if (command.entities.Count == 0) {
                return new CreateEntitiesResult();
            }
            try {
                if (tableType == TableType.Relational) {
                    var sql = SQL.CreateRelational(this, command, syncContext);
                    connection.ExecuteNonQuerySync(sql);
                } else {
                    var sql = SQL.CreateJsonColumn(this, command);
                    connection.ExecuteNonQuerySync(sql);
                }
                return new CreateEntitiesResult();
            }
            catch (MySqlException e) {
                return new CreateEntitiesResult { Error = DatabaseError(e) };
            }
        }
        
        /// <summary>sync version of <see cref="UpsertEntitiesAsync"/> </summary>
        public override UpsertEntitiesResult UpsertEntities(UpsertEntities command, SyncContext syncContext)
        {
            var syncConnection = syncContext.GetConnectionSync();
            if (syncConnection is not SyncConnection connection) {
                return new UpsertEntitiesResult { Error = syncConnection.Error };
            }
            if (command.entities.Count == 0) {
                return new UpsertEntitiesResult();
            }
            try {
                if (tableType == TableType.Relational) {
                    var sql = SQL.UpsertRelational(this, command, syncContext);
                    connection.ExecuteNonQuerySync(sql);
                } else {
                    var sql = SQL.UpsertJsonColumn(this, command);
                    connection.ExecuteNonQuerySync(sql);
                }
                return new UpsertEntitiesResult();
            }
            catch (MySqlException e) {
                return new UpsertEntitiesResult { Error = DatabaseError(e) };
            }
        }
        
        /// <summary>sync version of <see cref="ReadEntitiesAsync"/></summary>
        public override ReadEntitiesResult ReadEntities(ReadEntities command, SyncContext syncContext)
        {
            var syncConnection = syncContext.GetConnectionSync();
            if (syncConnection is not SyncConnection connection) {
                return new ReadEntitiesResult { Error = syncConnection.Error };
            }
            try {
                if (tableType == TableType.Relational) {
                    var sql             = SQL.ReadRelational(this, command);
                    using var reader    = connection.ExecuteReaderSync(sql);
                    var mapper          = new SQL2JsonMapper(reader);
                    return SQLTable.ReadEntitiesSync(reader, mapper, command, tableInfo, syncContext);
                } else {
                    var sql = SQL.ReadJsonColumn(this,command);
                    using var reader = connection.ExecuteReaderSync(sql);
                    return SQLUtils.ReadJsonColumnSync(reader, command);
                }
            }
            catch (MySqlException e) {
                return new ReadEntitiesResult { Error = DatabaseError(e) };
            }
        }
        
        /// <summary>sync version of <see cref="QueryEntitiesAsync"/></summary>
        public override QueryEntitiesResult QueryEntities(QueryEntities command, SyncContext syncContext)
        {
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
            catch (MySqlException e) {
                return new QueryEntitiesResult { Error = DatabaseError(e), sql = sql };
            }
        }
        
        public override AggregateEntitiesResult AggregateEntities (AggregateEntities command, SyncContext syncContext)
        {
            var syncConnection = syncContext.GetConnectionSync();
            if (syncConnection is not SyncConnection connection) {
                return new AggregateEntitiesResult { Error = syncConnection.Error };
            }
            if (command.type == AggregateType.count) {
                var sql = SQL.Count(this, command);
                var result  = connection.ExecuteSync(sql);
                if (result.Failed) { return new AggregateEntitiesResult { Error = result.TaskError() }; }
                return new AggregateEntitiesResult { value = (long)result.value };
            }
            return new AggregateEntitiesResult { Error = NotImplemented($"type: {command.type}") };
        }
        
        /// <summary>sync version of <see cref="DeleteEntitiesAsync"/> </summary>
        public override DeleteEntitiesResult DeleteEntities(DeleteEntities command, SyncContext syncContext)
        {
            var syncConnection = syncContext.GetConnectionSync();
            if (syncConnection is not SyncConnection connection) {
                return new DeleteEntitiesResult { Error = syncConnection.Error };
            }
            try {
                if (command.all == true) {
                    connection.ExecuteNonQuerySync($"DELETE from {name}");
                    return new DeleteEntitiesResult();    
                }
                var sql = SQL.Delete(this, command);
                connection.ExecuteNonQuerySync(sql);
                return new DeleteEntitiesResult();
            }
            catch (MySqlException e) {
                return new DeleteEntitiesResult { Error = DatabaseError(e) };
            }
        }
    }
}

#endif
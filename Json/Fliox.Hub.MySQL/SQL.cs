// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Text;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using static Friflo.Json.Fliox.Hub.Host.SQL.SQLName;

#if !UNITY_5_3_OR_NEWER || SQLSERVER

namespace Friflo.Json.Fliox.Hub.MySQL
{
    internal static class SQL
    {
        
        // --- create
        internal static string CreateRelational(MySQLContainer container, CreateEntities command, SyncContext syncContext) {
            var sql = new StringBuilder();
            sql.Append($"INSERT INTO {container.name}");
            SQLTable.AppendValuesSQL(sql, command.entities, SQLEscape.BackSlash, container.tableInfo, syncContext);
            return sql.ToString();
        }
        
        internal static string CreateJsonColumn(MySQLContainer container, CreateEntities command) {
            var sql = new StringBuilder();
            sql.Append($"INSERT INTO {container.name} ({ID},{DATA})\nVALUES ");
            SQLUtils.AppendValuesSQL(sql, command.entities, SQLEscape.BackSlash);
            return sql.ToString();
        }
        
        // --- upsert
        internal static string UpsertRelational(MySQLContainer container, UpsertEntities command, SyncContext syncContext) {
            var sql = new StringBuilder();
            sql.Append($"REPLACE INTO {container.name}");
            SQLTable.AppendValuesSQL(sql, command.entities, SQLEscape.BackSlash, container.tableInfo, syncContext);
            return sql.ToString();
        }
        
        internal static string UpsertJsonColumn(MySQLContainer container, UpsertEntities command) {
            var sql = new StringBuilder();
            sql.Append($"REPLACE INTO {container.name} ({ID},{DATA})\nVALUES");
            SQLUtils.AppendValuesSQL(sql, command.entities, SQLEscape.BackSlash);
            return sql.ToString();
        }
        
        // --- read
        internal static string ReadRelational(MySQLContainer container, ReadEntities read) {
            var sql = new StringBuilder();
            sql.Append("SELECT "); SQLTable.AppendColumnNames(sql, container.tableInfo);
            sql.Append($" FROM {container.name} WHERE {container.tableInfo.keyColumn.name} in\n");
            SQLUtils.AppendKeysSQL(sql, read.ids, SQLEscape.BackSlash);
            return sql.ToString();
        }
        
        internal static string ReadJsonColumn(MySQLContainer container, ReadEntities read) {
            var sql = new StringBuilder();
            sql.Append($"SELECT {ID}, {DATA} FROM {container.name} WHERE {ID} in\n");
            SQLUtils.AppendKeysSQL(sql, read.ids, SQLEscape.BackSlash);
            return sql.ToString();
        }
        
        // --- query
        internal static string Query(MySQLContainer container, QueryEntities command) {
            var filter  = command.GetFilter();
            var where   = filter.IsTrue ? "TRUE" : filter.MySQLFilter(container.provider, container.tableType);
            var sql     = SQLUtils.QueryEntitiesSQL(command, container.name, where, container.tableInfo);
            return sql;
        }
        
        // --- query
        internal static string Count(MySQLContainer container, AggregateEntities command) {
            var filter  = command.GetFilter();
            var where   = filter.IsTrue ? "" : $" WHERE {filter.MySQLFilter(container.provider, container.tableType)}";
            return $"SELECT COUNT(*) from {container.name}{where}";
        }
        
        // --- delete
        internal static string Delete(MySQLContainer container, DeleteEntities command) {
            var sql = new StringBuilder();
            var id = container.tableType == TableType.Relational ? container.tableInfo.keyColumn.name : ID;
            sql.Append($"DELETE FROM  {container.name} WHERE {id} in\n");
            SQLUtils.AppendKeysSQL(sql, command.ids, SQLEscape.BackSlash);
            return sql.ToString();
        }
    }
}

#endif

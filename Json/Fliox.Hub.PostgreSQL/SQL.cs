// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Text;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using static Friflo.Json.Fliox.Hub.Host.SQL.SQLName;

#if !UNITY_5_3_OR_NEWER || SQLSERVER

namespace Friflo.Json.Fliox.Hub.PostgreSQL
{
    internal static class SQL
    {
        // --- create
        internal static string CreateRelational(PostgreSQLContainer container, CreateEntities command, SyncContext syncContext) {
            var sql = new StringBuilder();
            sql.Append($"INSERT INTO {container.name}");
            SQLTable.AppendValuesSQL(sql, command.entities, SQLEscape.HasBool, container.tableInfo, syncContext);
            return sql.ToString();
        }
        
        internal static string CreateJsonColumn(PostgreSQLContainer container, CreateEntities command) {
            var sql = new StringBuilder();
            sql.Append($"INSERT INTO {container.name} ({ID},{DATA}) VALUES\n");
            SQLUtils.AppendValuesSQL(sql, command.entities, SQLEscape.HasBool);
            return sql.ToString();
        }
        
        // --- upsert
        internal static string UpsertRelational(PostgreSQLContainer container, UpsertEntities command, SyncContext syncContext) {
            var sql = new StringBuilder();
            var tableInfo = container.tableInfo;
            sql.Append($"INSERT INTO {container.name}");
            SQLTable.AppendValuesSQL(sql, command.entities, SQLEscape.HasBool, tableInfo, syncContext);
            sql.Append($"\nON CONFLICT(\"{tableInfo.keyColumn.name}\") DO UPDATE SET "); // {DATA} = excluded.{DATA};");
            foreach (var column in tableInfo.columns) {
                sql.Append('"'); sql.Append(column.name); sql.Append("\"=excluded.\""); sql.Append(column.name); sql.Append("\", ");
            }
            sql.Length -= 2;
            sql.Append(';');
            return sql.ToString();
        }
        
        internal static string UpsertJsonColumn(PostgreSQLContainer container, UpsertEntities command) {
            var sql = new StringBuilder();
            sql.Append($"INSERT INTO {container.name} ({ID},{DATA}) VALUES\n");
            SQLUtils.AppendValuesSQL(sql, command.entities, SQLEscape.HasBool);
            sql.Append($"\nON CONFLICT({ID}) DO UPDATE SET {DATA} = excluded.{DATA};");
            return sql.ToString();
        }
        
        // --- read
        internal static string ReadRelational(PostgreSQLContainer container, ReadEntities read) {
            var sql = new StringBuilder();
            sql.Append("SELECT "); SQLTable.AppendColumnNames(sql, container.tableInfo);
            sql.Append($" FROM {container.name} WHERE \"{container.tableInfo.keyColumn.name}\" in\n");
            SQLUtils.AppendKeysSQL(sql,  read.ids, SQLEscape.Default);
            return sql.ToString();
        }
        
        internal static string ReadJsonColumn(PostgreSQLContainer container, ReadEntities read) {
            var sql = new StringBuilder();
            sql.Append($"SELECT {ID}, {DATA} FROM {container.name} WHERE {ID} in\n");
            SQLUtils.AppendKeysSQL(sql,  read.ids, SQLEscape.Default);
            return sql.ToString();
        }
        
        // --- query
        internal static string Query(PostgreSQLContainer container, QueryEntities command) {
            var filter  = command.GetFilter();
            var where   = filter.IsTrue ? "TRUE" : filter.PostgresFilter(container.tableInfo.type, container.tableType);
            var sql     = SQLUtils.QueryEntitiesSQL(command, container.name, where, container.tableInfo);
            return sql;
        }
        
        // --- count
        internal static string Count(PostgreSQLContainer container, AggregateEntities command) {
            var filter  = command.GetFilter();
            var where   = filter.IsTrue ? "" : $" WHERE {filter.PostgresFilter(container.tableInfo.type, container.tableType)}";
            return $"SELECT COUNT(*) from {container.name}{where}";
        }
        
        // --- delete
        internal static string Delete(PostgreSQLContainer container, DeleteEntities command) {
            var sql = new StringBuilder();
            var id = container.tableType == TableType.Relational ? container.tableInfo.keyColumn.name : ID;
            sql.Append($"DELETE FROM  {container.name} WHERE {id} in\n");
            SQLUtils.AppendKeysSQL(sql, command.ids, SQLEscape.Default);
            return sql.ToString();
        }
    }
}

#endif

// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Text;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using static Friflo.Json.Fliox.Hub.Host.SQL.SQLName;

#if !UNITY_5_3_OR_NEWER || SQLSERVER

namespace Friflo.Json.Fliox.Hub.SQLServer
{
    internal static class SQL
    {
        internal static string ReadJsonColumn(SQLServerContainer container, ReadEntities read) {
            var sql = new StringBuilder();
            sql.Append($"SELECT {ID}, {DATA} FROM {container.name} WHERE {ID} in\n");
            SQLUtils.AppendKeysSQL(sql, read.ids, SQLEscape.PrefixN);
            return sql.ToString();
        }
        
        internal static string Query(SQLServerContainer container, QueryEntities command) {
            var filter  = command.GetFilter();
            var where   = filter.IsTrue ? "(1=1)" : filter.SQLServerFilter(container.tableType);
            var sql     = SQLServerUtils.QueryEntities(command, container.name, where, container.tableInfo);
            return sql;
        }
        
        internal static string Count(SQLServerContainer container, AggregateEntities command) {
            var filter  = command.GetFilter();
            var where   = filter.IsTrue ? "" : $" WHERE {filter.SQLServerFilter(container.tableType)}";
            var sql     = $"SELECT COUNT(*) from {container.name}{where}";
            return sql;
        }
        
        internal static string DeleteRelational(SQLServerContainer container, DeleteEntities command) {
            var sql = new StringBuilder();
            sql.Append($"DELETE FROM  {container.name} WHERE [{container.tableInfo.keyColumn.name}] in\n");
            SQLUtils.AppendKeysSQL(sql, command.ids, SQLEscape.PrefixN);
            return sql.ToString();
        }
    }
}

#endif

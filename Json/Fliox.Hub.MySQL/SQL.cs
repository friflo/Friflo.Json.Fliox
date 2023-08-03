// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Text;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using static Friflo.Json.Fliox.Hub.Host.SQL.SQLName;

#if !UNITY_5_3_OR_NEWER || SQLSERVER

namespace Friflo.Json.Fliox.Hub.MySQL
{
    internal static class SQL
    {
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
        
        internal static string Query(MySQLContainer container, QueryEntities command) {
            var filter  = command.GetFilter();
            var where   = filter.IsTrue ? "TRUE" : filter.MySQLFilter(container.provider, container.tableType);
            var sql     = SQLUtils.QueryEntitiesSQL(command, container.name, where, container.tableInfo);
            return sql;
        }
    }
}

#endif

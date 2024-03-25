// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLSERVER

using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.SQLServer
{
    internal sealed class SyncConnection : SyncDbConnection
    {
        internal readonly   SqlConnection                   sqlInstance;
        
        public  override void       ClearPool() => SqlConnection.ClearPool(sqlInstance);
        
        public SyncConnection (SqlConnection instance) : base (instance) {
            sqlInstance = instance;
        }
        
        protected override DbCommand ReadRelational(TableInfo tableInfo, ReadEntities read) {
            var sql = new StringBuilder();
            sql.Append("SELECT "); SQLTable.AppendColumnNames(sql, tableInfo);
            sql.Append($" FROM {tableInfo.container} WHERE {tableInfo.keyColumn.name} in\n");
            SQLUtils.AppendKeysSQL(sql, read.ids, SQLEscape.PrefixN);
            return new SqlCommand(sql.ToString(), sqlInstance);
        }
        
        protected override DbCommand PrepareReadOne(TableInfo tableInfo)
        {
            var sql = new StringBuilder();
            sql.Append("SELECT "); SQLTable.AppendColumnNames(sql, tableInfo);
            sql.Append($" FROM {tableInfo.container} WHERE {tableInfo.keyColumn.name} = @id;");
            var readOne     = new SqlCommand(sql.ToString(), sqlInstance);
            var type        = tableInfo.keyColumn.type;
            var parameters  = readOne.Parameters;
            switch (type) {
                case ColumnType.Uint8:  parameters.Add("@id", SqlDbType.TinyInt);           break;
                case ColumnType.Int16:  parameters.Add("@id", SqlDbType.SmallInt);          break;
                case ColumnType.Int32:  parameters.Add("@id", SqlDbType.Int);               break;
                case ColumnType.Int64:  parameters.Add("@id", SqlDbType.BigInt);            break;
                case ColumnType.Guid:   parameters.Add("@id", SqlDbType.UniqueIdentifier);  break;
                case ColumnType.String: parameters.Add("@id", SqlDbType.NVarChar, 255);     break;
                default: throw new InvalidOperationException($"invalid key type: {type}");
            }
            return readOne;
        }
        
        protected override DbCommand PrepareReadMany(TableInfo tableInfo)
        {
            var sql = new StringBuilder();
            sql.Append("SELECT "); SQLTable.AppendColumnNames(sql, tableInfo);
            sql.Append($" FROM {tableInfo.container} WHERE {tableInfo.keyColumn.name} in (select value from openjson(@ids));");
            var readMany = new SqlCommand(sql.ToString(), sqlInstance);
            readMany.Parameters.Add("@ids", SqlDbType.NVarChar, 100);
            return readMany;
        }
    }
}

#endif
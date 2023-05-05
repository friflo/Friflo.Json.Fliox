// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLSERVER

using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Microsoft.Data.SqlClient;

// ReSharper disable UseAwaitUsing
namespace Friflo.Json.Fliox.Hub.SQLServer
{
    public static partial class SQLServerUtils
    {
        // --- create / upsert using VALUES() in SQL statement
        internal static DbCommand CreateEntitiesCmd_Values (SyncConnection connection, List<JsonEntity> entities, string table) {
            var sql = new StringBuilder();
            sql.Append($"INSERT INTO {table} (id,data) VALUES\n");
            SQLUtils.AppendValuesSQL(sql, entities);
            return Command(sql.ToString(), connection);
        }
        
        internal static DbCommand UpsertEntitiesCmd_Values (SyncConnection connection, List<JsonEntity> entities, string table) {
            var sql = new StringBuilder();
            sql.Append(
$@"MERGE {table} AS target
USING (VALUES");
            SQLUtils.AppendValuesSQL(sql, entities);
            sql.Append(
@") AS source (id, data)
ON source.id = target.id
WHEN MATCHED THEN
    UPDATE SET target.data = source.data
WHEN NOT MATCHED THEN
    INSERT (id, data)
    VALUES (id, data);");
            return Command(sql.ToString(), connection);
        }
        
        // --- create / upsert using SqlBulkCopy. Fails on insertion if primary key exists
        internal static async Task BulkCopy(SyncConnection connection, List<JsonEntity> entities, string name) {
            var table = SQLUtils.ToDataTable(entities);

            var count = table.Rows.Count;
            var rowArray = new DataRow[count];  
            table.Rows.CopyTo(rowArray, 0);
            
            var sqlConn = connection.instance as SqlConnection;
            var bulk = new SqlBulkCopy(sqlConn);
            bulk.DestinationTableName = name;

            await bulk.WriteToServerAsync(rowArray);
        }
        
        internal static DbCommand DeleteEntitiesCmd_Values (SyncConnection connection, List<JsonKey> ids, string table) {
            var sql = new StringBuilder();
            sql.Append($"DELETE FROM  {table} WHERE id in\n");
            SQLUtils.AppendKeysSQL(sql, ids);
            return Command(sql.ToString(), connection);
        }
    }
}

#endif
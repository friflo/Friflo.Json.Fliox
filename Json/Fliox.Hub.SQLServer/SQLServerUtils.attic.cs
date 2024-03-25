// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLSERVER

using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.SQL;
using System.Data.SqlClient;
using static Friflo.Json.Fliox.Hub.Host.SQL.SQLName;

// ReSharper disable UseAwaitUsing
namespace Friflo.Json.Fliox.Hub.SQLServer
{
    internal static partial class SQLServerUtils
    {
        // --- create / upsert using VALUES() in SQL statement
        internal static async Task CreateEntitiesCmd_Values (SyncConnection connection, List<JsonEntity> entities, string table) {
            var sql = new StringBuilder();
            sql.Append($"INSERT INTO {table} ({ID},{DATA}) VALUES\n");
            SQLUtils.AppendValuesSQL(sql, entities, SQLEscape.Default);
            await connection.ExecuteNonQueryAsync(sql.ToString()).ConfigureAwait(false);
        }
        
        internal static async Task UpsertEntitiesCmd_Values (SyncConnection connection, List<JsonEntity> entities, string table) {
            var sql = new StringBuilder();
            sql.Append(
$@"MERGE {table} AS target
USING (VALUES");
            SQLUtils.AppendValuesSQL(sql, entities, SQLEscape.Default);
            sql.Append(
$@") AS source ({ID}, {DATA})
ON source.{ID} = target.{ID}
WHEN MATCHED THEN
    UPDATE SET target.{DATA} = source.{DATA}
WHEN NOT MATCHED THEN
    INSERT ({ID}, {DATA})
    VALUES ({ID}, {DATA});");
            await connection.ExecuteNonQueryAsync(sql.ToString()).ConfigureAwait(false);
        }
        
        // --- create / upsert using SqlBulkCopy. Fails on insertion if primary key exists
        internal static async Task BulkCopy(SyncConnection connection, List<JsonEntity> entities, string name) {
            var table = SQLUtils.ToDataTable(entities);

            var count = table.Rows.Count;
            var rowArray = new DataRow[count];  
            table.Rows.CopyTo(rowArray, 0);
            
            var bulk = new SqlBulkCopy(connection.sqlInstance);
            bulk.DestinationTableName = name;

            await bulk.WriteToServerAsync(rowArray).ConfigureAwait(false);
        }
        
        internal static async Task DeleteEntitiesCmd_Values (SyncConnection connection, ListOne<JsonKey> ids, string table) {
            var sql = new StringBuilder();
            sql.Append($"DELETE FROM  {table} WHERE {ID} in\n");
            SQLUtils.AppendKeysSQL(sql, ids, SQLEscape.PrefixN);
            await connection.ExecuteNonQueryAsync(sql.ToString()).ConfigureAwait(false);
        }
    }
}

#endif
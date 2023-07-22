
#if !UNITY_5_3_OR_NEWER

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using NUnit.Framework;
using SqlKata;
using static Friflo.Json.Tests.Provider.Env;
using static NUnit.Framework.Assert;

// ReSharper disable CompareOfFloatsByEqualityOperator
namespace Friflo.Json.Tests.Provider.Test
{
    // ReSharper disable once InconsistentNaming
    public static class Test_6_Statement
    {
        private static bool SupportSQL (string db) => IsSQLite(db) || IsMySQL(db) || IsMariaDB(db) || IsPostgres(db) || IsSQLServer(db);

        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestStatement_Select(string db) {
            if (!SupportSQL(db)) return;
            
            var compiler    = GetCompiler();
            var query       = new Query("testreadtypes");
            var result      = compiler.Compile(query);
            
            var client      = await GetClient(db);
            // var sql2        = "DROP TABLE IF EXISTS `testops`;";
            var sqlResult   = client.std.ExecuteRawSQL(new RawSql(result.Sql, true));
            await client.SyncTasks();
            
            var raw         = sqlResult.Result;
            AreEqual(21, raw.rowCount);
            var columnCount = raw.columnCount;
            var rows        = raw.Rows;
            IsTrue(columnCount >= 16);
            int n = 0;
            foreach (var row in rows) {
                AreEqual(columnCount, row.count);
                AreEqual(n, row.index);
                AreEqual(n, raw.GetRow(n).index);
                ReadRow(row);
                n++;
            }
        }
        
        private static void ReadRow(RawSqlRow row) {
            var columns = row.columns;
            for (var i = 0; i < row.count; i++) {
                var type = columns[i].type;
                if (row.IsNull(i)) {
                    switch (type) {
                        case RawColumnType.JSON:    IsNull(row.GetJSON   (i));  break;
                        case RawColumnType.String:  IsNull(row.GetString (i));  break;
                    }
                    continue;
                }
                switch (type) {
                    case RawColumnType.Bool:        row.GetBoolean  (i);    break;
                    //
                    case RawColumnType.Uint8:       row.GetByte     (i);    break;
                    case RawColumnType.Int16:       row.GetInt16    (i);    break;
                    case RawColumnType.Int32:       row.GetInt32    (i);    break;
                    case RawColumnType.Int64:       row.GetInt64    (i);    break;
                    //
                    case RawColumnType.Float:       row.GetFlt32    (i);    break;
                    case RawColumnType.Double:      row.GetFlt64    (i);    break;
                    //
                    case RawColumnType.JSON:        row.GetJSON     (i);    break;
                    case RawColumnType.String:      row.GetString   (i);    break;
                    //
                    case RawColumnType.Guid:        row.GetGuid     (i);    break;
                    case RawColumnType.DateTime:    row.GetDateTime (i);    break;
                }
            }
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestStatement_InvalidCommand(string db) {
            if (!SupportSQL(db)) return;
            
            var client  = await GetClient(db);

            var sql1    = client.std.ExecuteRawSQL(new RawSql("select id, guid, ddd from testreadtypes;", true));
            var sql2    = client.std.ExecuteRawSQL(new RawSql("select id, guid, ddd from testreadtypes;"));
            var sql3    = client.std.ExecuteRawSQL(null);
            await client.TrySyncTasks();
            
            IsFalse(sql1.Success);
            AreEqual(TaskErrorType.CommandError, sql1.Error.type);
            
            IsFalse(sql2.Success);
            AreEqual(TaskErrorType.CommandError, sql2.Error.type);
            
            IsFalse(sql3.Success);
            AreEqual(TaskErrorType.CommandError, sql3.Error.type);
            AreEqual("CommandError ~ missing SQL statement", sql3.Error.Message);
        }
    }
}

#endif

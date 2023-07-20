
#if !UNITY_5_3_OR_NEWER

using System.Threading.Tasks;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.DB.Cluster;
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
            var sqlResult   = client.std.ExecuteRawSQL(result.Sql);
            await client.SyncTasks();
            
            var raw = sqlResult.Result;
            AreEqual(21, raw.rowCount);
            var columnCount = raw.columnCount;
            var rows = raw.Rows;
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
            for (var i = 0; i < row.count; i++) {
                var type = row.GetItemType(i);
                switch (type) {
                    case JsonItemType.True:
                    case JsonItemType.False:        row.GetBoolean  (i);    break;
                    //
                    case JsonItemType.Uint8:        row.GetByte     (i);    break;
                    case JsonItemType.Int16:        row.GetInt16    (i);    break;
                    case JsonItemType.Int32:        row.GetInt32    (i);    break;
                    case JsonItemType.Int64:        row.GetInt64    (i);    break;
                    //
                    case JsonItemType.Flt32:        row.GetFlt32    (i);    break;
                    case JsonItemType.Flt64:        row.GetFlt64    (i);    break;
                    //
                    case JsonItemType.ByteString:
                    case JsonItemType.CharString:   row.GetString   (i);    break;

                }
            }
        }
    }
}

#endif

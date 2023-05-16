using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Cosmos;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Provider.Env;

// ReSharper disable CompareOfFloatsByEqualityOperator
namespace Friflo.Json.Tests.Provider.Test
{
    // ReSharper disable once InconsistentNaming
    public static class Test_3_3_QuerySubFields
    {
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Compare_SubField_Equal_Int(string db) {
            var client  = await GetClient(db);
            var query   = client.compare.Query(c => c.obj.int32 == 0);
            AreEqual("c => c.obj.int32 == 0",   query.filterLinq);
            AreEqual("c['obj']['int32'] = 0",  query.filter.CosmosFilter());
            
            await client.SyncTasks();
            // if (db == "postgres") { AreEqual("SELECT id, data FROM compare WHERE (data -> 'obj' ->> 'int32')::numeric = 0", query.SQL); }
            LogSQL(query.SQL);
            AreEqual(1, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Compare_SubField_NotEqual_Int(string db) {
            var client  = await GetClient(db);
            var query   = client.compare.Query(c => c.obj.int32 != 0);
            AreEqual("c => c.obj.int32 != 0",   query.filterLinq);
            
            await client.SyncTasks();
            LogSQL(query.SQL);
            AreEqual(5, query.Result.Count);
        }
    }
}

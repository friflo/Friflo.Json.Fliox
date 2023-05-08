using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Cosmos;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Tests.Provider.Client;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Provider.Env;

// ReSharper disable CompareOfFloatsByEqualityOperator
namespace Friflo.Json.Tests.Provider.Test
{
    /// <summary>
    /// Requires implementation of cursors used in <see cref="EntityContainer.AggregateEntitiesAsync"/>.  
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class Test_4_Aggregate
    {
        private const int ArticleCount = 2;

        // --- query all
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestCount_All(string db) {
            var client  = await GetClient(db);
            var query   = client.testOps.CountAll();
            await client.SyncTasks();
            AreEqual(ArticleCount, query.Result);
        }

        // --- query filter: enum
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestCount_Enum(string db) {
            var client  = await GetClient(db);
            var query1  = client.testEnum.Count(t => t.enumVal == TestEnum.e1);
            var query2  = client.testEnum.Count(t => t.enumValNull == TestEnum.e2);
            var query3  = client.testEnum.Count(t => t.enumValNull == null);
            
            var query4  = client.testEnum.Count(t => TestEnum.e1 == t.enumVal);
            var query5  = client.testEnum.Count(t => TestEnum.e2 == t.enumValNull);
            var query6  = client.testEnum.Count(t => null == t.enumValNull);
            
            AreEqual("t => t.enumVal == 'e1'",      query1.filterLinq);
            AreEqual("t => t.enumValNull == 'e2'",  query2.filterLinq);
            AreEqual("t => t.enumValNull == null",  query3.filterLinq);
            
            AreEqual("t => 'e1' == t.enumVal",      query4.filterLinq);
            AreEqual("t => 'e2' == t.enumValNull",  query5.filterLinq);
            AreEqual("t => null == t.enumValNull",  query6.filterLinq);

            await client.SyncTasks();
            
            AreEqual(3, query1.Result);
            AreEqual(1, query2.Result);
            AreEqual(2, query3.Result);
            
            AreEqual(3, query4.Result);
            AreEqual(1, query5.Result);
            AreEqual(2, query6.Result);
        }
        
        // --- query filter: quantify Any
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestCount_AnyIntArray(string db) {
            if (IsSQLite(db) || IsPostgres || IsSQLServer || IsMySQL || IsMariaDB) return;
            
            var client  = await GetClient(db);
            var query   = client.testQuantify.Count(t => t.intArray.Any(i => i == 1));
            AreEqual("t => t.intArray.Any(i => i == 1)",      query.filterLinq);
            await client.SyncTasks();
            AreEqual(2, query.Result);
        }
        
        // --- query filter: quantify All
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestCount_AllIntArray(string db) {
            if (IsSQLite(db) || IsPostgres || IsSQLServer || IsMySQL || IsMariaDB) return;
            
            var client  = await GetClient(db);
            var query   = client.testQuantify.Count(t => t.intArray.All(i => i == 1));
            AreEqual("t => t.intArray.All(i => i == 1)",      query.filterLinq);
            await client.SyncTasks();
            AreEqual(4, query.Result);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestCount_Compare_Equals(string db) {
            var client  = await GetClient(db);
            var query   = client.compare.Count(c => c.int32 == 1);
            AreEqual("c => c.int32 == 1",       query.filterLinq);
            AreEqual("c['int32'] = 1",          query.filter.CosmosFilter());
            await client.SyncTasks();
            AreEqual(1, query.Result);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestCount_Compare_LessEqual_Int(string db) {
            var client  = await GetClient(db);
            var query   = client.compare.Count(c => c.int32 <= 1);
            AreEqual("c => c.int32 <= 1",      query.filterLinq);
            await client.SyncTasks();
            AreEqual(2, query.Result);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestCount_Compare_Greater_Int(string db) {
            var client  = await GetClient(db);
            var query   = client.compare.Count(c => c.int32 > 0);
            AreEqual("c => c.int32 > 0",      query.filterLinq);
            await client.SyncTasks();
            AreEqual(1, query.Result);
        }
    }
}

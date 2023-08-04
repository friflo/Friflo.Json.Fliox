using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Provider.Env;

// ReSharper disable CompareOfFloatsByEqualityOperator
namespace Friflo.Json.Tests.Provider.Test
{
    // ReSharper disable once InconsistentNaming
    public static class Test_3_5_QueryQuantify
    {
        // --- query filter: quantify Any
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_AnyIntArray(string db) {
            var client  = await GetClient(db);
            var query   = client.testQuantify.Query(t => t.intArray.Any(i => i == 1));
            AreEqual("t => t.intArray.Any(i => i == 1)",      query.filterLinq);
            await client.SyncTasksEnv();
            LogSQL(query.SQL);
            AreEqual(2, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_AnyIntList(string db) {
            var client  = await GetClient(db);
            var query   = client.testQuantify.Query(t => t.intList.Any(i => i == 1));
            AreEqual("t => t.intList.Any(i => i == 1)",      query.filterLinq);
            await client.SyncTasksEnv();
            LogSQL(query.SQL);
            AreEqual(2, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_AnyObjectArray(string db) {
            var client  = await GetClient(db);
            var query   = client.testQuantify.Query(t => t.objectArray.Any(o => o.int32 == 10));
            AreEqual("t => t.objectArray.Any(o => o.int32 == 10)",      query.filterLinq);
            await client.SyncTasksEnv();
            LogSQL(query.SQL);
            AreEqual(1, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_AnyObjectList(string db) {
            var client  = await GetClient(db);
            var query   = client.testQuantify.Query(t => t.objectList.Any(o => o.str == "str-10"));
            AreEqual("t => t.objectList.Any(o => o.str == 'str-10')",      query.filterLinq);
            await client.SyncTasksEnv();
            LogSQL(query.SQL);
            AreEqual(1, query.Result.Count);
        }
        
        // --- query filter: quantify All
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_AllIntArray(string db) {
            var client  = await GetClient(db);
            var query   = client.testQuantify.Query(t => t.intArray.All(i => i == 1));
            AreEqual("t => t.intArray.All(i => i == 1)",      query.filterLinq);
            await client.SyncTasksEnv();
            LogSQL(query.SQL);
            AreEqual(4, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_AllIntList(string db) {
            var client  = await GetClient(db);
            var query   = client.testQuantify.Query(t => t.intList.All(i => i == 1));
            AreEqual("t => t.intList.All(i => i == 1)",      query.filterLinq);
            await client.SyncTasksEnv();
            LogSQL(query.SQL);
            AreEqual(4, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_AllObjectArray(string db) {
            var client  = await GetClient(db);
            var query   = client.testQuantify.Query(t => t.objectArray.All(o => o.int32 == 10));
            AreEqual("t => t.objectArray.All(o => o.int32 == 10)",      query.filterLinq);
            await client.SyncTasksEnv();
            LogSQL(query.SQL);
            AreEqual(4, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_AllObjectList(string db) {
            var client  = await GetClient(db);
            var query   = client.testQuantify.Query(t => t.objectList.All(o => o.str == "str-10"));
            AreEqual("t => t.objectList.All(o => o.str == 'str-10')",      query.filterLinq);
            await client.SyncTasksEnv();
            LogSQL(query.SQL);
            AreEqual(4, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_MinObjectList(string db) {
            if (IsSQLite(db) || IsPostgres(db) || IsSQLServer(db) || IsMySQL(db) || IsMariaDB(db) || IsCosmosDB(db)) return;
            
            var client  = await GetClient(db);
            var query   = client.testQuantify.Query(t => t.objectList.Min(o => o.int32) >= 10);
            AreEqual("t => t.objectList.Min(o => o.int32) >= 10", query.filterLinq);
            await client.SyncTasksEnv();
            LogSQL(query.SQL);
            AreEqual(2, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_MaxObjectList(string db) {
            if (IsSQLite(db) || IsPostgres(db) || IsSQLServer(db) || IsMySQL(db) || IsMariaDB(db) || IsCosmosDB(db)) return;
            
            var client  = await GetClient(db);
            var query   = client.testQuantify.Query(t => t.objectList.Max(o => o.int32) >= 10);
            AreEqual("t => t.objectList.Max(o => o.int32) >= 10", query.filterLinq);
            await client.SyncTasksEnv();
            LogSQL(query.SQL);
            AreEqual(2, query.Result.Count);
        }
    }
}

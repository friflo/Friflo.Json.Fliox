using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Tests.Provider.Client;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Provider.Env;

// ReSharper disable CompareOfFloatsByEqualityOperator
namespace Friflo.Json.Tests.Provider.Test
{
    public static class TestQueryFields
    {
        private const int ArticleCount = 2;

        // --- query all
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_All(string db) {
            var store = await GetClient(db);
            var query = store.testOps.QueryAll();
            await store.SyncTasks();
            AreEqual(ArticleCount, query.Result.Count);
        }

        // --- query filter: enum
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Enum(string db) {
            var store = await GetClient(db);
            var query1 = store.testEnum.Query(t => t.enumVal == TestEnum.e1);
            var query2 = store.testEnum.Query(t => t.enumValNull == TestEnum.e2);
            var query3 = store.testEnum.Query(t => t.enumValNull == null);
            
            var query4 = store.testEnum.Query(t => TestEnum.e1 == t.enumVal);
            var query5 = store.testEnum.Query(t => TestEnum.e2 == t.enumValNull);
            var query6 = store.testEnum.Query(t => null == t.enumValNull);
            
            AreEqual("t => t.enumVal == 'e1'",      query1.filterLinq);
            AreEqual("t => t.enumValNull == 'e2'",  query2.filterLinq);
            AreEqual("t => t.enumValNull == null",  query3.filterLinq);
            
            AreEqual("t => 'e1' == t.enumVal",      query4.filterLinq);
            AreEqual("t => 'e2' == t.enumValNull",  query5.filterLinq);
            AreEqual("t => null == t.enumValNull",  query6.filterLinq);

            await store.SyncTasks();
            
            AreEqual(3, query1.Result.Count);
            AreEqual(1, query2.Result.Count);
            AreEqual(2, query3.Result.Count);
            
            AreEqual(3, query4.Result.Count);
            AreEqual(1, query5.Result.Count);
            AreEqual(2, query6.Result.Count);
        }
        
        // --- query filter: quantify Any
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] // [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_AnyIntArray(string db) {
            var store   = await GetClient(db);
            var query   = store.testQuantify.Query(t => t.intArray.Any(i => i == 1));
            AreEqual("t => t.intArray.Any(i => i == 1)",      query.filterLinq);
            await store.SyncTasks();
            AreEqual(2, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] // [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_AnyIntList(string db) {
            var store   = await GetClient(db);
            var query   = store.testQuantify.Query(t => t.intList.Any(i => i == 1));
            AreEqual("t => t.intList.Any(i => i == 1)",      query.filterLinq);
            await store.SyncTasks();
            AreEqual(2, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] // [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_AnyObjectArray(string db) {
            var store   = await GetClient(db);
            var query   = store.testQuantify.Query(t => t.objectArray.Any(o => o.int32 == 10));
            AreEqual("t => t.objectArray.Any(o => o.int32 == 10)",      query.filterLinq);
            await store.SyncTasks();
            AreEqual(1, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] // [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_AnyObjectList(string db) {
            var store   = await GetClient(db);
            var query   = store.testQuantify.Query(t => t.objectList.Any(o => o.str == "str-10"));
            AreEqual("t => t.objectList.Any(o => o.str == 'str-10')",      query.filterLinq);
            await store.SyncTasks();
            AreEqual(1, query.Result.Count);
        }
        
        // --- query filter: quantify All
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] // [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_AllIntArray(string db) {
            var store   = await GetClient(db);
            var query   = store.testQuantify.Query(t => t.intArray.All(i => i == 1));
            AreEqual("t => t.intArray.All(i => i == 1)",      query.filterLinq);
            await store.SyncTasks();
            AreEqual(4, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] // [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_AllIntList(string db) {
            var store   = await GetClient(db);
            var query   = store.testQuantify.Query(t => t.intList.All(i => i == 1));
            AreEqual("t => t.intList.All(i => i == 1)",      query.filterLinq);
            await store.SyncTasks();
            AreEqual(4, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] // [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_AllObjectArray(string db) {
            var store   = await GetClient(db);
            var query   = store.testQuantify.Query(t => t.objectArray.All(o => o.int32 == 10));
            AreEqual("t => t.objectArray.All(o => o.int32 == 10)",      query.filterLinq);
            await store.SyncTasks();
            AreEqual(4, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] // [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_AllObjectList(string db) {
            var store   = await GetClient(db);
            var query   = store.testQuantify.Query(t => t.objectList.All(o => o.str == "str-10"));
            AreEqual("t => t.objectList.All(o => o.str == 'str-10')",      query.filterLinq);
            await store.SyncTasks();
            AreEqual(4, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Compare_Equals(string db) {
            var store   = await GetClient(db);
            var query   = store.compare.Query(c => c.int32 == 1);
            AreEqual("c => c.int32 == 1",      query.filterLinq);
            await store.SyncTasks();
            AreEqual(1, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Compare_Equals_null(string db) {
            var store   = await GetClient(db);
            var query   = store.compare.Query(c => c.int32 == null);
            AreEqual("c => c.int32 == null",      query.filterLinq);
            await store.SyncTasks();
            AreEqual(2, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Compare_Equals_null2(string db) {
            var store   = await GetClient(db);
            var query   = store.compare.Query(c => null ==  c.int32);
            AreEqual("c => null == c.int32",      query.filterLinq);
            await store.SyncTasks();
            AreEqual(2, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Compare_NotEquals_null(string db) {
            var store   = await GetClient(db);
            var query   = store.compare.Query(c => c.int32 != null);
            AreEqual("c => c.int32 != null",      query.filterLinq);
            await store.SyncTasks();
            AreEqual(1, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Compare_Less_Int(string db) {
            var store   = await GetClient(db);
            var query   = store.compare.Query(c => c.int32 < 2);
            AreEqual("c => c.int32 < 2",      query.filterLinq);
            await store.SyncTasks();
            AreEqual(1, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Compare_LessEqual_Int(string db) {
            var store   = await GetClient(db);
            var query   = store.compare.Query(c => c.int32 <= 1);
            AreEqual("c => c.int32 <= 1",      query.filterLinq);
            await store.SyncTasks();
            AreEqual(1, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Compare_Greater_Int(string db) {
            var store   = await GetClient(db);
            var query   = store.compare.Query(c => c.int32 > 0);
            AreEqual("c => c.int32 > 0",      query.filterLinq);
            await store.SyncTasks();
            AreEqual(1, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Compare_GreaterEqual_Int(string db) {
            var store   = await GetClient(db);
            var query   = store.compare.Query(c => c.int32 >= 0);
            AreEqual("c => c.int32 >= 0",      query.filterLinq);
            await store.SyncTasks();
            AreEqual(1, query.Result.Count);
        }
    }
}

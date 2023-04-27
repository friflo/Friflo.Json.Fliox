using System.Threading.Tasks;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Provider.Env;

namespace Friflo.Json.Tests.Provider.Test
{
    // ReSharper disable once InconsistentNaming
    public static class Test_5_QueryCursor
    {
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Limit(string db) {
            var client  = await GetClient(db);
            var query   = client.testCursor.QueryAll();
            query.limit = 2;
            await client.SyncTasks();
            AreEqual(2, query.Result.Count);
        }
        
        // Using maxCount less than available entities. So multiple query are required to return all entities.
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Cursor_MultiStep(string db) {
            var client      = await GetClient(db);
            var query       = client.testCursor.QueryAll();
            int count       = 0;
            int iterations  = 0;
            while (true) {
                query.maxCount  = 2;
                iterations++;
                await client.SyncTasks();
                
                count          += query.Result.Count;
                var cursor      = query.ResultCursor;
                if (cursor == null)
                    break;
                query           = client.testCursor.QueryAll();
                query.cursor    = cursor;
            }
            AreEqual(3, iterations);
            AreEqual(5, count);
        }
        
        // Using maxCount greater than available entities. So a single query return all entities.
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Cursor_SingleStep(string db) {
            var client      = await GetClient(db);
            var query       = client.testCursor.QueryAll();
            query.maxCount  = 100;
            await client.SyncTasks();
                
            AreEqual(5, query.Result.Count);
            IsNull(query.ResultCursor);
        }
        
        // Using maxCount less than available entities matching the filter.
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Cursor_Filter(string db) {
            var client      = await GetClient(db);
            var query       = client.testCursor.Query(c => c.value == 100);
            int count       = 0;
            int iterations  = 0;
            while (true) {
                query.maxCount  = 2;
                iterations++;
                await client.SyncTasks();
                
                count          += query.Result.Count;
                var cursor      = query.ResultCursor;
                if (cursor == null)
                    break;
                query           = client.testCursor.Query(c => c.value == 100); // todo add QueryNext()
                query.cursor    = cursor;
            }
            AreEqual(2, iterations);
            AreEqual(3, count);
        }
        
        // todo add remove cursor tests
    }
}

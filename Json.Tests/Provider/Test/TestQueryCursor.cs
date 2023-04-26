
using System.Threading.Tasks;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Provider.Env;

// ReSharper disable CompareOfFloatsByEqualityOperator
namespace Friflo.Json.Tests.Provider.Test
{
    public static class TestQueryCursor
    {
        private const int ArticleCount = 5;

        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] // todo [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Limit(string db) {
            var client  = await GetClient(db);
            var query   = client.testQuantify.QueryAll();
            query.limit = 2;
            await client.SyncTasks();
            AreEqual(2, query.Result.Count);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] // todo [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Cursor(string db) {
            var client      = await GetClient(db);
            var query       = client.testQuantify.QueryAll();
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
                query           = client.testQuantify.QueryAll();
                query.cursor    = cursor;
            }
            AreEqual(3, iterations);
            AreEqual(5, count);
        }

    }
}

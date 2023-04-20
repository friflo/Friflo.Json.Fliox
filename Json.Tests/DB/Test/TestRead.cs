using System.Threading.Tasks;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.DB.Env;

namespace Friflo.Json.Tests.DB.Test
{
    public static class TestRead
    {
        // --- read by id
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestRead_One(string db) {
            var store = await GetClient(db);
            var find  = store.testOps.Read().Find("a-1");
            await store.SyncTasks();
            NotNull(find.Result);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)]
        public static async Task TestRead_Many(string db) {
            var store = await GetClient(db);
            var read  = store.testOps.Read();
            var find1  = read.Find("a-1");
            var find2  = read.Find("a-2");
            await store.SyncTasks();
            NotNull(find1.Result);
            NotNull(find2.Result);
        }
    }
}
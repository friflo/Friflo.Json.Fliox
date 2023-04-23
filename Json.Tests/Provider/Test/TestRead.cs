using System.Threading.Tasks;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Provider.Env;

namespace Friflo.Json.Tests.Provider.Test
{
    public static class TestRead
    {
        // --- read by id
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_One(string db) {
            var client  = await GetClient(db);
            var find    = client.testOps.Read().Find("a-1");
            await client.SyncTasks();
            NotNull(find.Result);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestRead_Many(string db) {
            var client  = await GetClient(db);
            var read    = client.testOps.Read();
            var find1   = read.Find("a-1");
            var find2   = read.Find("a-2");
            await client.SyncTasks();
            NotNull(find1.Result);
            NotNull(find2.Result);
        }
    }
}

#if !UNITY_5_3_OR_NEWER

using System.Threading.Tasks;
using Friflo.Playground.Client;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Playground.DB.Env;

namespace Friflo.Playground.DB
{
    public static class TestDB
    {
        [OneTimeSetUp]
        public static void Init() {
            Setup();
        }
        
        private static TestClient GetClient(string db) {
            var hub    = GetDatabaseHub(db);
            return new TestClient(hub);
        }

        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestQueryAll(string db) {
            var store = GetClient(db);
            var query = store.articles.QueryAll();
            await store.SyncTasks();
            AreEqual(2, query.Result.Count);
        }
        
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestRead_One(string db) {
            var store = GetClient(db);
            var find  = store.articles.Read().Find("a-1");
            await store.SyncTasks();
            NotNull(find.Result);
        }
        
        [TestCase(File, Category = File)] [TestCase(Memory, Category = Memory)] [TestCase(Cosmos, Category = Cosmos)]
        public static async Task TestRead_Many(string db) {
            var store = GetClient(db);
            var read  = store.articles.Read();
            var find1  = read.Find("a-1");
            var find2  = read.Find("a-2");
            await store.SyncTasks();
            NotNull(find1.Result);
            NotNull(find2.Result);
        }

    }
}

#endif

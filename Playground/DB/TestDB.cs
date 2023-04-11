
#if !UNITY_5_3_OR_NEWER

using System.Threading.Tasks;
using Friflo.Playground.Client;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Playground.DB
{
    public static class TestDB
    {
        [OneTimeSetUp]
        public static void Init() {
            Env.Setup();
        }
        
        private static TestClient GetClient(string db) {
            var hub    = Env.GetDatabaseHub(db);
            return new TestClient(hub);
        }

        [TestCase(Env.File,   Category = Env.File)]
        [TestCase(Env.Memory, Category = Env.Memory)]
        public static async Task TestQuery(string db) {
            var store = GetClient(db);
            var query = store.articles.QueryAll();
            await store.SyncTasks();
            AreEqual(1, query.Result.Count);
        }
    }
}

#endif

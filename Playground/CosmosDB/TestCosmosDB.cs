
#if !UNITY_5_3_OR_NEWER

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Cosmos;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Provider;
using Friflo.Json.Tests.Unity.Utils;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace Friflo.Playground.CosmosDB
{
    public static class TestCosmosDB
    {
        /// withdraw from allocation detection by <see cref="LeakTestsFixture"/> => init before tracking starts
        [OneTimeSetUp]    public static void  Init()       { TestGlobals.Init(); }
        [OneTimeTearDown] public static void  Dispose()    { TestGlobals.Dispose(); }


        
        [Test]
        public static async Task CosmosCreatePocStore() {
            var client              = Json.Tests.Provider.CosmosEnv.CreateCosmosClient();
            var cosmosDatabase      = await client.CreateDatabaseIfNotExistsAsync(nameof(PocStore));
            using (var _            = SharedEnv.Default) // for LeakTestsFixture
            using (var database     = new CosmosDatabase("main_db", cosmosDatabase, new PocService()) { Throughput = 400 })
            using (var hub          = new FlioxHub(database, TestGlobals.Shared))
            using (var createStore  = new PocStore(hub) { UserId = "createStore"})
            using (var useStore     = new PocStore(hub) { UserId = "useStore"}) {
                await TestRelationPoC.CreateStore(createStore);
                await TestHappy.TestStores(createStore, useStore);
            }
        }
        
        [Test] 
        public static async Task CosmosTestEntityKey() {
            var client              = Json.Tests.Provider.CosmosEnv.CreateCosmosClient();
            var cosmosDatabase      = await client.CreateDatabaseIfNotExistsAsync(nameof(EntityIdStore));
            using (var _            = SharedEnv.Default) // for LeakTestsFixture
            using (var database     = new CosmosDatabase("main_db", cosmosDatabase, new PocService()) { Throughput = 400 })
            using (var hub          = new FlioxHub(database, TestGlobals.Shared)) {
                await TestEntityKey.AssertEntityKeyTests (hub);
            }
        }
    }
}

#endif

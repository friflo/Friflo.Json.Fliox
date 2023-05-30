
#if !UNITY_5_3_OR_NEWER

using System.Data.Common;
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

        private static CosmosClient CreateCosmosClient() {
            var privateSettings     = CommonUtils.GetBasePath() + "appsettings.private.json";
            var configuration       = new ConfigurationBuilder().AddJsonFile(privateSettings).Build();
            var connectionString    = configuration["cosmos"];    // The Azure Cosmos DB endpoint for running this sample.
            var builder             = new DbConnectionStringBuilder { ConnectionString = connectionString! };
            var accountEndpoint     = (string)builder["AccountEndpoint"];
            var accountKey          = (string)builder["AccountKey"];
            var options             = new CosmosClientOptions { ApplicationName = "Friflo.Playground" };
            return new CosmosClient(accountEndpoint, accountKey, options);
        }
        
        [Test]
        public static async Task CosmosCreatePocStore() {
            var client              = CreateCosmosClient();
            using (var _            = SharedEnv.Default) // for LeakTestsFixture
            using (var database     = new CosmosDatabase("main_db", client, null, new PocService()) { Throughput = 400 })
            using (var hub          = new FlioxHub(database, TestGlobals.Shared))
            using (var createStore  = new PocStore(hub) { UserId = "createStore"})
            using (var useStore     = new PocStore(hub) { UserId = "useStore"}) {
                await TestRelationPoC.CreateStore(createStore);
                await TestHappy.TestStores(createStore, useStore);
            }
        }
        
        [Test] 
        public static async Task CosmosTestEntityKey() {
            var client              = CreateCosmosClient();
            using (var _            = SharedEnv.Default) // for LeakTestsFixture
            using (var database     = new CosmosDatabase("main_db", client, null, new PocService()) { Throughput = 400 })
            using (var hub          = new FlioxHub(database, TestGlobals.Shared)) {
                await TestEntityKey.AssertEntityKeyTests (hub);
            }
        }
    }
}

#endif

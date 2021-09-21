
#if !UNITY_5_3_OR_NEWER

using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Cosmos;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy;
using Friflo.Json.Tests.Common.Utils;
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

        private static IConfiguration InitConfiguration() {
            var appSettings     = CommonUtils.GetBasePath() + "appsettings.test.json";
            var privateSettings = CommonUtils.GetBasePath() + "appsettings.private.json";
            return new ConfigurationBuilder().AddJsonFile(appSettings).AddJsonFile(privateSettings).Build();
        }
        
        private static CosmosClient _client;
        
        private static CosmosClient CreateCosmosClient() {
            if (_client != null)
                return _client;
            var config      = InitConfiguration();
            var endpointUri = config["EndPointUri"];    // The Azure Cosmos DB endpoint for running this sample.
            var primaryKey  = config["PrimaryKey"];     // The primary key for the Azure Cosmos account.
            var options     = new CosmosClientOptions { ApplicationName = "Friflo.Playground" };
            return _client  = new CosmosClient(endpointUri, primaryKey, options);
        }   
        
        [Test]
        public static async Task CosmosCreatePocStore() {
            var client              = CreateCosmosClient();
            var cosmosDatabase      = await client.CreateDatabaseIfNotExistsAsync(nameof(PocStore));
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var database     = new CosmosDatabase(cosmosDatabase, 400))
            using (var createStore  = new PocStore(database, "createStore"))
            using (var useStore     = new PocStore(database, "useStore")) {
                await TestRelationPoC.CreateStore(createStore);
                await TestStore.TestStores(createStore, useStore);
            }
        }
        
        [Test] 
        public static async Task CosmosTestEntityKey() {
            var client              = CreateCosmosClient();
            var cosmosDatabase      = await client.CreateDatabaseIfNotExistsAsync(nameof(EntityIdStore));
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var typeStore    = new TypeStore())
            using (var database     = new CosmosDatabase(cosmosDatabase, 400)) {
                await TestEntityKey.AssertEntityKeyTests (database, typeStore);
            }
        }
    }
}

#endif

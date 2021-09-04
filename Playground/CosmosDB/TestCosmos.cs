
#if !UNITY_5_3_OR_NEWER

using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Cosmos;
using Friflo.Json.Fliox.DB.Database;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Graph;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Graph.Happy;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace Friflo.Playground.CosmosDB
{
    public static class TestCosmos
    {
        /// withdraw from allocation detection by <see cref="LeakTestsFixture"/> => init before tracking starts
        [OneTimeSetUp]    public static void  Init()       { TestGlobals.Init(); }
        [OneTimeTearDown] public static void  Dispose()    { TestGlobals.Dispose(); }
        
        [Test] public static async Task  CosmosCreateAsync() { await CosmosCreate(); }

        private static IConfiguration InitConfiguration() {
            var appSettings     = CommonUtils.GetBasePath() + "appsettings.test.json";
            var privateSettings = CommonUtils.GetBasePath() + "appsettings.private.json";
            return new ConfigurationBuilder().AddJsonFile(appSettings).AddJsonFile(privateSettings).Build();
        }
        
        private static async Task<DatabaseResponse> CosmosCreateDatabase() {
            var config = InitConfiguration();
            var endpointUri = config["EndPointUri"];    // The Azure Cosmos DB endpoint for running this sample.
            var primaryKey  = config["PrimaryKey"];     // The primary key for the Azure Cosmos account.
            var options     = new CosmosClientOptions { ApplicationName = "Friflo.Playground" };
            var client      = new CosmosClient(endpointUri, primaryKey, options);
            return await client.CreateDatabaseIfNotExistsAsync("PocStore");
        }

        private static async Task CosmosCreate() {
            var database            = await CosmosCreateDatabase();
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase = new CosmosDatabase(database, 400))
            using (var createStore  = new PocStore(fileDatabase, "createStore"))
            using (var useStore     = new PocStore(fileDatabase, "useStore")) {
                await TestRelationPoC.CreateStore(createStore);
                await TestStore.TestStores(createStore, useStore);
            }
        }
    }
}

#endif

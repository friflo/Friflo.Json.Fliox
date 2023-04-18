#if !UNITY_5_3_OR_NEWER

using Friflo.Json.Tests.Common.Utils;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace Friflo.Json.Tests.DB
{
    public static class EnvCosmosDB
    {
        private static IConfiguration InitConfiguration() {
            var basePath        = CommonUtils.GetBasePath();
            var appSettings     = basePath + "appsettings.test.json";
            var privateSettings = basePath + "appsettings.private.json";
            return new ConfigurationBuilder().AddJsonFile(appSettings).AddJsonFile(privateSettings).Build();
        }
        
        private static CosmosClient _client;
        
        public static CosmosClient CreateCosmosClient() {
            if (_client != null)
                return _client;
            var config      = InitConfiguration();
            var endpointUri = config["EndPointUri"];    // The Azure Cosmos DB endpoint for running this sample.
            var primaryKey  = config["PrimaryKey"];     // The primary key for the Azure Cosmos account.
            var options     = new CosmosClientOptions { ApplicationName = "Friflo.Playground" };
            return _client  = new CosmosClient(endpointUri, primaryKey, options);
        }   
    }
}

#endif

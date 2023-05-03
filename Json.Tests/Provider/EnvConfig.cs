#if !UNITY_5_3_OR_NEWER

using System;
using System.Threading.Tasks;
using Friflo.Json.Tests.Common.Utils;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace Friflo.Json.Tests.Provider
{
    public static class EnvConfig
    {
        private static IConfiguration InitConfiguration() {
            var basePath        = CommonUtils.GetBasePath();
            var appSettings     = basePath + "appsettings.test.json";
            var privateSettings = basePath + "appsettings.private.json";
            return new ConfigurationBuilder().AddJsonFile(appSettings).AddJsonFile(privateSettings).Build();
        }
        
        // --- CosmosDB
        public static CosmosClient CreateCosmosClient() {
            var config      = InitConfiguration();
            var endpointUri = config["EndPointUri"];    // The Azure Cosmos DB endpoint for running this sample.
            var primaryKey  = config["PrimaryKey"];     // The primary key for the Azure Cosmos account.
            var options     = new CosmosClientOptions { ApplicationName = "Friflo.Playground" };
            return new CosmosClient(endpointUri, primaryKey, options);
        }

        // --- MySQL / MariaDB
        public static async Task<MySqlConnection> OpenMySQLConnection(string provider) {
            var config              = InitConfiguration();
            string connectionString = config[provider];
            if (connectionString == null) {
                throw new ArgumentException($"provider not found in appsettings. provider: {provider}");
            }
            var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            return connection;
        }
        
        // --- PostgreSQL
        public static string GetPostgresConnection() {
            var config = InitConfiguration();
            return config["postgres"];
        }
        
        // --- SQL Server
        public static string GetSQLServerConnection() {
            var config = InitConfiguration();
            return config["sqlserver"];
        }
    }
}

#endif

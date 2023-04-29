#if !UNITY_5_3_OR_NEWER

using System;
using System.Threading.Tasks;
using Friflo.Json.Tests.Common.Utils;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace Friflo.Json.Tests.Provider
{
    public static class MySQLEnv
    {
        private static IConfiguration InitConfiguration() {
            var basePath        = CommonUtils.GetBasePath();
            var appSettings     = basePath + "appsettings.test.json";
            var privateSettings = basePath + "appsettings.private.json";
            return new ConfigurationBuilder().AddJsonFile(appSettings).AddJsonFile(privateSettings).Build();
        }
        
        public static async Task<MySqlConnection> OpenMySQLConnection(string provider) {
            var config              = InitConfiguration();
            string connectionString =  config[provider];
            if (connectionString == null) {
                throw new ArgumentException($"provider not found in appsettings. provider: {provider}");
            }
            var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            return connection;
        }   
    }
}

#endif

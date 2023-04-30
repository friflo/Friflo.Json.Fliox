#if !UNITY_5_3_OR_NEWER

using System.Threading.Tasks;
using Friflo.Json.Tests.Common.Utils;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Friflo.Json.Tests.Provider
{
    public static class PostgreSQLEnv
    {
        private static IConfiguration InitConfiguration() {
            var basePath        = CommonUtils.GetBasePath();
            var appSettings     = basePath + "appsettings.test.json";
            var privateSettings = basePath + "appsettings.private.json";
            return new ConfigurationBuilder().AddJsonFile(appSettings).AddJsonFile(privateSettings).Build();
        }
        
        public static async Task<NpgsqlConnection> OpenPostgresConnection() {
            var config              = InitConfiguration();
            string connectionString =  config["postgres"];
            var connection          = new NpgsqlConnection(connectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            return connection;
        }   
    }
}

#endif

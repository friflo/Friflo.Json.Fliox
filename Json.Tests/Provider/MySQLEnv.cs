#if !UNITY_5_3_OR_NEWER

using Friflo.Json.Tests.Common.Utils;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace Friflo.Json.Tests.Provider
{
    public static class MySQLEnv
    {
        private static IConfiguration InitConfiguration() {
            var basePath        = CommonUtils.GetBasePath();
            var appSettings     = basePath + "mysql.test.json";
            var privateSettings = basePath + "mysql.private.json";
            return new ConfigurationBuilder().AddJsonFile(appSettings).AddJsonFile(privateSettings).Build();
        }
        
        private static MySqlConnection _connection;
        
        public static MySqlConnection CreateMySQLConnection() {
            if (_connection != null)
                return _connection;
            var config              = InitConfiguration();
            var connectionString    = config["MySQLConnection"];
            return _connection = new MySqlConnection(connectionString);
        }   
    }
}

#endif

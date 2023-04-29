#if !UNITY_5_3_OR_NEWER

using System;
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
        
        private static MySqlConnection _connection;
        
        public static MySqlConnection CreateMySQLConnection(string provider) {
            if (_connection != null)
                return _connection;
            var config              = InitConfiguration();
            string connectionString =  config[provider];
            if (connectionString == null) {
                throw new ArgumentException($"provider not found in appsettings. provider: {provider}");
            }
            return _connection = new MySqlConnection(connectionString);
        }   
    }
}

#endif

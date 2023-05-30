#if !UNITY_5_3_OR_NEWER

using Friflo.Json.Tests.Common.Utils;
using Microsoft.Extensions.Configuration;

namespace Friflo.Json.Tests.Provider
{
    public static class EnvConfig
    {
        private static readonly IConfiguration Configuration;
        
        static EnvConfig() {
            var basePath        = CommonUtils.GetBasePath();
            var appSettings     = basePath + "appsettings.test.json";
            var privateSettings = basePath + "appsettings.private.json";
            Configuration       = new ConfigurationBuilder().AddJsonFile(appSettings).AddJsonFile(privateSettings).Build();
        }
        
        public static string GetConnectionString(string provider) {
            return Configuration[provider];
        }
    }
}

#endif

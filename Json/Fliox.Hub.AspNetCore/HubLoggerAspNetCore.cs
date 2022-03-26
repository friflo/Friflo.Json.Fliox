using Microsoft.Extensions.Logging;

namespace Friflo.Json.Fliox.Hub.AspNetCore
{
    public class HubLoggerAspNetCore : IHubLogger
    {
        private readonly ILogger logger;
        
        public HubLoggerAspNetCore(ILoggerFactory loggerFactory) {
            logger = loggerFactory.CreateLogger<HubLoggerAspNetCore>();
        }
        
        public void Log(HubLog hubLog, string message) {
            var logLevel = GetLogLevel(hubLog);
            logger.Log(logLevel, message);
        }
        
        private static LogLevel GetLogLevel(HubLog hubLog) {
            switch (hubLog) {
                case HubLog.Error:  return LogLevel.Error;
                case HubLog.Info:   return LogLevel.Information;
                default:            return LogLevel.Error;
            }
        }
    }
}
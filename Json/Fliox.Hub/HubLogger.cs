// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Fliox.Hub
{
    public enum HubLog {
        Error,
        Info
    }

    public interface IHubLogger {
        void Log  (HubLog hubLog, string message, Exception exception);
    }
    
    // ----------------------------------- HubLogger -----------------------------------
    public class HubLogger : IHubLogger
    {
        internal    IHubLogger  instance = ConsoleLogger;

        private static readonly HubLoggerConsole ConsoleLogger = new HubLoggerConsole();

        public void Log(HubLog hubLog, string message, Exception exception = null) {
            instance.Log(hubLog, message, exception);
        }
    }
    
    // -------------------------------- HubLoggerConsole --------------------------------
    internal class HubLoggerConsole : IHubLogger
    {
        public void Log(HubLog hubLog, string message, Exception exception) {
            var prefix          = GetLogPrefix(hubLog);
            var exceptionStr    = exception == null ? "" : exception.ToString(); 
            Console.WriteLine($"{prefix}{message}{exceptionStr}");
        }
        
        private static string GetLogPrefix(HubLog hubLog) {
            switch (hubLog) {
                case HubLog.Error:  return "error: ";
                case HubLog.Info:   return "info:  ";
                default:            return "error: ";
            }
        }
    }
    
#if UNITY_5_3_OR_NEWER
    // -------------------------------- HubLoggerUnity --------------------------------
    internal class HubLoggerUnity : IHubLogger
    {
        public void Log(HubLog hubLog, string message, Exception exception) {
            var fullMessage     = exception == null ? message : $"{message}, exception: {exception}";
            switch (hubLog) {
                case HubLog.Error:
                    UnityEngine.Debug.LogError(fullMessage);
                    break;                    
                case HubLog.Info:
                    UnityEngine.Debug.Log(fullMessage);
                    break;
            }
        }
    }
#endif
}
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
        void        Log  (HubLog hubLog, string message, Exception exception = null);
    }
    
    /// <summary> Used unify log interface and identify all classes generating logs </summary>
    internal interface ILogSource {
        IHubLogger  Logger { get; }
    }
    
    // ----------------------------------- HubLogger -----------------------------------
    internal sealed class HubLogger : IHubLogger
    {
        internal    IHubLogger  instance = ConsoleLogger;

        private static readonly HubLoggerConsole ConsoleLogger = new HubLoggerConsole();

        public void Log(HubLog hubLog, string message, Exception exception) {
            instance.Log(hubLog, message, exception);
        }
    }
    
    // -------------------------------- HubLoggerConsole --------------------------------
    internal sealed class HubLoggerConsole : IHubLogger
    {
        public void Log(HubLog hubLog, string message, Exception exception) {
            var prefix  = GetLogPrefix(hubLog);
            var msg     = exception == null ?
                $"{prefix}{message}" :
                $"{prefix}{message} {exception}";
            Console.WriteLine(msg);
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
    public sealed class HubLoggerUnity : IHubLogger
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
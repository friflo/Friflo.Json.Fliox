// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Fliox.Hub
{
    public interface IHubLogger
    {
        void Log  (HubLog hubLog, string message);
    }
    
    public enum HubLog
    {
        Error,
        Info
    }
    
    internal class HubLoggerConsole : IHubLogger
    {
        public void Log(HubLog hubLog, string message) {
            Console.Error.WriteLine($"error: {message}");
        }
    }
    
    public class HubLogger : IHubLogger
    {
        internal    IHubLogger  instance = ConsoleLogger;

        private static readonly HubLoggerConsole ConsoleLogger = new HubLoggerConsole();

        public void Log(HubLog hubLog, string message) {
            instance.Log(hubLog, message);
        }
    } 
}
// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_2020_1_OR_NEWER

using System;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Friflo.Json.Fliox.Hub.AspNetCore
{
    public sealed class HubLoggerAspNetCore : IHubLogger
    {
        private readonly ILogger logger;
        
        public HubLoggerAspNetCore(ILoggerFactory loggerFactory) {
            logger = loggerFactory.CreateLogger<HubLoggerAspNetCore>();
        }
        
        public void Log(HubLog hubLog, string message, Exception exception) {
            var logLevel = GetLogLevel(hubLog);
            logger.Log(logLevel, exception, message);
        }
        
        public void Log(HubLog hubLog, StringBuilder message, Exception exception) {
            var logLevel = GetLogLevel(hubLog);
            logger.Log(logLevel, exception, message.ToString());
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

#endif
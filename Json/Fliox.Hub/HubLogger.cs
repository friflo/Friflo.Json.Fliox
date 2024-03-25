// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;

namespace Friflo.Json.Fliox.Hub
{
    public enum HubLog {
        Error   = 1,
        Info    = 2
    }

    /// <summary>
    /// Implementations of <see cref="IHubLogger"/> must be thread safe
    /// </summary>
    public interface IHubLogger {
        void        Log  (HubLog hubLog, string        message, Exception exception = null);
        void        Log  (HubLog hubLog, StringBuilder message, Exception exception = null);
    }
    
    /// <summary> Used unify log interface and identify all classes generating logs </summary>
    public interface ILogSource {
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
        
        public void Log(HubLog hubLog, StringBuilder message, Exception exception) {
            instance.Log(hubLog, message, exception);
        }
        
        internal static string GetLogPrefix(HubLog hubLog) {
            switch (hubLog) {
                case HubLog.Error:  return "error: ";
                case HubLog.Info:   return "info:  ";
                default:            return "error: ";
            }
        }
    }
    
    // -------------------------------- HubLoggerConsole --------------------------------
    internal sealed class HubLoggerConsole : IHubLogger
    {
        public void Log(HubLog hubLog, string message, Exception exception) {
            var prefix  = HubLogger.GetLogPrefix(hubLog);
            var msg     = exception == null ?
                $"{prefix}{message}" :
                $"{prefix}{message} {exception}";
            Console.WriteLine(msg);
        }
        
        public void Log(HubLog hubLog, StringBuilder message, Exception exception) {
            Log(hubLog, message.ToString(), exception);
        }
    }
    
    // -------------------------------- HubLoggerNull --------------------------------
    public sealed class HubLoggerNull : IHubLogger
    {
        public void Log(HubLog hubLog, string        message, Exception exception) { }
        public void Log(HubLog hubLog, StringBuilder message, Exception exception) { }
    }
    
    // -------------------------------- HubLoggerStream --------------------------------
    public sealed class HubLoggerStream : IHubLogger
    {
        private readonly TextWriter writer;
        
        public HubLoggerStream(string filePath, Encoding encoding = null) {
            var fileStream  = new FileStream(filePath, FileMode.Append, FileAccess.Write);
            writer          = new StreamWriter(fileStream, encoding ?? Encoding.UTF8);
        }
        
        public HubLoggerStream(Stream stream, Encoding encoding = null) {
            writer          = new StreamWriter(stream, encoding ?? Encoding.UTF8);
        }
        
        public void Close() {
            lock (writer) {
                writer.Close();
            }
        }
            
        public void Log(HubLog hubLog, string message, Exception exception) {
            var prefix  = HubLogger.GetLogPrefix(hubLog);
            lock (writer) {
                writer.Write(prefix);
                writer.Write(message);
                if (exception != null) {
                    writer.Write(" ");
                    writer.Write(exception);
                }
                writer.WriteLine();
                writer.Flush();
            }
        }
        
        public void Log(HubLog hubLog, StringBuilder message, Exception exception) {
            var prefix  = HubLogger.GetLogPrefix(hubLog);
            lock (writer) {
                writer.Write(prefix);
                writer.Write(message);
                if (exception != null) {
                    writer.Write(" ");
                    writer.Write(exception);
                }
                writer.WriteLine();
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

        public void Log(HubLog hubLog, StringBuilder message, Exception exception) {
            Log(hubLog, message.ToString(), exception);
        }
    }
#endif
}
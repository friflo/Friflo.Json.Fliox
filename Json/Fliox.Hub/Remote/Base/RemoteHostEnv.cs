// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Host.Event;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote
{
    public class RemoteHostEnv
    {
        public   readonly   HostMetrics     metrics = new HostMetrics();
        public              bool            useReaderPool;
        /// Only set to true for testing. It avoids an early out at <see cref="EventSubClient.SendEvents"/>
        public              bool            fakeOpenClosedSockets;
        public              bool            logMessages;
    }
    
    public sealed class HostMetrics {
        public  SocketMetrics    webSocket;
        public  SocketMetrics    udp;
    }
    
    /// <summary>
    /// Time values are the difference of two timestamps: endTime - startTime <br/>
    /// timestamps are used from <see cref="Stopwatch.GetTimestamp"/>
    /// </summary>
    public struct SocketMetrics {
        /// <summary> accumulated count of all received WebSocket messages </summary>
        public  int     receivedCount;
        /// <summary> accumulated read request time of all WebSocket's </summary>
        public  long    requestReadTime;
        /// <summary> accumulated request execution time of all WebSocket's </summary>
        public  long    requestExecuteTime;
    }
}
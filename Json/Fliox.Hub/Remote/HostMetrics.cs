// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Friflo.Json.Fliox.Hub.Remote
{
    public sealed class HostMetrics {
        public  WebSocketMetrics    webSocket;
    }
    
    /// <summary>
    /// Time values are the difference of two timestamps: endTime - startTime <br/>
    /// timestamps are used from <see cref="Stopwatch.GetTimestamp"/>
    /// </summary>
    public struct WebSocketMetrics {
        /// <summary> accumulated count of all received WebSocket messages </summary>
        public  int     receivedCount;
        /// <summary> accumulated read request time of all WebSocket's </summary>
        public  long    requestReadTime;
        /// <summary> accumulated request execution time of all WebSocket's </summary>
        public  long    requestExecuteTime;
    }
}
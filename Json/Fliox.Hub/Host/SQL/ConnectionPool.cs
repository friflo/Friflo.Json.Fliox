// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Timers;

namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    /// <summary>
    /// Idle connections are closed and removed from <see cref="ConnectionPool{T}"/> after <see cref="IdleTimeout"/> ms.  
    /// </summary>
    public sealed class ConnectionPool<T> where T : ISyncConnection
    {
        /// <summary>
        /// oldest connection 
        /// </summary>
        private  readonly   Queue<Connection<T>>    connectionPool;
        private  readonly   Timer                   closeTimer;
        private  const      long                    IdleTimeout = 5000; // ms

        public ConnectionPool() {
            connectionPool          = new Queue<Connection<T>>();
            closeTimer              = new Timer();
            closeTimer.AutoReset    = true;
            closeTimer.Interval     = 1000; // ms
            closeTimer.Elapsed     += OnCloseTimerEvent;
        }
        
        private void OnCloseTimerEvent(object source, ElapsedEventArgs e) {
            var now = DateTime.Now;
            lock (connectionPool) {
                while (connectionPool.Count > 0) {
                    // Peek() - returns a connection from the beginning of the queue
                    var connection = connectionPool.Peek();
                    var diff = now - connection.idleStart;
                    if (diff.TotalMilliseconds < IdleTimeout) {
                        return;
                    }
                    connection.instance.Dispose();
                    connectionPool.Dequeue();
                }
                closeTimer.Stop();
            }
        }
        
        public bool TryPop(out T syncConnection) {
            lock (connectionPool) {
                while (true) {
                    // TryDequeue() - removed connection from beginning of the queue
                    if (connectionPool.Count == 0) {
                        syncConnection = default;
                        return false;
                    }
                    var connection = connectionPool.Dequeue();
                    syncConnection = connection.instance;
                    if (syncConnection.IsOpen) {
                        return true;
                    }
                }
            }
        }
        
        public void Push(ISyncConnection syncConnection) {
            var connection = new Connection<T>((T)syncConnection);
            lock (connectionPool) {
                closeTimer.Start();
                // Enqueue() - add connection the the end of the queue 
                connectionPool.Enqueue(connection);    
            }
        }
    }
    
    internal readonly struct Connection<T> where T : ISyncConnection
    {
        internal readonly T         instance;
        internal readonly DateTime  idleStart;
        
        internal Connection(T instance) {
            this.instance   = instance;
            this.idleStart  = DateTime.Now;
        }
    }
}
// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    public sealed class ConnectionPool<T> where T : ISyncConnection
    {
        private  readonly   ConcurrentStack<T> connectionPool = new ConcurrentStack<T>();
        
        public bool TryPop(out T syncConnection) {
            while (true) {
                if (!connectionPool.TryPop(out syncConnection)) {
                    return false;
                }
                if (syncConnection.IsOpen) {
                    return true;
                }
            }
        }
        
        public void Push(ISyncConnection syncConnection) {
            connectionPool.Push((T)syncConnection);
        }
    }
}
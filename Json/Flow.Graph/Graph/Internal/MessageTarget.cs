// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Sync;


namespace Friflo.Json.Flow.Graph.Internal
{
    public class MessageTarget : IMessageTarget
    {
        readonly EntityStore store;
        
        internal MessageTarget (EntityStore store) {
            this.store = store; 
        } 
            
        // --- IMessageTarget 
        public async Task<bool> SendMessage(PushMessage message, SyncContext syncContext) {
            return true;
        }
        
        public bool IsOpen () {
            return true;
        }
    }
}
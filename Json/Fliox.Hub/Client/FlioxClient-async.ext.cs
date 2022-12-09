// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Utils;

namespace Friflo.Json.Fliox.Hub.Client
{
    public partial class FlioxClient
    {
        /// <summary> Specific characteristic: Method can run in parallel on any thread </summary>
        private async Task<SyncResult> TrySyncAcknowledgeEvents() {
            // cannot reuse request, context & buffer method can run on any thread
            var syncRequest = new SyncRequest { tasks = new List<SyncRequestTask>() };
            var buffer      = new MemoryBuffer(MemoryBufferCapacity);
            var syncContext = new SyncContext(_intern.sharedEnv, _intern.eventReceiver); 
            syncContext.SetMemoryBuffer(buffer);
            syncContext.clientId = _intern.clientId;
            
            InitSyncRequest(syncRequest);
            var response    = await ExecuteRequestAsync(syncRequest, syncContext).ConfigureAwait(false);

            var syncStore   = new SyncStore();  // create default (empty) SyncStore
            return HandleSyncResponse(syncRequest, response, syncStore, buffer);
        }
        
        /// <summary> Cancel execution of pending calls to <see cref="SyncTasks"/> and <see cref="TrySyncTasks"/> </summary>
        public async Task CancelPendingSyncs() {
            List<SyncContext>   pendingSyncs;
            List<Task>          pendingTasks;
            lock (_intern.pendingSyncs) {
                var count       = _intern.pendingSyncs.Count;
                pendingSyncs    = new List<SyncContext> (count);
                pendingTasks    = new List<Task>        (count);
                foreach (var pair in _intern.pendingSyncs) {
                    pendingSyncs.Add(pair.Value);
                    pendingTasks.Add(pair.Key);
                }
            }
            foreach (var sync in pendingSyncs) {
                sync.Cancel();
            }
            await Task.WhenAll(pendingTasks).ConfigureAwait(false);
        }
    }
}
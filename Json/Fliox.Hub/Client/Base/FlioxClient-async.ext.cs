// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Utils;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    public partial class FlioxClient
    {
        /// <summary> Specific characteristic: Method can run in parallel on any thread </summary>
        private async Task<SyncResult> TrySyncAcknowledgeEvents() {
            // cannot reuse request, context & buffer method can run on any thread
            var syncRequest = new SyncRequest { tasks = new ListOne<SyncRequestTask>() };
            var buffer      = new MemoryBuffer(Static.MemoryBufferCapacity);
            var syncContext = new SyncContext(_readonly.sharedEnv, options.eventReceiver); 
            syncContext.SetMemoryBuffer(buffer);
            syncContext.clientId = _intern.clientId;
            syncRequest.intern.executeSync = false;
            InitSyncRequest(syncRequest);
            var response    = await ExecuteAsync(syncRequest, syncContext, ExecutionType.Async).ConfigureAwait(false);

            var syncStore   = new SyncStore();  // create default (empty) SyncStore
            return HandleSyncResponse(syncRequest, response, syncStore, buffer);
        }
        
        /// <summary> Cancel execution of pending calls to <see cref="SyncTasks"/> and <see cref="TrySyncTasks"/> </summary>
        public async Task CancelPendingSyncs() {
            List<SyncContext>   pendingSyncs;
            List<Task>          pendingTasks;
            lock (_readonly.pendingSyncs) {
                var count       = _readonly.pendingSyncs.Count;
                pendingSyncs    = new List<SyncContext> (count);
                pendingTasks    = new List<Task>        (count);
                foreach (var pair in _readonly.pendingSyncs) {
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
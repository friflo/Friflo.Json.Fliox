// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;

namespace Friflo.Json.Fliox.Hub.Client
{
    public partial class FlioxClient
    {
        /// <summary> Execute all tasks created by methods of <see cref="EntitySet{TKey,T}"/> and <see cref="FlioxClient"/> </summary>
        /// <remarks>
        /// In case any task failed a <see cref="SyncTasksException"/> is thrown. <br/>
        /// As an alternative use <see cref="TrySyncTasks"/> to execute tasks which does not throw an exception. <br/>
        /// The method can be called without awaiting the result of a previous call. </remarks>
        public async Task<SyncResult> SyncTasks() {
            var syncRequest = CreateSyncRequest(out SyncStore syncStore);
            var buffer      = CreateMemoryBuffer();
            var syncContext = new SyncContext(_intern.sharedEnv, _intern.eventReceiver, buffer, _intern.clientId);
            var response    = await ExecuteRequestAsync(syncRequest, syncContext).ConfigureAwait(Static.OriginalContext);
            
            var result      = HandleSyncResponse(syncRequest, response, syncStore);
            if (!result.Success)
                throw new SyncTasksException(response.error, result.failed);
            syncContext.Release();
            return result;
        }
        
        /// <summary> Execute all tasks created by methods of <see cref="EntitySet{TKey,T}"/> and <see cref="FlioxClient"/> </summary>
        /// <remarks>
        /// Failed tasks are available via the returned <see cref="SyncResult"/> in the field <see cref="SyncResult.failed"/> <br/>
        /// In performance critical application this method should be used instead of <see cref="SyncTasks"/> as throwing exceptions is expensive. <br/> 
        /// The method can be called without awaiting the result of a previous call. </remarks>
        public async Task<SyncResult> TrySyncTasks() {
            var syncRequest = CreateSyncRequest(out SyncStore syncStore);
            var buffer      = CreateMemoryBuffer();
            var syncContext = new SyncContext(_intern.sharedEnv, _intern.eventReceiver, buffer, _intern.clientId);
            var response    = await ExecuteRequestAsync(syncRequest, syncContext).ConfigureAwait(Static.OriginalContext);

            var result      = HandleSyncResponse(syncRequest, response, syncStore);
            syncContext.Release();
            return result;
        }
        
        private async Task<ExecuteSyncResult> ExecuteRequestAsync(SyncRequest syncRequest, SyncContext syncContext) {
            _intern.syncCount++;
            if (_intern.ackTimerPending) {
                _intern.ackTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                _intern.ackTimerPending = false;
            }
            Task<ExecuteSyncResult> task = null;
            try {
                task = _intern.hub.ExecuteRequestAsync(syncRequest, syncContext);
                lock (_intern.pendingSyncs) {
                    _intern.pendingSyncs.Add(task, syncContext);
                }
                var response = await task.ConfigureAwait(false);
                
                // The Hub returns a client id if the client didn't provide one and one of its task require one. 
                var success = response.success;
                if (_intern.clientId.IsNull() && success != null && !success.clientId.IsNull()) {
                    SetClientId(success.clientId);
                }
                lock (_intern.pendingSyncs) {
                    _intern.pendingSyncs.Remove(task);
                }
                return response;
            }
            catch (Exception e) {
                lock (_intern.pendingSyncs) {
                    if (task != null) _intern.pendingSyncs.Remove(task);
                }
                var errorMsg = ErrorResponse.ErrorFromException(e).ToString();
                return new ExecuteSyncResult(errorMsg, ErrorResponseType.Exception);
            }
        }
    }
}
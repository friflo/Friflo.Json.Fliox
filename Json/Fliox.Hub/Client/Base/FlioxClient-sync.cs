// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading;
// using System.Threading.Tasks; intentionally not used in sync version
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using static Friflo.Json.Fliox.Hub.Host.ExecutionType;

// Note!  Keep file in sync with:  FlioxClient-async.cs

//
//
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    public partial class FlioxClient
    {
        /// <summary> Execute all tasks created by methods of <see cref="EntitySet{TKey,T}"/> and <see cref="FlioxClient"/> </summary>
        /// <remarks>
        /// In case any task failed a <see cref="SyncTasksException"/> is thrown. <br/>
        /// As an alternative use <see cref="TrySyncTasks"/> to execute tasks which does not throw an exception. <br/>
        /// The method can be called without awaiting the result of a previous call. </remarks>
        public SyncResult SyncTasksSynchronous() {
            var hub             = _readonly.hub;
            var syncRequest     = CreateSyncRequest(out SyncStore syncStore);
            var buffer          = CreateMemoryBuffer();
            var syncContext     = CreateSyncContext(buffer);
            syncRequest.intern.executeSync = true;
            var executionType   = hub.InitSyncRequest(syncRequest);
            ExecuteSyncResult syncResult;
            switch (executionType) {
                case Queue: syncResult = Execute (syncRequest, syncContext);                break;
                case Async: throw new InvalidOperationException ("async execution required");
                default:    syncResult = Execute (syncRequest, syncContext);                break;
            }
            var result      = HandleSyncResponse(syncRequest, syncResult, syncStore, buffer);
            ReuseSyncContext(syncContext, syncRequest);
            if (!result.Success) {
                throw new SyncTasksException(syncResult.error, result.failed);
            }
            return result;
        }
        
        /// <summary> Execute all tasks created by methods of <see cref="EntitySet{TKey,T}"/> and <see cref="FlioxClient"/> </summary>
        /// <remarks>
        /// Failed tasks are available via the returned <see cref="SyncResult"/> in the field <see cref="SyncResult.failed"/> <br/>
        /// In performance critical application this method should be used instead of <see cref="SyncTasks"/> as throwing exceptions is expensive. <br/> 
        /// The method can be called without awaiting the result of a previous call. </remarks>
        public SyncResult TrySyncTasksSynchronous() {
            var hub             = _readonly.hub;
            var syncRequest     = CreateSyncRequest(out SyncStore syncStore);
            var buffer          = CreateMemoryBuffer();
            var syncContext     = CreateSyncContext(buffer);
            syncRequest.intern.executeSync = true;
            var executionType   = hub.InitSyncRequest(syncRequest);
            ExecuteSyncResult syncResult;
            switch (executionType) {
                case Queue: syncResult = Execute (syncRequest, syncContext);            break;
                case Async: throw new InvalidOperationException ("async execution required");
                default:    syncResult = Execute (syncRequest, syncContext);            break;
            }
            var result = HandleSyncResponse(syncRequest, syncResult, syncStore, buffer);
            ReuseSyncContext(syncContext, syncRequest);
            return result;
        }
        
        private ExecuteSyncResult Execute(SyncRequest syncRequest, SyncContext syncContext) {
            _intern.syncCount++;
            if (_intern.ackTimerPending) {
                _intern.ackTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                _intern.ackTimerPending = false;
            }
            // Task<ExecuteSyncResult> task = null; not required
            try {
                //
                var response = _readonly.hub.ExecuteRequest(syncRequest, syncContext);
                //
                //
                //
                // add to pendingSyncs for counting and canceling
                // lock (_intern.pendingSyncs) not required
                //
                //
                //
                
                // The Hub returns a client id if the client didn't provide one and one of its task require one. 
                var success = response.success;
                if (_intern.clientId.IsNull() && success != null && !success.clientId.IsNull()) {
                    SetClientId(success.clientId);
                }
                // lock (_intern.pendingSyncs) not required
                //
                //
                return response;
            }
            catch (Exception e) {
                // lock (_intern.pendingSyncs) not required
                //
                //
                var errorMsg = ErrorResponse.ErrorFromException(e).ToString();
                return new ExecuteSyncResult(errorMsg, ErrorResponseType.Exception);
            }
        }
    }
}
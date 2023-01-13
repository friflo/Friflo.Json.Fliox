// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Utils;
using static Friflo.Json.Fliox.Hub.Host.ExecutionType;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
namespace Friflo.Json.Fliox.Hub.Remote
{
    /// <summary>
    /// <see cref="WebHostHandler"/> provide a set of methods to:<br/>
    /// - parse serialized <see cref="SyncRequest"/> messages.<br/>
    /// - execute <see cref="SyncRequest"/>'s and send serialized <see cref="SyncResponse"/> to client<br/>
    /// </summary>
    /// <remarks>
    /// The implementation aims to prevent <b>head-of-line blocking</b>.<br/>
    /// Which means it supports <b>out-of-order delivery</b> for responses send to clients.<br/>
    /// If enabling queueing request in <see cref="DatabaseService"/> <b>head-of-line blocking</b> can occur
    /// in case request execution is not 'instant'<br/>
    /// <br/>
    /// Requests can be executed synchronous, asynchronous or on a specific thread / execution context.<br/>
    /// - If <see cref="DatabaseService"/> is configured to queue requests they are executed
    ///   on the thread calling <see cref="DatabaseService.ExecuteQueuedRequestsAsync"/> <br/>
    /// - Synchronous in case a request can be executed synchronous<br/>
    /// - Asynchronous in case a request requires asynchronous execution<br/>
    /// </remarks>
    public abstract class WebHostHandler : EventReceiver, ILogSource
    {
        private   readonly  FlioxHub    hub;
        protected readonly  TypeStore   typeStore;
        protected readonly  SharedEnv   sharedEnv;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public              IHubLogger  Logger { get; }
        protected abstract  void        SendMessage(in JsonValue message);

        protected WebHostHandler(RemoteHost remoteHost) {
            var env     = remoteHost.sharedEnv;
            sharedEnv   = env;
            typeStore   = sharedEnv.TypeStore;
            hub         = remoteHost.localHub;
            Logger      = remoteHost.Logger;
        }

        public override void SendEvent(in ClientEvent clientEvent) {
            try {
                SendMessage(clientEvent.message);
            }
            catch (Exception e) {
                Logger.Log(HubLog.Error, "WebSocketHost.SendEvent", e);
            }
        }
        
        protected SyncRequest ParseRequest(in JsonValue request, ObjectReader reader, ObjectWriter writer) {
            var syncRequest = RemoteUtils.ReadSyncRequest(reader, request, out string error);
            if (error == null) {
                return syncRequest;
            }
            var response = JsonResponse.CreateError(writer, error, ErrorResponseType.BadRequest, null);
            SendMessage(response.body);
            return null;
        }
        
        protected void ExecuteRequest(SyncRequest syncRequest, ObjectWriter writer)
        {
            // todo optimize: pool SyncContext
            var syncPools       = new SyncPools(typeStore);
            var syncBuffers     = new SyncBuffers(new List<SyncRequestTask>(), new List<SyncRequestTask>(), new List<JsonValue>());
            var syncContext     = new SyncContext(sharedEnv, this, syncBuffers, syncPools); // reused context
            var memoryBuffer    = new MemoryBuffer(4 * 1024);

            syncContext.Init();
            syncContext.SetMemoryBuffer(memoryBuffer);
            var reqId = syncRequest.reqId;
            try {
                var executionType = hub.InitSyncRequest(syncRequest);
                Task<ExecuteSyncResult> syncResultTask;
                switch (executionType) {
                    case Async: syncResultTask  = hub.ExecuteRequestAsync (syncRequest, syncContext); break;
                    case Queue: syncResultTask  = hub.QueueRequestAsync   (syncRequest, syncContext); break;
                    default:
                        var syncResult          = hub.ExecuteRequest      (syncRequest, syncContext);
                        SendResponse(syncResult, reqId, writer);
                        return;
                }
                syncResultTask.ContinueWith(task => {
                    SyncResultContinuation(task, reqId, writer);
                });
            }
            catch (Exception e) {
                SendResponseException(e, reqId, writer);
            }
        }
        
        private void SyncResultContinuation(Task<ExecuteSyncResult> task, int? reqId, ObjectWriter writer) {
            var status = task.Status;
            switch (status) {
                case TaskStatus.RanToCompletion:
                    var syncResult = task.Result;
                    SendResponse(syncResult, reqId, writer);
                    return;
                case TaskStatus.Faulted:
                    var exception = task.Exception;
                    SendResponseException(exception, reqId, writer);
                    return;
                case TaskStatus.Canceled:
                    var canceledException = task.Exception; // OperationCanceledException
                    SendResponseException(canceledException, reqId, writer);
                    return;
                default:
                    var errorMsg = $"unexpected continuation task status {status}, reqId: {reqId}";
                    Logger.Log(HubLog.Error, errorMsg);
                    Debug.Fail(errorMsg);
                    return;
            }
        }
        
        private void SendResponse(in ExecuteSyncResult syncResult, int? reqId, ObjectWriter writer) {
            var error = syncResult.error;
            if (error != null) {
                var errorResponse = JsonResponse.CreateError(writer, error.message, error.type, reqId);
                SendMessage(errorResponse.body);
                return;
            }
            var response = RemoteHost.CreateJsonResponse(syncResult, reqId, writer);
            SendMessage(response.body);
        }
        
        protected void SendResponseException(Exception e, int? reqId, ObjectWriter writer) {
            var errorMsg = ErrorResponse.ErrorFromException(e).ToString();
            var response = JsonResponse.CreateError(writer, errorMsg, ErrorResponseType.Exception, reqId);
            SendMessage(response.body);
        }
    }
}
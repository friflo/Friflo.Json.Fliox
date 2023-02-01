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
using Friflo.Json.Fliox.Pools;
using Friflo.Json.Fliox.Utils;
using static Friflo.Json.Fliox.Hub.Host.ExecutionType;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote
{
    /// <summary>
    /// Counterpart of <see cref="SocketClientHub"/> used by socket implementations running on the server.<br/>
    /// <br/>
    /// <see cref="SocketHost"/> provide a set of methods to:<br/>
    /// - parse serialized <see cref="SyncRequest"/> messages.<br/>
    /// - execute <see cref="SyncRequest"/>'s and send serialized <see cref="SyncResponse"/> to client<br/>
    /// </summary>
    /// <remarks>
    /// The implementation aims to prevent <b>head-of-line blocking</b>.<br/>
    /// Which means it supports <b>out-of-order delivery</b> for responses send to clients.<br/>
    /// <br/>
    /// <b>Special case</b><br/>
    /// If enabling queueing request in <see cref="DatabaseService"/>
    /// <b>head-of-line blocking</b> can occur in case request execution is not 'instant'<br/>
    /// <br/>
    /// Requests can be executed synchronous, asynchronous or on a specific thread / execution context.<br/>
    /// - If <see cref="DatabaseService"/> is configured to queue requests they are executed
    ///   on the thread calling <see cref="DatabaseService.queue"/> <br/>
    /// - Synchronous in case a request can be executed synchronous<br/>
    /// - Asynchronous in case a request requires asynchronous execution<br/>
    /// </remarks>
    public abstract class SocketHost : EventReceiver, ILogSource
    {
        private   readonly  FlioxHub                    hub;
        private   readonly  TypeStore                   typeStore;
        private   readonly  SharedEnv                   sharedEnv;
        private   readonly  ObjectPool<ReaderPool>      readerPool;
        private   readonly  ObjectMapper                readMapper;
        private   readonly  ObjectPool<ObjectMapper>    objectPool;
        private   readonly  bool                        useReaderPool;
        private   readonly  Stack<SyncContext>          syncContextPool;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public              IHubLogger  Logger { get; }
        protected abstract  void        SendMessage(in JsonValue message, in SocketContext socketContext);

        protected SocketHost(FlioxHub hub, HostEnv hostEnv) {
            var env         = hub.sharedEnv;
            sharedEnv       = env;
            typeStore       = sharedEnv.TypeStore;
            this.hub        = hub;
            Logger          = hub.Logger;
            var pool        = sharedEnv.Pool;
            readerPool      = pool.ReaderPool;
            objectPool      = pool.ObjectMapper;
            readMapper      = objectPool.Get().instance;
            useReaderPool   = hostEnv.useReaderPool;
            syncContextPool = new Stack<SyncContext>();
        }

        public override void SendEvent(in ClientEvent clientEvent) {
            try {
                SendMessage(clientEvent.message, default);
            }
            catch (Exception e) {
                Logger.Log(HubLog.Error, "WebSocketHost.SendEvent", e);
            }
        }
        
        protected SyncRequest ParseRequest(in JsonValue request, in SocketContext socketContext) {
            var reader = readMapper.reader;
            if (useReaderPool) {
                reader.ReaderPool = readerPool.Get().instance.Reuse();
            }
            var syncRequest = RemoteUtils.ReadSyncRequest(reader, request, out string error);
            if (error == null) {
                return syncRequest;
            }
            var response = JsonResponse.CreateError(readMapper.writer, error, ErrorResponseType.BadRequest, null);
            SendMessage(response.body, socketContext);
            return null;
        }
        
        /// <summary>create or use a pooled <see cref="SyncContext"/></summary>
        private SyncContext CreateSyncContext() {
            SyncContext syncContext;
            lock (syncContextPool) {
                if (syncContextPool.TryPop(out syncContext)) {
                    return syncContext;
                }
            }
            var syncPools       = new SyncPools(typeStore);
            var syncBuffers     = new SyncBuffers(new List<SyncRequestTask>(), new List<SyncRequestTask>(), new List<JsonValue>());
            var memoryBuffer    = new MemoryBuffer(4 * 1024);
            syncContext         = new SyncContext(sharedEnv, this, syncBuffers, syncPools); // reused context
            syncContext.SetMemoryBuffer(memoryBuffer);
            return syncContext;
        }
        
        /// <summary>
        /// <b>Note</b> <br/>
        /// Return <see cref="SyncContext"/> to pool after calling <see cref="SendResponse"/> or <see cref="SendResponseException"/>
        /// as <see cref="SyncContext.MemoryBuffer"/> could be used when writing <see cref="JsonResponse"/>.
        /// </summary>
        private void ReturnSyncContext(SyncContext syncContext) {
            var memoryBuffer = syncContext.MemoryBuffer;
            syncContext.Init();
            syncContext.SetMemoryBuffer(memoryBuffer);
            lock (syncContextPool) {
                syncContextPool.Push(syncContext);
            }
        }
        
        protected void ExecuteRequest(SyncRequest syncRequest, SocketContext socketContext)
        {
            var syncContext = CreateSyncContext();

            syncContext.MemoryBuffer.Reset();
            var reqId   = syncRequest.reqId;
            var pool    = readMapper.reader.ReaderPool;
            try {
                var executionType = hub.InitSyncRequest(syncRequest);
                Task<ExecuteSyncResult> syncResultTask;
                switch (executionType) {
                    case Async: syncResultTask  = hub.ExecuteRequestAsync (syncRequest, syncContext); break;
                    case Queue: syncResultTask  = hub.QueueRequestAsync   (syncRequest, syncContext); break;
                    default:
                        var syncResult          = hub.ExecuteRequest      (syncRequest, syncContext);
                        if (pool != null) readerPool.Return(pool);
                        SendResponse(syncResult, reqId, socketContext);
                        ReturnSyncContext(syncContext);
                        return;
                }
                syncResultTask.ContinueWith(task => {
                    if (pool != null) readerPool.Return(pool);
                    SyncResultContinuation(task, reqId, socketContext);
                    ReturnSyncContext(syncContext);
                });
            }
            catch (Exception e) {
                SendResponseException(e, reqId, socketContext);
                ReturnSyncContext(syncContext);
            }
        }
        
        private void SyncResultContinuation(Task<ExecuteSyncResult> task, int? reqId, in SocketContext socketContext) {
            var status = task.Status;
            switch (status) {
                case TaskStatus.RanToCompletion:
                    var syncResult = task.Result;
                    SendResponse(syncResult, reqId, socketContext);
                    return;
                case TaskStatus.Faulted:
                    var exception = task.Exception;
                    SendResponseException(exception, reqId, socketContext);
                    return;
                case TaskStatus.Canceled:
                    var canceledException = task.Exception; // OperationCanceledException
                    SendResponseException(canceledException, reqId, socketContext);
                    return;
                default:
                    var errorMsg = $"unexpected continuation task status {status}, reqId: {reqId}";
                    Logger.Log(HubLog.Error, errorMsg);
                    Debug.Fail(errorMsg);
                    return;
            }
        }
        
        private void SendResponse(in ExecuteSyncResult syncResult, int? reqId, in SocketContext socketContext) {
            var error               = syncResult.error;
            var mapper              = objectPool.Get().instance;
            var writer              = mapper.writer;
            writer.Pretty           = false;
            writer.WriteNullMembers = false;
            if (error != null) {
                var errorResponse = JsonResponse.CreateError(writer, error.message, error.type, reqId);
                SendMessage(errorResponse.body, socketContext);
            } else {
                var response = RemoteHost.CreateJsonResponse(syncResult, reqId, writer);
                SendMessage(response.body, socketContext);
            }
            objectPool.Return(mapper);
        }
        
        protected void SendResponseException(Exception e, int? reqId, in SocketContext socketContext) {
            var errorMsg    = ErrorResponse.ErrorFromException(e).ToString();
            var mapper      = objectPool.Get().instance;
            var response    = JsonResponse.CreateError(mapper.writer, errorMsg, ErrorResponseType.Exception, reqId);
            objectPool.Return(mapper);
            SendMessage(response.body, socketContext);
        }
    }
    
    /// <summary>
    /// Used by <see cref="UdpSocketHost"/> to send a response to the <see cref="remoteEndPoint"/>
    /// which made a request.<br/>
    /// It is not required by <see cref="WebSocketHost"/> as the remote endpoint is implicit in the used WebSocket.
    /// </summary>
    public readonly struct SocketContext
    {
        internal readonly System.Net.IPEndPoint remoteEndPoint;
        
        internal SocketContext(System.Net.IPEndPoint remoteEndPoint) {
            this.remoteEndPoint = remoteEndPoint;
        }
    }
}
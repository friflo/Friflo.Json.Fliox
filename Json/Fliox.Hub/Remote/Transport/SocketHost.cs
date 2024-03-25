// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Remote.Tools;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Pools;
using Friflo.Json.Fliox.Utils;
using static Friflo.Json.Fliox.Hub.Host.ExecutionType;

// Note! - Must not have any dependency to System.Net, System.Net.Sockets, System.Net.WebSockets, System.Net.Http
//         or other specific communication implementations.

// ReSharper disable once CheckNamespace
// ReSharper disable ConvertToUsingDeclaration
namespace Friflo.Json.Fliox.Hub.Remote
{
    /// <summary>
    /// Counterpart of <see cref="SocketClientHub"/> used by socket implementations running on the server.<br/>
    /// <br/>
    /// <see cref="SocketHost"/> provide a set of methods to:<br/>
    /// - parse serialized <see cref="SyncRequest"/> messages.<br/>
    /// - execute <see cref="SyncRequest"/>'s and send serialized <see cref="SyncResponse"/> to client<br/>
    /// <br/>
    /// <see cref="SocketHost"/> is thread safe.
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
    public abstract class SocketHost : IEventReceiver
    {
        // --- all fields are thread safe types
        private   readonly  FlioxHub                    hub;
        private   readonly  IHost                       host;
        private   readonly  Pool                        pool;
        private   readonly  bool                        useReaderPool;
        private   readonly  Stack<SyncContext>          syncContextPool; // requires lock
        
        protected           IHubLogger                  Logger      => hub.Logger;
        
        protected abstract  void        SendMessage(in JsonValue message);

        protected SocketHost(FlioxHub hub, IHost host) {
            this.hub        = hub;
            this.host       = host;
            pool            = hub.sharedEnv.pool;
            useReaderPool   = hub.GetFeature<RemoteHostEnv>().useReaderPool;
            syncContextPool = new Stack<SyncContext>();
        }

        // --- IEventReceiver
        public abstract string Endpoint { get; }
        public abstract bool   IsOpen ();
        public abstract bool   IsRemoteTarget ();
        public void SendEvent(in ClientEvent clientEvent) {
            try {
                SendMessage(clientEvent.message);
            }
            catch (Exception e) {
                Logger.Log(HubLog.Error, "WebSocketHost.SendEvent", e);
            }
        }
        
        private SyncRequest ParseRequest(ObjectMapper mapper, in JsonValue request) {
            var syncRequest = MessageUtils.ReadSyncRequest(mapper.reader, hub.sharedEnv, request, out string error);
            if (error == null) {
                return syncRequest;
            }
            var response = JsonResponse.CreateError(mapper.writer, error, ErrorResponseType.BadRequest, null);
            SendMessage(response.body);
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
            var syncPools       = new SyncPools();
            var syncBuffers     = new SyncBuffers(new List<SyncRequestTask>(), new List<SyncRequestTask>(), new List<JsonValue>());
            var memoryBuffer    = new MemoryBuffer(4 * 1024);
            syncContext         = new SyncContext(hub.sharedEnv, this, syncBuffers, syncPools) { Host = host }; // reused context
            syncContext.SetMemoryBuffer(memoryBuffer);
            return syncContext;
        }
        
        /// <summary>
        /// <b>Note</b> <br/>
        /// Return <see cref="SyncContext"/> to syncContextPool after calling <see cref="SendResponse"/> or <see cref="SendResponseException"/>
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
        
        protected internal void OnReceive(in JsonValue request, ref SocketMetrics metrics)
        {
            ReaderPool readerPool = null; 
            if (useReaderPool) {
                readerPool = pool.ReaderPool.Get().instance.Reuse();
            }
            // --- precondition: message was read from socket
            try {
                // --- 1. Parse request
                Interlocked.Increment(ref metrics.receivedCount);
                var t1              = Stopwatch.GetTimestamp();
                SyncRequest syncRequest;
                using (var pooled = pool.ObjectMapper.Get()) {
                    var mapper                  = pooled.instance;
                    mapper.reader.ReaderPool    = readerPool;
                    syncRequest = ParseRequest(mapper, request);
                }
                var t2              = Stopwatch.GetTimestamp();
                Interlocked.Add(ref metrics.requestReadTime, t2 - t1);
                if (syncRequest == null) {
                    return;
                }
                // --- 2. Execute request
                ExecuteRequest (syncRequest, readerPool);
                var t3              = Stopwatch.GetTimestamp();
                Interlocked.Add(ref metrics.requestExecuteTime, t3 - t2);
            }
            catch (Exception e) {
                SendResponseException(e, null);
            }
        }
        
        private void ExecuteRequest(SyncRequest syncRequest, ReaderPool readerPool)
        {
            var syncContext = CreateSyncContext();

            syncContext.MemoryBuffer.Reset();
            var reqId   = syncRequest.reqId;
            try {
                var executionType = hub.InitSyncRequest(syncRequest);
                Task<ExecuteSyncResult> syncResultTask;
                switch (executionType) {
                    case Async: syncResultTask  = hub.ExecuteRequestAsync (syncRequest, syncContext); break;
                    case Queue: syncResultTask  = hub.QueueRequestAsync   (syncRequest, syncContext); break;
                    default:
                        var syncResult          = hub.ExecuteRequest      (syncRequest, syncContext);
                        if (readerPool != null) pool.ReaderPool.Return(readerPool);
                        SendResponse(syncResult, reqId);
                        ReturnSyncContext(syncContext);
                        return;
                }
                syncResultTask.ContinueWith(task => {
                    if (readerPool != null) pool.ReaderPool.Return(readerPool);
                    SyncResultContinuation(task, reqId);
                    ReturnSyncContext(syncContext);
                });
            }
            catch (Exception e) {
                SendResponseException(e, reqId);
                ReturnSyncContext(syncContext);
            }
        }
        
        private void SyncResultContinuation(Task<ExecuteSyncResult> task, int? reqId) {
            var status = task.Status;
            switch (status) {
                case TaskStatus.RanToCompletion:
                    var syncResult = task.Result;
                    SendResponse(syncResult, reqId);
                    return;
                case TaskStatus.Faulted:
                    var exception = task.Exception;
                    SendResponseException(exception, reqId);
                    return;
                case TaskStatus.Canceled:
                    var canceledException = task.Exception; // OperationCanceledException
                    SendResponseException(canceledException, reqId);
                    return;
                default:
                    var errorMsg = $"unexpected continuation task status {status}, reqId: {reqId}";
                    Logger.Log(HubLog.Error, errorMsg);
                    Debug.Fail(errorMsg);
                    return;
            }
        }
        
        private void SendResponse(in ExecuteSyncResult syncResult, int? reqId) {
            var error   = syncResult.error;
            using (var pooled = pool.ObjectMapper.Get()) {
                var mapper  = pooled.instance;
                var writer  = MessageUtils.GetCompactWriter(mapper);
                if (error != null) {
                    var errorResponse = JsonResponse.CreateError(writer, error.message, error.type, reqId);
                    SendMessage(errorResponse.body);
                } else {
                    var response = RemoteHostUtils.CreateJsonResponse(syncResult, reqId, hub.sharedEnv, writer);
                    SendMessage(response.body);
                }
            }
        }
        
        private void SendResponseException(Exception e, int? reqId) {
            var errorMsg    = ErrorResponse.ErrorFromException(e).ToString();
            using (var pooled = pool.ObjectMapper.Get()) {
                var mapper      = pooled.instance;
                var response    = JsonResponse.CreateError(mapper.writer, errorMsg, ErrorResponseType.Exception, reqId);
                SendMessage(response.body);
            }
        }
    }
}
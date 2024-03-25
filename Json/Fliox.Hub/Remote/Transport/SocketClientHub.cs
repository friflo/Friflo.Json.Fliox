// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Remote.Tools;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Pools;
using Friflo.Json.Fliox.Utils;

// Note! - Must not have any dependency to System.Net, System.Net.Sockets, System.Net.WebSockets, System.Net.Http
//         or other specific communication implementations.

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote
{
    /// <summary>
    /// By default a remote client like <see cref="SocketClientHub"/> can be used by multiple <see cref="Client.FlioxClient"/>'s.<br/>
    /// <br/>
    /// To minimize the size of serialized <see cref="EventMessage"/>'s sent to subscribed clients the <see cref="EventDispatcher"/>
    /// can omit sending the target client id by settings <see cref="EventDispatcher.SendTargetClientId"/> to false.<br/>
    /// In this case all <see cref="SocketClientHub"/>'s must be initialized with <see cref="Single"/> enabling
    /// processing events by the single <see cref="Client.FlioxClient"/> using a <see cref="SocketClientHub"/>. 
    /// </summary>
    public enum RemoteClientAccess
    {
        /// <summary>Remote client can be used only by a single <see cref="Client.FlioxClient"/></summary>
        Single,
        /// <summary>Remote client can be used by multiple <see cref="Client.FlioxClient"/>'s</summary>
        Multi
    }
    
    [Flags]
    public enum ProtocolFeature
    {
        Duplicates = 1
    }
    
    /// <summary>
    /// Counterpart of <see cref="SocketHost"/> used by socket implementations running on clients.
    /// </summary>
    public abstract class SocketClientHub : FlioxHub
    {
        public              RemoteClientEnv                         ClientEnv { get => env; set => env = value ?? throw new ArgumentNullException(nameof(ClientEnv)); }
        
        private   readonly  Dictionary<ShortString, IEventReceiver> eventReceivers;
        private   readonly  ObjectPool<ReaderPool>                  responseReaderPool;
        private   readonly  ProtocolFeature                         protocolFeature;
        private   readonly  RemoteClientAccess                      access;
        private             Utf8JsonParser                          messageParser; // non thread-safe
        protected           RemoteClientEnv                         env     = new RemoteClientEnv();
        protected           StringBuilder                           sbSend;
        protected           StringBuilder                           sbRecv;

        protected SocketClientHub(
            EntityDatabase      database,
            SharedEnv           env,
            ProtocolFeature     protocolFeature,
            RemoteClientAccess  access = RemoteClientAccess.Multi)
            : base(database, env)
        {
            eventReceivers          = new Dictionary<ShortString, IEventReceiver>(ShortString.Equality);
            responseReaderPool      = new ObjectPool<ReaderPool>(() => new ReaderPool(sharedEnv.typeStore));
            this.protocolFeature    = protocolFeature;
            this.access             = access;     
        }

        public abstract bool IsConnected { get; }
        public abstract Task Close();
        
        /// <summary>
        /// A class extending  <see cref="SocketClientHub"/> must implement this method.<br/>
        /// Implementation must be thread-safe as multiple <see cref="Client.FlioxClient"/> instances are allowed to
        /// use a single <see cref="FlioxHub"/> instance simultaneously.
        /// </summary>
        public abstract override Task<ExecuteSyncResult> ExecuteRequestAsync(SyncRequest syncRequest, SyncContext syncContext);
        
        internal override void AddEventReceiver(in ShortString clientId, IEventReceiver eventReceiver) {
            if (access == RemoteClientAccess.Single && eventReceivers.Count > 0) {
                throw new InvalidOperationException("Remote client is configured for single access");
            }
            eventReceivers.Add(clientId, eventReceiver);
        }
        
        internal override void RemoveEventReceiver(in ShortString clientId) {
            if (clientId.IsNull())
                return;
            eventReceivers.Remove(clientId);
        }
        
        protected internal override bool    IsRemoteHub => true;
        
        public override ExecutionType InitSyncRequest(SyncRequest syncRequest) {
            // base.InitSyncRequest(syncRequest);
            return ExecutionType.Async;
        }
        
        internal override  ObjectPool<ReaderPool> GetResponseReaderPool() => responseReaderPool;
        
        /// <summary>
        /// Method is not thread safe.<br/>
        /// Expectation is calling this method sequentially from a receive message loop.  
        /// </summary>
        protected void OnReceive(in JsonValue message, RemoteRequestMap requestMap, ObjectReader reader) {
            // --- determine message type
            var messageHead = MessageUtils.ReadMessageHead(ref messageParser, message);
                    
            // --- handle either response or event message
            switch (messageHead.type) {
                case MessageType.resp:
                case MessageType.error:
                    if (!messageHead.reqId.HasValue)
                        throw new InvalidOperationException($"missing reqId in response:\n{message}");
                    var id = messageHead.reqId.Value;
                    if (!requestMap.Remove(id, out RemoteRequest request)) {
                        var msg = $"reqId not found. id: {id}";
                        if ((protocolFeature & ProtocolFeature.Duplicates) == 0)    throw new InvalidOperationException(msg);
                        Logger.Log(HubLog.Info, msg);                               // UDP can receive duplicates. 
                        break;
                    }
                    reader.ReaderPool   = request.responseReaderPool;
                    var response        = reader.Read<ProtocolResponse>(message);
                    request.response.SetResult(response);
                    break;
                case MessageType.ev:
                    var clientEvent = new ClientEvent (messageHead.dstClientId, message);
                    OnReceiveEvent(clientEvent);
                    break;
            }
        }
        
        private void OnReceiveEvent(in ClientEvent clientEvent) {
            if (access == RemoteClientAccess.Single) {
                // case: 0 or 1 eventReceivers
                if (eventReceivers.Count == 0) {
                    var msg = $"{GetType().Name}({this}) has no clients, client id: {clientEvent.dstClientId}";
                    Logger.Log(HubLog.Error, msg);
                    return;
                }
                // case: 1 eventReceiver
                foreach (var pair in eventReceivers) {
                    var receiver = pair.Value;
                    if (!clientEvent.dstClientId.IsNull() && !clientEvent.dstClientId.IsEqual(pair.Key)) {
                        var msg = $"expect events only for client id: {pair.Key}, was: {clientEvent.dstClientId}";
                        Logger.Log(HubLog.Error, msg);
                        return;
                    }
                    receiver.SendEvent(clientEvent);
                    return;
                }
                return;
            }
            if (eventReceivers.TryGetValue(clientEvent.dstClientId, out var eventReceiver)) {
                eventReceiver.SendEvent(clientEvent);
                return;
            }
            var msg2 = $"received event for unknown client: {GetType().Name}({this}), client id: {clientEvent.dstClientId}";
            Logger.Log(HubLog.Error, msg2);
        }
        
        protected static ExecuteSyncResult CreateSyncResult(ProtocolResponse response) {
            if (response is SyncResponse syncResponse) {
                return new ExecuteSyncResult(syncResponse);
            }
            if (response is ErrorResponse errorResponse) {
                return new ExecuteSyncResult(errorResponse.message, errorResponse.type);
            }
            return new ExecuteSyncResult($"invalid response: Was: {response.MessageType}", ErrorResponseType.BadResponse);
        }
        
        protected static ExecuteSyncResult CreateSyncError(Exception e, object remoteHost) {
            var error = ErrorResponse.ErrorFromException(e);
            error.Append(" remoteHost: ");
            error.Append(remoteHost);
            var msg = error.ToString();
            return new ExecuteSyncResult(msg, ErrorResponseType.Exception);
        }
    }
    
    public sealed class RemoteDatabase : EntityDatabase
    {
        public   override   string      StorageType => "remote";
        
        public RemoteDatabase(string dbName) : base(dbName, null, null) { }

        public override EntityContainer CreateContainer(in ShortString name, EntityDatabase database) {
            throw new InvalidOperationException("RemoteDatabase cannot create a container");
        }
    }
}

// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Pools;
using Friflo.Json.Fliox.Utils;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
namespace Friflo.Json.Fliox.Hub.Remote
{
    /// <summary>
    /// By default a remote client like <see cref="RemoteClientHub"/> can be used by multiple <see cref="Client.FlioxClient"/>'s.<br/>
    /// <br/>
    /// To minimize the size of serialized <see cref="EventMessage"/>'s sent to subscribed clients the <see cref="EventDispatcher"/>
    /// can omit sending the target client id by settings <see cref="EventDispatcher.SendTargetClientId"/> to false.<br/>
    /// In this case all <see cref="RemoteClientHub"/>'s must be initialized with <see cref="Single"/> enabling
    /// processing events by the single <see cref="Client.FlioxClient"/> using a <see cref="RemoteClientHub"/>. 
    /// </summary>
    public enum RemoteClientAccess
    {
        /// <summary>Remote client can be used only by a single <see cref="Client.FlioxClient"/></summary>
        Single,
        /// <summary>Remote client can be used by multiple <see cref="Client.FlioxClient"/>'s</summary>
        Multi
    }
    
    public abstract class RemoteClientHub : FlioxHub
    {
        private  readonly   Dictionary<JsonKey, EventReceiver>  eventReceivers;
        private  readonly   ObjectPool<ReaderPool>              responseReaderPool;
        private  readonly   RemoteClientAccess                  access;

        // ReSharper disable once EmptyConstructor - added for source navigation
        protected RemoteClientHub(EntityDatabase database, SharedEnv env, RemoteClientAccess access = RemoteClientAccess.Multi)
            : base(database, env)
        {
            eventReceivers      = new Dictionary<JsonKey, EventReceiver>(JsonKey.Equality);
            responseReaderPool  = new ObjectPool<ReaderPool>(() => new ReaderPool(sharedEnv.TypeStore));
            this.access         = access;     
        }

        /// <summary>A class extending  <see cref="RemoteClientHub"/> must implement this method.</summary>
        public abstract override Task<ExecuteSyncResult> ExecuteRequestAsync(SyncRequest syncRequest, SyncContext syncContext);
        
        public override void AddEventReceiver(in JsonKey clientId, EventReceiver eventReceiver) {
            if (access == RemoteClientAccess.Single && eventReceivers.Count > 0) {
                throw new InvalidOperationException("Remote client is configured for single access");
            }
            eventReceivers.Add(clientId, eventReceiver);
        }
        
        public override void RemoveEventReceiver(in JsonKey clientId) {
            if (clientId.IsNull())
                return;
            eventReceivers.Remove(clientId);
        }
        
        public override ExecutionType InitSyncRequest(SyncRequest syncRequest) {
            // base.InitSyncRequest(syncRequest);
            return ExecutionType.Async;
        }
        
        internal override  ObjectPool<ReaderPool> GetResponseReaderPool() => responseReaderPool;
        
        protected void OnReceiveEvent(in ClientEvent clientEvent) {
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
    }
    
    internal sealed class RemoteDatabase : EntityDatabase
    {
        public   override   string      StorageType => "remote";
        
        internal RemoteDatabase(string dbName) : base(dbName, null, null) { }

        public override EntityContainer CreateContainer(in JsonKey name, EntityDatabase database) {
            throw new InvalidOperationException("RemoteDatabase cannot create a container");
        }
    }
}

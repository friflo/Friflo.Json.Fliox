// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
namespace Friflo.Json.Fliox.Hub.Remote
{
    public abstract class RemoteClientHub : FlioxHub
    {
        private  readonly   Dictionary<JsonKey, EventReceiver>   eventReceivers;

        // ReSharper disable once EmptyConstructor - added for source navigation
        protected RemoteClientHub(EntityDatabase database, SharedEnv env)
            : base(database, env)
        {
            eventReceivers = new Dictionary<JsonKey, EventReceiver>(JsonKey.Equality);
        }

        /// <summary>A class extending  <see cref="RemoteClientHub"/> must implement this method.</summary>
        public abstract override Task<ExecuteSyncResult> ExecuteRequestAsync(SyncRequest syncRequest, SyncContext syncContext);
        
        public override void AddEventReceiver(in JsonKey clientId, EventReceiver eventReceiver) {
            eventReceivers.Add(clientId, eventReceiver);
        }
        
        public override void RemoveEventReceiver(in JsonKey clientId) {
            if (clientId.IsNull())
                return;
            eventReceivers.Remove(clientId);
        }
        
        protected void OnReceiveEvent(EventMessage eventMessage) {
            if (eventReceivers.TryGetValue(eventMessage.dstClientId, out var eventReceiver)) {
                // each eventMessage is an individual instance => reusedEvent: false  
                eventReceiver.SendEvent(eventMessage, false, default);
                return;
            }
            var msg = $"received event for unknown client: {GetType().Name}({this}), client id: {eventMessage.dstClientId}";
            Logger.Log(HubLog.Error, msg);
        }
    }
    
    internal sealed class RemoteDatabase : EntityDatabase
    {
        public   override   string      StorageType => "remote";
        
        internal RemoteDatabase(string dbName) : base(dbName, null, null) { }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            throw new InvalidOperationException("RemoteDatabase cannot create a container");
        }
    }
}

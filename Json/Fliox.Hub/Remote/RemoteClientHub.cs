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
        private  readonly   Dictionary<JsonKey, IEventReceiver>   eventReceivers = new Dictionary<JsonKey, IEventReceiver>(JsonKey.Equality);

        // ReSharper disable once EmptyConstructor - added for source navigation
        protected RemoteClientHub(EntityDatabase database, SharedEnv env)
            : base(database, env)
        { }

        /// <summary>A class extending  <see cref="RemoteClientHub"/> must implement this method.</summary>
        public abstract override Task<ExecuteSyncResult> ExecuteSync(SyncRequest syncRequest, SyncContext syncContext);
        
        public override void AddEventReceiver(in JsonKey clientId, IEventReceiver eventReceiver) {
            eventReceivers.Add(clientId, eventReceiver);
        }
        
        public override void RemoveEventReceiver(in JsonKey clientId) {
            if (clientId.IsNull())
                return;
            eventReceivers.Remove(clientId);
        }
        
        protected void ProcessEvent(ProtocolEvent ev) {
            var eventReceiver     = eventReceivers[ev.dstClientId];
            eventReceiver.ProcessEvent(ev);
        }
    }
    
    internal class RemoteDatabase : EntityDatabase
    {
        internal RemoteDatabase(string dbName) : base(dbName, null, null) { }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            throw new InvalidOperationException("RemoteDatabase cannot create a container");
        }
    }
}

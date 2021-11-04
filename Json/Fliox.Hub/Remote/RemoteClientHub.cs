// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Mapper;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
namespace Friflo.Json.Fliox.Hub.Remote
{
    public abstract class RemoteClientHub : FlioxHub
    {
        private  readonly   Dictionary<JsonKey, IEventTarget>   clientTargets = new Dictionary<JsonKey, IEventTarget>(JsonKey.Equality);
        private  readonly   Pool                                pool = new Pool(SharedHost.Instance.Pool);

        // ReSharper disable once EmptyConstructor - added for source navigation
        protected RemoteClientHub(EntityDatabase database, SharedEnv env, string hostName = null) : base(database, env, hostName) { }

        /// <summary>A class extending  <see cref="RemoteClientHub"/> must implement this method.</summary>
        public abstract override Task<ExecuteSyncResult> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext);
        
        public override void AddEventTarget(in JsonKey clientId, IEventTarget eventTarget) {
            clientTargets.Add(clientId, eventTarget);
        }
        
        public override void RemoveEventTarget(in JsonKey clientId) {
            if (clientId.IsNull())
                return;
            clientTargets.Remove(clientId);
        }
        
        protected void ProcessEvent(ProtocolEvent ev) {
            var eventTarget     = clientTargets[ev.dstClientId];
            var messageContext  = new MessageContext(pool, eventTarget);
            eventTarget.ProcessEvent(ev, messageContext);
            messageContext.Release();
        }
    }
}

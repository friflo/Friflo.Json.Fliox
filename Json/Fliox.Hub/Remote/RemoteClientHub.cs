// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
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
        private  readonly   Pool                                pool;
        private  readonly   SharedCache                         sharedCache;

        // ReSharper disable once EmptyConstructor - added for source navigation
        protected RemoteClientHub(EntityDatabase database, SharedEnv env)
            : base(database, env)
        {
            pool        = new Pool(sharedEnv.Pool);
            sharedCache = sharedEnv.sharedCache;
        }

        /// <summary>A class extending  <see cref="RemoteClientHub"/> must implement this method.</summary>
        public abstract override Task<ExecuteSyncResult> ExecuteSync(SyncRequest syncRequest, ExecuteContext executeContext);
        
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
            var executeContext  = new ExecuteContext(pool, eventTarget, sharedCache);
            eventTarget.ProcessEvent(ev, executeContext);
            executeContext.Release();
        }
    }
    
    internal class RemoteDatabase : EntityDatabase
    {
        internal RemoteDatabase(string databaseName) : base(databaseName, null, null) { }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            throw new InvalidOperationException("RemoteDatabase cannot create a container");
        }
    }
}

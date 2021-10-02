// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Host.Event;
using Friflo.Json.Fliox.DB.Protocol;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
namespace Friflo.Json.Fliox.DB.Remote
{
    public abstract class RemoteClientDatabase : EntityDatabase
    {
        private  readonly   Dictionary<string, IEventTarget>    clientTargets = new Dictionary<string, IEventTarget>();
        private  readonly   Pools                               pools = new Pools(Pools.SharedPools);

        // ReSharper disable once EmptyConstructor - added for source navigation
        protected RemoteClientDatabase() {
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            RemoteClientContainer container = new RemoteClientContainer(name, this);
            return container;
        }
        
        /// <summary>A class extending  <see cref="RemoteClientDatabase"/> must implement this method.</summary>
        public abstract override Task<MsgResponse<SyncResponse>> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext);
        
        public override void AddEventTarget(string userId, IEventTarget eventTarget) {
            clientTargets.Add(userId, eventTarget);
        }
        
        public override void RemoveEventTarget(string userId) {
            clientTargets.Remove(userId);
        }
        
        protected void ProcessEvent(ProtocolEvent ev) {
            var eventTarget     = clientTargets[ev.targetId];
            var messageContext  = new MessageContext(pools, eventTarget);
            eventTarget.ProcessEvent(ev, messageContext);
            messageContext.Release();
        }
    }
    
    public sealed class RemoteClientContainer : EntityContainer
    {
        public RemoteClientContainer(string name, EntityDatabase database)
            : base(name, database) {
        }

        public override Task<CreateEntitiesResult> CreateEntities(CreateEntities command, MessageContext messageContext) {
            throw new InvalidOperationException("RemoteClientContainer does not execute CRUD commands");
        }

        public override Task<UpsertEntitiesResult> UpsertEntities(UpsertEntities command, MessageContext messageContext) {
            throw new InvalidOperationException("RemoteClientContainer does not execute CRUD commands");
        }

        public override Task<ReadEntitiesResult> ReadEntities(ReadEntities command, MessageContext messageContext) {
            throw new InvalidOperationException("RemoteClientContainer does not execute CRUD commands");
        }
        
        public override Task<QueryEntitiesResult> QueryEntities(QueryEntities command, MessageContext messageContext) {
            throw new InvalidOperationException("RemoteClientContainer does not execute CRUD commands");
        }
        
        public override Task<DeleteEntitiesResult> DeleteEntities(DeleteEntities command, MessageContext messageContext) {
            throw new InvalidOperationException("RemoteClientContainer does not execute CRUD commands");
        }
    }
}

// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database.Event;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Sync;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
namespace Friflo.Json.Flow.Database.Remote
{
    public abstract class RemoteClientDatabase : EntityDatabase
    {
        private             int                                 reqId;
        private  readonly   ProtocolType                        protocolType;
        private  readonly   Dictionary<string, IEventTarget>    clientTargets = new Dictionary<string, IEventTarget>();
        private  readonly   Pools                               pools = new Pools(Pools.SharedPools);

        // ReSharper disable once EmptyConstructor - added for source navigation
        protected RemoteClientDatabase(ProtocolType protocolType) {
            this.protocolType = protocolType;
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            RemoteClientContainer container = new RemoteClientContainer(name, this);
            return container;
        }
        
        public override void AddEventTarget(string clientId, IEventTarget eventTarget) {
            clientTargets.Add(clientId, eventTarget);
        }
        
        public override void RemoveEventTarget(string clientId) {
            clientTargets.Remove(clientId);
        }
        
        protected void ProcessEvent(DatabaseEvent ev) {
            var eventTarget     = clientTargets[ev.targetId];
            var messageContext  = new MessageContext(pools, eventTarget);
            eventTarget.ProcessEvent(ev, messageContext);
            messageContext.Release();
        }

        protected abstract Task<JsonResponse> ExecuteRequestJson(int requestId, string jsonRequest, MessageContext messageContext);
        
        public override async Task<SyncResponse> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext) {
            var response = await ExecuteRequest(syncRequest, messageContext).ConfigureAwait(false);
            if (response is SyncResponse syncResponse)
                return syncResponse;
            var error = (ErrorResponse)response;
            return new SyncResponse {error = error};
        }
        
        private async Task<DatabaseResponse> ExecuteRequest(DatabaseRequest request, MessageContext messageContext) {
            int requestId = Interlocked.Increment(ref reqId);
            request.reqId = requestId; 
            using (var pooledMapper = messageContext.pools.ObjectMapper.Get()) {
                ObjectMapper mapper = pooledMapper.instance;
                mapper.Pretty = true;
                mapper.WriteNullMembers = false;
                var jsonRequest = CreateRequest(mapper.writer, request);
                var result = await ExecuteRequestJson(requestId, jsonRequest, messageContext).ConfigureAwait(false);
                
                ObjectReader reader = mapper.reader;
                if (result.statusType == ResponseStatusType.Ok) {
                    var response = reader.Read<DatabaseResponse>(result.body);
                    if (reader.Error.ErrSet)
                        return new ErrorResponse{message = reader.Error.msg.ToString()};
                    // At this point the returned result.body is valid JSON.
                    // => All entities of a SyncResponse.results have either a valid JSON value or an error. 
                    return response;
                }
                var errorResponse = reader.Read<ErrorResponse>(result.body);
                if (reader.Error.ErrSet)
                    return new ErrorResponse{message = reader.Error.msg.ToString()};
                return errorResponse;
            }
        }
        
        private string CreateRequest (ObjectWriter writer, DatabaseRequest request) {
            switch (protocolType) {
                case ProtocolType.ReqResp:
                    return writer.Write(request);
                case ProtocolType.BiDirect:
                    var msg = new DatabaseMessage { req = request };
                    return writer.Write(msg);
            }
            throw new InvalidOperationException("cannot be reached");
        }
    }
    
    public class RemoteClientContainer : EntityContainer
    {
        public RemoteClientContainer(string name, EntityDatabase database)
            : base(name, database) {
        }

        public override Task<CreateEntitiesResult> CreateEntities(CreateEntities command, MessageContext messageContext) {
            throw new InvalidOperationException("RemoteClientContainer does not execute CRUD commands");
        }

        public override Task<UpdateEntitiesResult> UpdateEntities(UpdateEntities command, MessageContext messageContext) {
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

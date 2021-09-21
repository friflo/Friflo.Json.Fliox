// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.NoSQL.Event;
using Friflo.Json.Fliox.DB.Sync;
using Friflo.Json.Fliox.Mapper;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
namespace Friflo.Json.Fliox.DB.NoSQL.Remote
{
    public abstract class RemoteClientDatabase : EntityDatabase
    {
        private             int                                 reqId;
        private  readonly   Dictionary<string, IEventTarget>    clientTargets = new Dictionary<string, IEventTarget>();
        private  readonly   Pools                               pools = new Pools(Pools.SharedPools);

        // ReSharper disable once EmptyConstructor - added for source navigation
        protected RemoteClientDatabase() {
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
        
        protected void ProcessEvent(ProtocolEvent ev) {
            var eventTarget     = clientTargets[ev.targetId];
            var messageContext  = new MessageContext(pools, eventTarget);
            eventTarget.ProcessEvent(ev, messageContext);
            messageContext.Release();
        }

        protected abstract Task<JsonResponse> ExecuteRequestJson(int requestId, JsonUtf8 jsonRequest, MessageContext messageContext);
        
        public override async Task<SyncResponse> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext) {
            var response = await ExecuteRequest(syncRequest, messageContext).ConfigureAwait(false);
            if (response is SyncResponse syncResponse)
                return syncResponse;
            var error = (ErrorResponse)response;
            return new SyncResponse {error = error};
        }
        
        private async Task<ProtocolResponse> ExecuteRequest(ProtocolRequest request, MessageContext messageContext) {
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
                    var msg = reader.Read<ProtocolMessage>(result.body);
                    if (reader.Error.ErrSet)
                        return new ErrorResponse{message = reader.Error.msg.AsString()};
                    // At this point the returned result.body is valid JSON.
                    // => All entities of a SyncResponse.results have either a valid JSON value or an error.
                    if (msg is ProtocolResponse req)
                        return req;
                    return new ErrorResponse{ message = $"Expect response. Was MessageType: {msg.MessageType}"};
                }
                var errMsg = reader.Read<ProtocolMessage>(result.body);
                if (reader.Error.ErrSet)
                    return new ErrorResponse{message = reader.Error.msg.AsString()};
                if (errMsg is ErrorResponse errorResp)
                    return errorResp;
                return new ErrorResponse{ message = $"Expect error response. Was MessageType: {errMsg.MessageType}"};
            }
        }
        
        private JsonUtf8 CreateRequest (ObjectWriter writer, ProtocolRequest request) {
            return new JsonUtf8(writer.WriteAsArray<ProtocolMessage>(request));
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

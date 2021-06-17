// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database.Event;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Sync;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
namespace Friflo.Json.Flow.Database.Remote
{
    public abstract class RemoteClientDatabase : EntityDatabase
    {
        private  readonly   ProtocolType                        protocolType;
        private  readonly   Dictionary<string, IEventTarget>    clientTargets = new Dictionary<string, IEventTarget>();

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
            var contextPools    = new Pools(Pools.SharedPools);
            var syncContext     = new SyncContext(contextPools, eventTarget);
            eventTarget.ProcessEvent(ev, syncContext);
        }

        protected abstract Task<JsonResponse> ExecuteRequestJson(string jsonRequest, SyncContext syncContext);
        
        public override async Task<SyncResponse> ExecuteSync(SyncRequest syncRequest, SyncContext syncContext) {
            var response = await ExecuteRequest(syncRequest, syncContext).ConfigureAwait(false);
            if (response is SyncResponse syncResponse)
                return syncResponse;
            var error = (ErrorResponse)response;
            return new SyncResponse {error = error};
        }
        
        private async Task<DatabaseResponse> ExecuteRequest(DatabaseRequest request, SyncContext syncContext) {
            using (var pooledMapper = syncContext.pools.ObjectMapper.Get()) {
                ObjectMapper mapper = pooledMapper.instance;
                mapper.Pretty = true;
                mapper.WriteNullMembers = false;
                var jsonRequest = CreateRequest(mapper.writer, request);
                var result = await ExecuteRequestJson(jsonRequest, syncContext).ConfigureAwait(false);
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

        public override Task<CreateEntitiesResult> CreateEntities(CreateEntities command, SyncContext syncContext) {
            throw new InvalidOperationException("RemoteClientContainer does not execute CRUD commands");
        }

        public override Task<UpdateEntitiesResult> UpdateEntities(UpdateEntities command, SyncContext syncContext) {
            throw new InvalidOperationException("RemoteClientContainer does not execute CRUD commands");
        }

        public override Task<ReadEntitiesResult> ReadEntities(ReadEntities command, SyncContext syncContext) {
            throw new InvalidOperationException("RemoteClientContainer does not execute CRUD commands");
        }
        
        public override Task<QueryEntitiesResult> QueryEntities(QueryEntities command, SyncContext syncContext) {
            throw new InvalidOperationException("RemoteClientContainer does not execute CRUD commands");
        }
        
        public override Task<DeleteEntitiesResult> DeleteEntities(DeleteEntities command, SyncContext syncContext) {
            throw new InvalidOperationException("RemoteClientContainer does not execute CRUD commands");
        }
    }
}

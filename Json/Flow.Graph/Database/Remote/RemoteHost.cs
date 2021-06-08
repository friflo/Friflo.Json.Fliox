// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Sync;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
namespace Friflo.Json.Flow.Database.Remote
{
    public class RemoteHostDatabase : EntityDatabase
    {
        private readonly    EntityDatabase  local;

        public RemoteHostDatabase(EntityDatabase local) {
            this.local = local;
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            EntityContainer localContainer = local.CreateContainer(name, local);
            RemoteHostContainer container = new RemoteHostContainer(name, this, localContainer);
            return container;
        }
        
        public override async Task<SyncResponse> ExecuteSync(SyncRequest syncRequest, SyncContext syncContext) {
            var result = await local.ExecuteSync(syncRequest, syncContext).ConfigureAwait(false);
            return result;
        }

        public async Task<JsonResponse> ExecuteRequestJson(string jsonRequest) {
            var contextPools    = new Pools(Pools.SharedPools);
            var syncContext     = new SyncContext(contextPools);
            try {
                string jsonResponse;
                using (var pooledMapper = syncContext.pools.ObjectMapper.Get()) {
                    ObjectMapper mapper = pooledMapper.instance;
                    ObjectReader reader = mapper.reader;
                    var request = reader.Read<DatabaseRequest>(jsonRequest);
                    if (reader.Error.ErrSet)
                        return JsonResponse.CreateResponseError(syncContext, reader.Error.msg.ToString(), RequestStatusType.Error);
                    DatabaseResponse response = await ExecuteRequest(request, syncContext).ConfigureAwait(false);
                    mapper.WriteNullMembers = false;
                    mapper.Pretty = true;
                    jsonResponse = mapper.Write(response);
                }
                syncContext.pools.AssertNoLeaks();
                return new JsonResponse(jsonResponse, RequestStatusType.Ok);
            } catch (Exception e) {
                var errorMsg = ResponseError.ErrorFromException(e).ToString();
                return JsonResponse.CreateResponseError(syncContext, errorMsg, RequestStatusType.Exception);
            }
        }
        
        private async Task<DatabaseResponse> ExecuteRequest(DatabaseRequest request, SyncContext syncContext) {
            switch (request.RequestType) {
                case RequestType.sync:
                    return await ExecuteSync((SyncRequest)request, syncContext).ConfigureAwait(false);
                case RequestType.subscribe:
                    throw new NotImplementedException();
                default:
                    throw new NotImplementedException();
            }
        }
    }
    
    public enum RequestStatusType {
        /// maps to HTTP 200 OK
        Ok,         
        /// maps to HTTP 400 Bad Request
        Error,
        /// maps to HTTP 500 Internal Server Error
        Exception
    }
    
    public class JsonResponse
    {
        public readonly     string              body;
        public readonly     RequestStatusType   statusType;
        
        public JsonResponse(string body, RequestStatusType statusType) {
            this.body       = body;
            this.statusType  = statusType;
        }
        
        public static JsonResponse CreateResponseError(SyncContext syncContext, string message, RequestStatusType type) {
            var responseError = new ResponseError {message = message};
            using (var pooledMapper = syncContext.pools.ObjectMapper.Get()) {
                ObjectMapper mapper = pooledMapper.instance;
                var body = mapper.Write(responseError);
                return new JsonResponse(body, type);
            }
        }
    }
    
    public class RemoteHostContainer : EntityContainer
    {
        private readonly    EntityContainer local;
        
        public  override    bool            Pretty       => local.Pretty;

        public RemoteHostContainer(string name, EntityDatabase database, EntityContainer localContainer)
            : base(name, database) {
            local = localContainer;
        }


        public override async Task<CreateEntitiesResult> CreateEntities(CreateEntities command, SyncContext syncContext) {
            return await local.CreateEntities(command, syncContext).ConfigureAwait(false);
        }

        public override async Task<UpdateEntitiesResult> UpdateEntities(UpdateEntities command, SyncContext syncContext) {
            return await local.UpdateEntities(command, syncContext).ConfigureAwait(false);
        }

        public override async Task<ReadEntitiesResult> ReadEntities(ReadEntities command, SyncContext syncContext) {
            return await local.ReadEntities(command, syncContext).ConfigureAwait(false);
        }
        
        public override async Task<QueryEntitiesResult> QueryEntities(QueryEntities command, SyncContext syncContext) {
            return await local.QueryEntities(command, syncContext).ConfigureAwait(false);
        }
        
        public override async Task<DeleteEntitiesResult> DeleteEntities(DeleteEntities command, SyncContext syncContext) {
            return await local.DeleteEntities(command, syncContext).ConfigureAwait(false);
        }
    }
}

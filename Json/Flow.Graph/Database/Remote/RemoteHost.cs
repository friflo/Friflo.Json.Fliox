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

        public async Task<JsonResponse> ExecuteRequestJson(string jsonRequest, SyncContext syncContext, ProtocolType type) {
            try {
                string jsonResponse;
                using (var pooledMapper = syncContext.pools.ObjectMapper.Get()) {
                    ObjectMapper    mapper  = pooledMapper.instance;
                    ObjectReader    reader  = mapper.reader;
                    DatabaseRequest request = ReadRequest (reader, jsonRequest, type);
                    if (reader.Error.ErrSet)
                        return JsonResponse.CreateResponseError(syncContext, reader.Error.msg.ToString(), RequestStatusType.Error);
                    DatabaseResponse response = await ExecuteRequest(request, syncContext).ConfigureAwait(false);
                    mapper.WriteNullMembers = false;
                    mapper.Pretty = true;
                    jsonResponse = CreateResponse(mapper.writer, response, type);
                }
                return new JsonResponse(jsonResponse, RequestStatusType.Ok);
            } catch (Exception e) {
                var errorMsg = ErrorResponse.ErrorFromException(e).ToString();
                return JsonResponse.CreateResponseError(syncContext, errorMsg, RequestStatusType.Exception);
            }
        }
        
        private static DatabaseRequest ReadRequest (ObjectReader reader, string jsonRequest, ProtocolType type) {
            switch (type) {
                case ProtocolType.ReqResp:
                    return reader.Read<DatabaseRequest>(jsonRequest);
                case ProtocolType.BiDirect:
                    var msg = reader.Read<DatabaseMessage>(jsonRequest);
                    return msg.req;
            }
            throw new InvalidOperationException("can't be reached");
        }
        
        private static string CreateResponse (ObjectWriter writer, DatabaseResponse response, ProtocolType type) {
            switch (type) {
                case ProtocolType.ReqResp:
                    return writer.Write(response);
                case ProtocolType.BiDirect:
                    var message = new DatabaseMessage { resp = response };
                    return writer.Write(message);
            }
            throw new InvalidOperationException("can't be reached");
        }
        
        private async Task<DatabaseResponse> ExecuteRequest(DatabaseRequest request, SyncContext syncContext) {
            switch (request.RequestType) {
                case RequestType.sync:
                    return await ExecuteSync((SyncRequest)request, syncContext).ConfigureAwait(false);
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
            var errorResponse = new ErrorResponse {message = message};
            using (var pooledMapper = syncContext.pools.ObjectMapper.Get()) {
                ObjectMapper mapper = pooledMapper.instance;
                var body = mapper.Write(errorResponse);
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

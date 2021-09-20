// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Sync;
using Friflo.Json.Fliox.Mapper;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
namespace Friflo.Json.Fliox.DB.NoSQL.Remote
{
    public class RemoteHostDatabase : EntityDatabase
    {
        internal readonly   EntityDatabase  local;
        /// Only set to true for testing. It avoids an early out at <see cref="Event.EventSubscriber.SendEvents"/> 
        public              bool            fakeOpenClosedSockets;

        public RemoteHostDatabase(EntityDatabase local) {
            this.local = local;
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            EntityContainer localContainer = local.CreateContainer(name, local);
            RemoteHostContainer container = new RemoteHostContainer(name, this, localContainer);
            return container;
        }
        
        public override async Task<SyncResponse> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext) {
            var result = await local.ExecuteSync(syncRequest, messageContext).ConfigureAwait(false);
            result.reqId = syncRequest.reqId;
            return result;
        }

        public async Task<JsonResponse> ExecuteRequestJson(Utf8Json jsonRequest, MessageContext messageContext, ProtocolType type) {
            try {
                Utf8Json jsonResponse;
                using (var pooledMapper = messageContext.pools.ObjectMapper.Get()) {
                    ObjectMapper    mapper  = pooledMapper.instance;
                    ObjectReader    reader  = mapper.reader;
                    DatabaseRequest request = ReadRequest (reader, jsonRequest, type);
                    if (reader.Error.ErrSet)
                        return JsonResponse.CreateResponseError(messageContext, reader.Error.msg.AsString(), ResponseStatusType.Error);
                    DatabaseResponse response = await ExecuteRequest(request, messageContext).ConfigureAwait(false);
                    mapper.WriteNullMembers = false;
                    mapper.Pretty = true;
                    jsonResponse = CreateResponse(mapper.writer, response, type);
                }
                return new JsonResponse(jsonResponse, ResponseStatusType.Ok);
            } catch (Exception e) {
                var errorMsg = ErrorResponse.ErrorFromException(e).ToString();
                return JsonResponse.CreateResponseError(messageContext, errorMsg, ResponseStatusType.Exception);
            }
        }
        
        /// Caller need to check <see cref="reader"/> error state. 
        private static DatabaseRequest ReadRequest (ObjectReader reader, Utf8Json jsonRequest, ProtocolType type) {
            switch (type) {
                case ProtocolType.ReqResp:
                    return reader.Read<DatabaseRequest>(jsonRequest);
                case ProtocolType.BiDirect:
                    var msg = reader.Read<DatabaseMessage>(jsonRequest);
                    if (reader.Success)
                        return msg.req;
                    return null;
            }
            throw new InvalidOperationException("can't be reached");
        }
        
        private static Utf8Json CreateResponse (ObjectWriter writer, DatabaseResponse response, ProtocolType type) {
            switch (type) {
                case ProtocolType.ReqResp:
                    return new Utf8Json(writer.WriteAsArray(response));
                case ProtocolType.BiDirect:
                    var message = new DatabaseMessage { resp = response };
                    return new Utf8Json(writer.WriteAsArray(message));
            }
            throw new InvalidOperationException("can't be reached");
        }
        
        private async Task<DatabaseResponse> ExecuteRequest(DatabaseRequest request, MessageContext messageContext) {
            switch (request.RequestType) {
                case RequestType.sync:
                    return await ExecuteSync((SyncRequest)request, messageContext).ConfigureAwait(false);
                default:
                    throw new NotImplementedException();
            }
        }
    }
    
    public enum ResponseStatusType {
        /// maps to HTTP 200 OK
        Ok,         
        /// maps to HTTP 400 Bad Request
        Error,
        /// maps to HTTP 500 Internal Server Error
        Exception
    }
    
    public class JsonResponse
    {
        public readonly     Utf8Json            body;
        public readonly     ResponseStatusType  statusType;
        
        public JsonResponse(Utf8Json body, ResponseStatusType statusType) {
            this.body       = body;
            this.statusType  = statusType;
        }
        
        public static JsonResponse CreateResponseError(MessageContext messageContext, string message, ResponseStatusType type) {
            var errorResponse = new ErrorResponse {message = message};
            using (var pooledMapper = messageContext.pools.ObjectMapper.Get()) {
                ObjectMapper mapper = pooledMapper.instance;
                var bodyArray = mapper.WriteAsArray(errorResponse);
                var body = new Utf8Json(bodyArray);
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


        public override async Task<CreateEntitiesResult> CreateEntities(CreateEntities command, MessageContext messageContext) {
            return await local.CreateEntities(command, messageContext).ConfigureAwait(false);
        }

        public override async Task<UpsertEntitiesResult> UpsertEntities(UpsertEntities command, MessageContext messageContext) {
            return await local.UpsertEntities(command, messageContext).ConfigureAwait(false);
        }

        public override async Task<ReadEntitiesResult> ReadEntities(ReadEntities command, MessageContext messageContext) {
            return await local.ReadEntities(command, messageContext).ConfigureAwait(false);
        }
        
        public override async Task<QueryEntitiesResult> QueryEntities(QueryEntities command, MessageContext messageContext) {
            return await local.QueryEntities(command, messageContext).ConfigureAwait(false);
        }
        
        public override async Task<DeleteEntitiesResult> DeleteEntities(DeleteEntities command, MessageContext messageContext) {
            return await local.DeleteEntities(command, messageContext).ConfigureAwait(false);
        }
    }
}

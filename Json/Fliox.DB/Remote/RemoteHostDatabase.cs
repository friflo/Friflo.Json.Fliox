// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.Mapper;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
namespace Friflo.Json.Fliox.DB.Remote
{
    public class RemoteHostDatabase : EntityDatabase
    {
        internal readonly   EntityDatabase  local;
        /// Only set to true for testing. It avoids an early out at <see cref="Host.Event.EventSubscriber.SendEvents"/> 
        public              bool            fakeOpenClosedSockets;

        public RemoteHostDatabase(EntityDatabase local) {
            this.local = local;
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            EntityContainer localContainer = local.CreateContainer(name, local);
            RemoteHostContainer container = new RemoteHostContainer(name, this, localContainer);
            return container;
        }
        
        public override async Task<MsgResponse<SyncResponse>> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext) {
            var response = await local.ExecuteSync(syncRequest, messageContext).ConfigureAwait(false);
            response.Result.reqId = syncRequest.reqId;
            return response;
        }

        public async Task<JsonResponse> ExecuteJsonRequest(JsonUtf8 jsonRequest, MessageContext messageContext) {
            try {
                var request = RemoteUtils.ReadProtocolMessage(jsonRequest, messageContext.pools, out string error);
                switch (request) {
                    case null:
                        return JsonResponse.CreateError(messageContext, error, JsonResponseStatus.Error);
                    case SyncRequest syncRequest:
                        var         response        = await ExecuteSync(syncRequest, messageContext).ConfigureAwait(false);
                        JsonUtf8    jsonResponse    = RemoteUtils.CreateProtocolMessage(response.Result, messageContext.pools);
                        return new JsonResponse(jsonResponse, JsonResponseStatus.Ok);
                    default:
                        var msg = $"Unknown request. Name: {request.GetType().Name}";
                        return JsonResponse.CreateError(messageContext, msg, JsonResponseStatus.Error);
                }
            }
            catch (Exception e) {
                var errorMsg = ErrorResponse.ErrorFromException(e).ToString();
                return JsonResponse.CreateError(messageContext, errorMsg, JsonResponseStatus.Exception);
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

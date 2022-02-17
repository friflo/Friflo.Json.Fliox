// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Mapper;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
namespace Friflo.Json.Fliox.Hub.Remote
{
    public class RemoteHostHub : FlioxHub
    {
        private  readonly   FlioxHub     localHub;
        
        /// Only set to true for testing. It avoids an early out at <see cref="Host.Event.EventSubscriber.SendEvents"/> 
        public              bool            fakeOpenClosedSockets;

        protected RemoteHostHub(FlioxHub hub, SharedEnv env, string hostName) : base(hub.database, env, hostName) {
            localHub = hub;
        }
        
        public override async Task<ExecuteSyncResult> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext) {
            var response    = await localHub.ExecuteSync(syncRequest, messageContext).ConfigureAwait(false);
            SetContainerResults(response.success);
            response.Result.reqId       = syncRequest.reqId;
            return response;
        }

        public async Task<JsonResponse> ExecuteJsonRequest(JsonValue jsonRequest, MessageContext messageContext) {
            try {
                var request = RemoteUtils.ReadProtocolMessage(jsonRequest, messageContext.pool, out string error);
                switch (request) {
                    case null:
                        return JsonResponse.CreateError(messageContext, error, ErrorResponseType.BadResponse);
                    case SyncRequest syncRequest:
                        var         response        = await ExecuteSync(syncRequest, messageContext).ConfigureAwait(false);
                        JsonValue   jsonResponse    = RemoteUtils.CreateProtocolMessage(response.Result, messageContext.pool);
                        return new JsonResponse(jsonResponse, JsonResponseStatus.Ok);
                    default:
                        var msg = $"Unknown request. Name: {request.GetType().Name}";
                        return JsonResponse.CreateError(messageContext, msg, ErrorResponseType.BadResponse);
                }
            }
            catch (Exception e) {
                var errorMsg = ErrorResponse.ErrorFromException(e).ToString();
                return JsonResponse.CreateError(messageContext, errorMsg, ErrorResponseType.Exception);
            }
        }
        
        /// Required only by <see cref="RemoteHostHub"/>
        /// Distribute <see cref="ContainerEntities.entityMap"/> to <see cref="ContainerEntities.entities"/>,
        /// <see cref="ContainerEntities.notFound"/> and <see cref="ContainerEntities.errors"/> to simplify and
        /// minimize response by removing redundancy.
        /// <see cref="Client.FlioxClient.GetContainerResults"/> remap these properties.
        private static void SetContainerResults(SyncResponse response)
        {
            if (response == null)
                return;
            var resultMap = response.resultMap;
            response.resultMap = null;
            var containers = response.containers = new List<ContainerEntities>(resultMap.Count);
            foreach (var resultPair in resultMap) {
                ContainerEntities value = resultPair.Value;
                containers.Add(value);
            }
            foreach (var container in containers) {
                var entityMap       = container.entityMap;
                var entities        = new List<JsonValue> (entityMap.Count);
                container.entities  = entities;
                List<JsonKey> notFound = null;
                var errors          = container.errors;
                container.errors    = null;
                entities.Capacity   = entityMap.Count;
                foreach (var entityPair in entityMap) {
                    EntityValue entity = entityPair.Value;
                    if (entity.Error != null) {
                        errors.Add(entityPair.Key, entity.Error);
                        continue;
                    }
                    var json = entity.Json;
                    if (json.IsNull()) {
                        if (notFound == null) {
                            notFound = new List<JsonKey>();
                        }
                        notFound.Add(entityPair.Key);
                        continue;
                    }
                    entities.Add(json);
                }
                entityMap.Clear();
                if (notFound != null) {
                    container.notFound = notFound;
                }
                if (errors != null && errors.Count > 0) {
                    container.errors = errors;
                }
                if (entities.Count > 0) {
                    container.count = entities.Count;
                }
            }
            resultMap.Clear();
        }
    }
}

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
    public class RemoteHost : IDisposable
    {
        private  readonly   FlioxHub    localHub;
        public   readonly   SharedEnv   sharedEnv;
        
        /// Only set to true for testing. It avoids an early out at <see cref="Host.Event.EventSubscriber.SendEvents"/> 
        public              bool        fakeOpenClosedSockets;
        
        internal            FlioxHub    LocalHub => localHub;

        protected RemoteHost(FlioxHub hub, SharedEnv env) {
            sharedEnv   = env  ?? SharedEnv.Default;
            localHub    = hub;
        }
        
        public void Dispose() { }
        
        internal async Task<JsonResponse> ExecuteJsonRequest(JsonValue jsonRequest, ExecuteContext executeContext) {
            try {
                var request = RemoteUtils.ReadProtocolMessage(jsonRequest, executeContext.ObjectMapper, out string error);
                switch (request) {
                    case null:
                        return JsonResponse.CreateError(executeContext, error, ErrorResponseType.BadResponse);
                    case SyncRequest syncRequest:
                        var response = await localHub.ExecuteSync(syncRequest, executeContext).ConfigureAwait(false);
                        
                        SetContainerResults(response.success);
                        response.Result.reqId   = syncRequest.reqId;
                        JsonValue jsonResponse  = RemoteUtils.CreateProtocolMessage(response.Result, executeContext.ObjectMapper);
                        return new JsonResponse(jsonResponse, JsonResponseStatus.Ok);
                    default:
                        var msg = $"Unknown request. Name: {request.GetType().Name}";
                        return JsonResponse.CreateError(executeContext, msg, ErrorResponseType.BadResponse);
                }
            }
            catch (Exception e) {
                var errorMsg = ErrorResponse.ErrorFromException(e).ToString();
                return JsonResponse.CreateError(executeContext, errorMsg, ErrorResponseType.Exception);
            }
        }
        
        /// Required only by <see cref="RemoteHost"/>
        /// Distribute <see cref="ContainerEntities.entityMap"/> to <see cref="ContainerEntities.entities"/>,
        /// <see cref="ContainerEntities.notFound"/> and <see cref="ContainerEntities.errors"/> to simplify and
        /// minimize response by removing redundancy.
        /// <see cref="Client.FlioxClient.GetContainerResults"/> remap these properties.
        public static void SetContainerResults(SyncResponse response)
        {
            if (response == null)
                return;
            var resultMap       = response.resultMap;
            response.resultMap  = null;
            if (resultMap.Count == 0)
                return;
            var containers      = new List<ContainerEntities>(resultMap.Count);
            response.containers = containers;
            foreach (var resultPair in resultMap) {
                ContainerEntities value = resultPair.Value;
                containers.Add(value);
            }
            foreach (var container in containers) {
                var entityMap       = container.entityMap;
                var entities        = new List<JsonValue> (entityMap.Count);
                container.entities  = entities;
                List<JsonKey>       notFound = null;
                List<EntityError>   errors   = null;
                entities.Capacity   = entityMap.Count;
                foreach (var entityPair in entityMap) {
                    EntityValue entity  = entityPair.Value;
                    var error           = entity.Error;
                    if (error != null) {
                        if (errors == null) {
                            errors = new List<EntityError>();
                        }
                        errors.Add(error);
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
                container.notFound  = notFound;
                container.errors    = errors;
                if (entities.Count > 0) {
                    container.count = entities.Count;
                }
            }
            resultMap.Clear();
        }

    }
}

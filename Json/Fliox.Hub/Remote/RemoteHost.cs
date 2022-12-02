// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Utils;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
namespace Friflo.Json.Fliox.Hub.Remote
{
    public class RemoteHost : IDisposable, ILogSource
    {
        private  readonly   FlioxHub    localHub;
        public   readonly   SharedEnv   sharedEnv;
        
        /// Only set to true for testing. It avoids an early out at <see cref="EventSubClient.SendEvents"/> 
        public              bool        fakeOpenClosedSockets;
        
        internal            FlioxHub    LocalHub    => localHub;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public              IHubLogger  Logger      => sharedEnv.hubLogger;


        public RemoteHost(FlioxHub hub, SharedEnv env) {
            sharedEnv   = env  ?? SharedEnv.Default;
            localHub    = hub;
        }
        
        public void Dispose() { }
        
        /// <summary>
        /// <b>Attention</b> returned <see cref="JsonResponse"/> is <b>only</b> valid until the passed <paramref name="mapper"/> is reused
        /// </summary>
        public async Task<JsonResponse> ExecuteJsonRequest(
            ObjectMapper    mapper,
            JsonValue       jsonRequest,
            SyncContext     syncContext)
        {
            try {
                var syncRequest = RemoteUtils.ReadSyncRequest(mapper, jsonRequest, out string error);
                if (error != null) {
                    return JsonResponse.CreateError(mapper, error, ErrorResponseType.BadResponse, null);
                }
                var response = await localHub.ExecuteSync(syncRequest, syncContext).ConfigureAwait(false);
                
                var responseError = response.error;
                if (responseError != null) {
                    return JsonResponse.CreateError(mapper, responseError.message, responseError.type, syncRequest.reqId);
                }
                SetContainerResults(response.success);
                response.Result.reqId   = syncRequest.reqId;
                JsonValue jsonResponse  = RemoteUtils.CreateProtocolMessage(response.Result, mapper);
                return new JsonResponse(jsonResponse, JsonResponseStatus.Ok);
            }
            catch (Exception e) {
                var errorMsg = ErrorResponse.ErrorFromException(e).ToString();
                return JsonResponse.CreateError(mapper, errorMsg, ErrorResponseType.Exception, null);
            }
        }
        
        /// Required only by <see cref="RemoteHost"/>
        /// Distribute <see cref="ContainerEntities.entityMap"/> to <see cref="ContainerEntities.entities"/>,
        /// <see cref="ContainerEntities.notFound"/> and <see cref="ContainerEntities.errors"/> to simplify and
        /// minimize response by removing redundancy.
        /// <see cref="Client.FlioxClient.GetContainerResults"/> remap these properties.
        public static void SetContainerResults(SyncResponse response)
        {
            var containers = response?.containers;
            if (containers == null)
                return;
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
        }
        
        public SyncContext CreateSyncContext(MemoryBuffer memoryBuffer, EventReceiver eventReceiver, JsonKey clientId) {
            return new SyncContext (sharedEnv, eventReceiver, memoryBuffer);
        }
    }
}

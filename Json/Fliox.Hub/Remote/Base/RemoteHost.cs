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
using static Friflo.Json.Fliox.Hub.Host.ExecutionType;

// ReSharper disable MethodHasAsyncOverload
// ReSharper disable InlineTemporaryVariable

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote
{
    public class RemoteHost : IDisposable, ILogSource
    {
        public   readonly   FlioxHub        localHub;
        public   readonly   SharedEnv       sharedEnv;
        public   readonly   HostMetrics     metrics = new HostMetrics();
        public              bool            useReaderPool;

        /// Only set to true for testing. It avoids an early out at <see cref="EventSubClient.SendEvents"/> 
        public              bool            fakeOpenClosedSockets;
        
        internal            FlioxHub        LocalHub    => localHub;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public              IHubLogger      Logger      => sharedEnv.hubLogger;


        public RemoteHost(FlioxHub hub, SharedEnv env) {
            sharedEnv   = env  ?? SharedEnv.Default;
            localHub    = hub;
        }
        
        public void Dispose() { }
        
        /// <summary>
        /// <b>Attention</b> returned <see cref="JsonResponse"/> is <b>only</b> valid until the passed <paramref name="mapper"/> is reused
        /// </summary>
        /// <remarks>
        /// <b>Hint</b> Copy / Paste implementation to avoid an async call in caller
        /// </remarks>
        public async Task<JsonResponse> ExecuteJsonRequestAsync(
            ObjectMapper    mapper,
            JsonValue       jsonRequest,
            SyncContext     syncContext)
        {
            // used response assignment instead of return in each branch to provide copy/paste code to avoid an async call in caller
            JsonResponse response;
            try {
                var syncRequest = RemoteUtils.ReadSyncRequest(mapper.reader, jsonRequest, out string error);
                if (error != null) {
                    response = JsonResponse.CreateError(mapper.writer, error, ErrorResponseType.BadResponse, null);
                } else {
                    var hub             = localHub;
                    var executionType   = hub.InitSyncRequest(syncRequest);
                    ExecuteSyncResult syncResult;
                    switch (executionType) {
                        case Async: syncResult = await hub.ExecuteRequestAsync (syncRequest, syncContext).ConfigureAwait(false); break;
                        case Queue: syncResult = await hub.QueueRequestAsync   (syncRequest, syncContext).ConfigureAwait(false); break;
                        default:    syncResult =       hub.ExecuteRequest      (syncRequest, syncContext);                       break;
                    }
                    response = CreateJsonResponse(syncResult, syncRequest.reqId, mapper.writer);
                }
            }
            catch (Exception e) {
                var errorMsg = ErrorResponse.ErrorFromException(e).ToString();
                response = JsonResponse.CreateError(mapper.writer, errorMsg, ErrorResponseType.Exception, null);
            }
            return response;
        }
        
        public JsonResponse ExecuteJsonRequest(
            ObjectMapper    mapper,
            in JsonValue    jsonRequest,
            SyncContext     syncContext)
        {
            // used response assignment instead of return in each branch to provide copy/paste code to avoid an async call in caller
            JsonResponse response;
            try {
                var syncRequest = RemoteUtils.ReadSyncRequest(mapper.reader, jsonRequest, out string error);
                if (error != null) {
                    response = JsonResponse.CreateError(mapper.writer, error, ErrorResponseType.BadResponse, null);
                } else {
                    localHub.InitSyncRequest(syncRequest);
                    var syncResult = localHub.ExecuteRequest(syncRequest, syncContext);
                
                    response = CreateJsonResponse(syncResult, syncRequest.reqId, mapper.writer);
                }
            }
            catch (Exception e) {
                var errorMsg = ErrorResponse.ErrorFromException(e).ToString();
                response = JsonResponse.CreateError(mapper.writer, errorMsg, ErrorResponseType.Exception, null);
            }
            return response;
        }
        
        public static JsonResponse CreateJsonResponse(in ExecuteSyncResult response, in int? reqId, ObjectWriter writer) {
            var responseError = response.error;
            if (responseError != null) {
                return JsonResponse.CreateError(writer, responseError.message, responseError.type, reqId);
            }
            SetContainerResults(response.success);
            response.Result.reqId   = reqId;
            JsonValue jsonResponse  = RemoteUtils.CreateProtocolMessage(response.Result, writer);
            return new JsonResponse(jsonResponse, JsonResponseStatus.Ok);
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
                    container.len = entities.Count;
                }
            }
        }
    }
}

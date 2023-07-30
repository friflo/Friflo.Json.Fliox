// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)

// ReSharper disable MethodHasAsyncOverload
// ReSharper disable InlineTemporaryVariable
namespace Friflo.Json.Fliox.Hub.Remote.Tools
{
    public static class RemoteHostUtils
    {
        public static JsonResponse ExecuteJsonRequest(
            FlioxHub        hub,
            ObjectMapper    mapper,
            in JsonValue    jsonRequest,
            SyncContext     syncContext)
        {
            // used response assignment instead of return in each branch to provide copy/paste code to avoid an async call in caller
            JsonResponse response;
            try {
                var syncRequest = MessageUtils.ReadSyncRequest(mapper.reader, hub.sharedEnv, jsonRequest, out string error);
                if (error != null) {
                    response = JsonResponse.CreateError(mapper.writer, error, ErrorResponseType.BadResponse, null);
                } else {
                    hub.InitSyncRequest(syncRequest);
                    var syncResult = hub.ExecuteRequest(syncRequest, syncContext);
                
                    response = CreateJsonResponse(syncResult, syncRequest.reqId, syncContext.sharedEnv, mapper.writer);
                }
            }
            catch (Exception e) {
                var errorMsg = ErrorResponse.ErrorFromException(e).ToString();
                response = JsonResponse.CreateError(mapper.writer, errorMsg, ErrorResponseType.Exception, null);
            }
            return response;
        }
        
        public static JsonResponse CreateJsonResponse(in ExecuteSyncResult response, in int? reqId, SharedEnv env, ObjectWriter writer) {
            var responseError = response.error;
            if (responseError != null) {
                return JsonResponse.CreateError(writer, responseError.message, responseError.type, reqId);
            }
            ResponseToJson(response.success);
            response.Result.reqId   = reqId;
            JsonValue jsonResponse  = MessageUtils.WriteProtocolMessage(response.Result, env, writer);
            return new JsonResponse(jsonResponse, JsonResponseStatus.Ok);
        }
        
        // SYNC_READ entities -> JSON
        public static void ResponseToJson(SyncResponse response)
        {
            foreach (var taskResult in response.tasks.GetReadOnlySpan())
            {
                switch (taskResult.TaskType) {
                    case TaskType.read:
                        var read = (ReadEntitiesResult)taskResult;
                        read.entities.EntitiesToJson(out read.set, out read.notFound, out read.errors);
                        ReferencesToJson(read.references);
                        break;
                    case TaskType.query:
                        var query = (QueryEntitiesResult)taskResult;
                        query.entities.EntitiesToJson(out query.set, out _, out query.errors);
                        query.len = query.entities.Length;
                        ReferencesToJson(query.references);
                        break;
                }
            }
        }
        
        private static void ReferencesToJson(List<ReferencesResult> references) {
            if (references == null) {
                return;
            }
            foreach (var result in references) {
                var values = result.entities.values;
                result.set = new ListOne<JsonValue>(values.Length);
                foreach (var value in values) {
                    result.set.Add(value.Json);
                }
                ReferencesToJson(result.references);
            }
        }
        
        /// Required only by <see cref="RemoteHostUtils"/>
        /// Distribute <see cref="ContainerEntities.entityMap"/> to <see cref="ContainerEntities.entities"/>,
        /// <see cref="ContainerEntities.notFound"/> and <see cref="ContainerEntities.errors"/> to simplify and
        /// minimize response by removing redundancy.
        /// <see cref="Client.FlioxClient.GetContainerResults"/> remap these properties.

        // SYNC_READ : entities -> JSON       OBSOLETE
        public static void SetContainerResults_Old(SyncResponse response) {
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

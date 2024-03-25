// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client.Internal;
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
        
        // SYNC_READ : entities -> JSON
        public static void ResponseToJson(SyncResponse response)
        {
            foreach (var taskResult in response.tasks.GetReadOnlySpan())
            {
                switch (taskResult.TaskType) {
                    case TaskType.read:
                        var read = (ReadEntitiesResult)taskResult;
                        Set.EntitiesToJson(read.entities.Values, out read.set, out read.notFound, out read.errors);
                        ReferencesToJson(read.references);
                        break;
                    case TaskType.query:
                        var query = (QueryEntitiesResult)taskResult;
                        Set.EntitiesToJson(query.entities.Values, out query.set, out _, out query.errors);
                        int len = query.set.Count;
                        if (len > 0) {
                            query.len = len;
                        }
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
                Set.EntitiesToJson(result.entities.Values, out result.set, out _, out result.errors);
                int len = result.set.Count;
                if (len > 0) {
                    result.len = len;
                }
                ReferencesToJson(result.references);
            }
        }
    }
}

// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Validation;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query;
using static Friflo.Json.Fliox.Hub.Host.ExecutionType;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)

// ReSharper disable MethodHasAsyncOverload
// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
namespace Friflo.Json.Fliox.Hub.Remote.Rest
{
    /// <summary>static class to ensure all REST methods are static</summary>
    internal static class RestUtils
    {
        // -------------------------------------- helper methods --------------------------------------
        internal static JsonKey[] GetKeysFromIds(string[] ids) {
            var keys = new JsonKey[ids.Length];
            for (int n = 0; n < ids.Length; n++) {
                keys[n] = new JsonKey(ids[n]);
            }
            return keys;
        }
        
        internal static bool IsValidJson (Pool pool, in JsonValue value, out string error) {
            error = null;
            if (value.IsNull())
                return true;
            using (var pooled = pool.TypeValidator.Get()) {
                var validator = pooled.instance;
                if (!validator.ValidateJson(value, out error)) {
                    return false;
                }
            }
            return true;
        }
        
        internal static string GetErrorType (string command) {
            return command != null ? "command error" : "message error";
        }
        
        private static bool TryParseParamAsInt(RequestContext context, string name, NameValueCollection queryParams, out int? result) {
            var valueStr = queryParams[name];
            if (valueStr == null) {
                result = null;
                return true;
            }
            if (int.TryParse(valueStr, out int value)) {
                result = value;
                return true;
            }
            context.WriteError("url parameter error", $"expect {name} as integer. was: {valueStr}", 400);
            result = null;
            return false;
        }
        
        private static bool HasQueryKey(NameValueCollection queryParams, string searchKey, out bool value, out string error) {
            var  allKeys  = queryParams.AllKeys;
            for (int n = 0; n < allKeys.Length; n++) {
                var paramValue = queryParams.Get(n); // what a crazy interface! :)
                if (paramValue == null)
                    continue;
                if (paramValue == searchKey || paramValue == "true") {
                    value = true;
                    error = null;
                    return true;
                }
                if (paramValue == "false") {
                    value = false;
                    error = null;
                    return true;
                }
                value = false;
                error = $"invalid boolean query parameter value: {paramValue}, parameter: {searchKey}";
                return false;
            }
            error = null;
            value = false;
            return true;
        }
        
        private static bool GerOrderByKey(RequestContext context, NameValueCollection queryParams, out SortOrder? value) {
            var orderByKey = queryParams["orderByKey"];
            switch (orderByKey) {
                case null:      value = null;           return true;
                case "asc":     value = SortOrder.asc;  return true;
                case "desc":    value = SortOrder.desc; return true;
                default:
                    context.WriteError("query parameter error", $"Expect asc|desc. was: {orderByKey}", 400);
                    value = null;
                    return false;
            }
        }
        
        // -------------------------------------- resource access  --------------------------------------
        internal static async Task GetEntitiesById(RequestContext context, ShortString database, ShortString container, JsonKey[] keys) {
            if (database.IsEqual(context.hub.database.nameShort))
                database = default;
            var readEntities = new ReadEntities { container = container, ids = new ListOne<JsonKey>(keys.Length)};
            foreach (var id in keys) {
                readEntities.ids.Add(id);    
            }
            var hub             = context.hub;
            var syncRequest     = CreateSyncRequest(context, database, readEntities, out var syncContext);
            var executionType   = hub.InitSyncRequest(syncRequest);
            ExecuteSyncResult syncResult;
            switch (executionType) {
                case Async: syncResult = await hub.ExecuteRequestAsync (syncRequest, syncContext).ConfigureAwait(false); break;
                case Queue: syncResult = await hub.QueueRequestAsync   (syncRequest, syncContext).ConfigureAwait(false); break;
                default:    syncResult =       hub.ExecuteRequest      (syncRequest, syncContext);                       break;
            }
            var restResult  = CreateRestResult(context, syncResult);
            if (restResult.taskResult == null)
                return;
            var readResult  = (ReadEntitiesResult)restResult.taskResult;
            var resultError = readResult.Error;
            if (resultError != null) {
                context.WriteError("read error", resultError.message, 500);
                return;
            }
            var values      = readResult.entities.Values;
            var entities    = new List<JsonValue>(values.Length);
            foreach (var value in values) {
                var json = value.Json;
                if (json.IsNull())
                    continue;
                entities.Add(json);
            }
            context.AddHeader("len", entities.Count.ToString()); // added to simplify debugging experience
            using (var pooled = context.ObjectMapper.Get()) {
                var writer          = MessageUtils.GetPrettyWriter(pooled.instance);
                var entitiesJson    = writer.WriteAsValue(entities);
                context.Write(entitiesJson, "application/json", 200);
            }
        }
        
        internal static async Task QueryEntities(RequestContext context, ShortString database, ShortString container, NameValueCollection queryParams) {
            if (database.IsEqual(context.hub.database.nameShort))
                database = default;
            var filter = CreateFilterTree(context, queryParams);
            if (filter.IsNull())
                return;
            if (!TryParseParamAsInt(context, "maxCount", queryParams, out int? maxCount))
                return;
            if (!TryParseParamAsInt(context, "limit",    queryParams, out int? limit))
                return;
            if (!GerOrderByKey(context, queryParams, out var orderByKey))
                return;
            var cursor          = queryParams["cursor"];
            var hub             = context.hub;
            var queryEntities   = new QueryEntities{
                container = container, orderByKey = orderByKey, filterTree = filter,
                maxCount = maxCount, cursor = cursor, limit = limit
            };
            var syncRequest     = CreateSyncRequest(context, database, queryEntities, out var syncContext);
            var executionType   = hub.InitSyncRequest(syncRequest);
            ExecuteSyncResult syncResult;
            switch (executionType) {
                case Async: syncResult = await hub.ExecuteRequestAsync (syncRequest, syncContext).ConfigureAwait(false); break;
                case Queue: syncResult = await hub.QueueRequestAsync   (syncRequest, syncContext).ConfigureAwait(false); break;
                default:    syncResult =       hub.ExecuteRequest      (syncRequest, syncContext);                       break;
            }
            var restResult  = CreateRestResult(context, syncResult);
            if (restResult.taskResult == null)
                return;
            var queryResult = (QueryEntitiesResult)restResult.taskResult;
            var resultError = queryResult.Error;
            if (resultError != null) {
                context.WriteError("query error", resultError.message, 500);
                return;
            }
            var taskResult  = (QueryEntitiesResult)restResult.syncResponse.tasks[0];
            if (taskResult.cursor != null) {
                context.AddHeader("cursor", taskResult.cursor);
            }
            var values      = queryResult.entities.Values;
            var entities    = new List<JsonValue>(values.Length);
            foreach  (var entity in values) {
                entities.Add(entity.Json);
            }
            context.AddHeader("len", entities.Count.ToString()); // added to simplify debugging experience
            using (var pooled = context.ObjectMapper.Get()) {
                var writer      = MessageUtils.GetPrettyWriter(pooled.instance);
                var response    = writer.WriteAsValue(entities);
                context.Write(response, "application/json", 200);
            }
        }
        
        /// enforce "o" as lambda argument
        // private const string DefaultParam   = "o";
        private const string InvalidFilter  = "invalid filter";
        
        private static JsonValue CreateFilterTree(RequestContext context, NameValueCollection queryParams) {
            var sharedCache         = context.SharedCache;
            var filterValidation    = sharedCache.GetValidationType(typeof(FilterOperation));
            using (var pooled = context.ObjectMapper.Get()) {
                var mapper = pooled.instance;
                var filter = CreateFilter(context, queryParams, mapper, filterValidation);
                if (filter == null)
                    return new JsonValue();
                var filterJson = mapper.writer.Write(filter);
                return new JsonValue(filterJson);
            }
        }
        
        private static FilterOperation CreateFilter(RequestContext context, NameValueCollection queryParams, ObjectMapper mapper, ValidationType filterValidation) {
            // --- handle filter expression
            var filter = queryParams["filter"];
            if (filter != null) {
                // var env   = new QueryEnv(DefaultParam); 
                var op    = Operation.Parse(filter, out string error);
                if (error != null) {
                    context.WriteError(InvalidFilter, error, 400);
                    return null;
                }
                if (op is FilterOperation filterOperation) {
                    return filterOperation;
                }
                context.WriteError(InvalidFilter, "filter must be boolean operation", 400);
                return null;
            }
            // --- handle filter tree
            var filterTree = queryParams["filter-tree"];
            if (filterTree == null) {
                return Operation.FilterTrue;
            }
            var pool = context.Pool;
            using (var pooled = pool.TypeValidator.Get()) {
                var validator   = pooled.instance;
                var json        = new JsonValue(filterTree);
                if (!validator.ValidateObject(json, filterValidation, out var error)) {
                    context.WriteError(InvalidFilter, error, 400);
                    return null;
                }
            }
            var reader = mapper.reader;
            var filterOp = mapper.reader.Read<FilterOperation>(filterTree);
            if (reader.Error.ErrSet) {
                context.WriteError(InvalidFilter, reader.Error.ToString(), 400);
                return null;
            }
            // Early out on invalid filter (e.g. symbol not found). Init() is cheap. If successful QueryEntities does the same check. 
            var operationCx = new OperationContext();
            if (!operationCx.Init(filterOp, out var message)) {
                context.WriteError(InvalidFilter, message, 400);
                return null;
            }
            return filterOp;
        }
        
        internal static async Task GetEntity(RequestContext context, ShortString database, ShortString container, string id) {
            if (database.IsEqual(context.hub.database.nameShort))
                database = default;
            var hub             = context.hub;
            var entityId        = new JsonKey(id);
            var readEntities    = new ReadEntities { container = container, ids = new ListOne<JsonKey> {entityId}};
            var syncRequest     = CreateSyncRequest(context, database, readEntities, out var syncContext);
            var executionType   = hub.InitSyncRequest(syncRequest);
            ExecuteSyncResult syncResult;
            switch (executionType) {
                case Async: syncResult = await hub.ExecuteRequestAsync (syncRequest, syncContext).ConfigureAwait(false); break;
                case Queue: syncResult = await hub.QueueRequestAsync   (syncRequest, syncContext).ConfigureAwait(false); break;
                default:    syncResult =       hub.ExecuteRequest      (syncRequest, syncContext);                       break;
            }
            var restResult  = CreateRestResult(context, syncResult);
            if (restResult.taskResult == null)
                return;
            var readResult  = (ReadEntitiesResult)restResult.taskResult;
            var resultError = readResult.Error;
            if (resultError != null) {
                context.WriteError("read error", resultError.message, 500);
                return;
            }
            var values  = readResult.entities.Values;
            if (values.Length < 1) {
                context.Write(new JsonValue(), "application/json", 404);
                return;
            }
            var value   = values[0];
            var entityError = value.Error;
            if (entityError != null) {
                context.WriteError("entity error", $"{entityError.type} - {entityError.message}", 404);
                return;
            }
            var entityStatus = value.Json.IsNull() ? 404 : 200;
            context.Write(value.Json, "application/json", entityStatus);
        }
        
        internal static async Task DeleteEntities(RequestContext context, ShortString database, ShortString container, JsonKey[] keys) {
            if (database.IsEqual(context.hub.database.nameShort))
                database = default;
            var deleteEntities  = new DeleteEntities { container = container, ids = new ListOne<JsonKey>(keys.Length) };
            foreach (var key in keys) {
                deleteEntities.ids.Add(key);
            }
            var hub             = context.hub;
            var syncRequest     = CreateSyncRequest(context, database, deleteEntities, out var syncContext);
            var executionType   = hub.InitSyncRequest(syncRequest);
            ExecuteSyncResult syncResult;
            switch (executionType) {
                case Async: syncResult = await hub.ExecuteRequestAsync (syncRequest, syncContext).ConfigureAwait(false); break;
                case Queue: syncResult = await hub.QueueRequestAsync   (syncRequest, syncContext).ConfigureAwait(false); break;
                default:    syncResult =       hub.ExecuteRequest      (syncRequest, syncContext);                       break;
            }
            var restResult      = CreateRestResult(context, syncResult);
            if (restResult.taskResult == null)
                return;
            var deleteResult    = (DeleteEntitiesResult)restResult.taskResult;
            var resultError     = deleteResult.Error;
            if (resultError != null) {
                context.WriteError("delete error", resultError.message, 500);
                return;
            }
            var entityErrors = deleteResult.errors;
            if (entityErrors != null) {
                var sb = new StringBuilder();
                FormatEntityErrors (entityErrors, sb);
                context.WriteError("DELETE errors", sb.ToString(), 400);
                return;
            }
            context.WriteString("deleted successful", "text/plain", 200);
        }
        
        private static List<JsonEntity> Body2JsonValues(RequestContext context, string id, string keyName, in JsonValue value, out string error)
        {
            var pool        = context.Pool;
            keyName         = keyName ?? "id";
            var entityId    = new JsonKey(id);
            if (id != null) {
                // check if given id matches entity key
                using (var pooled = pool.EntityProcessor.Get()) {
                    var processor = pooled.instance;
                    if (!processor.GetEntityKey(value, keyName, out JsonKey key, out error)) {
                        return null;
                    }
                    if (!entityId.IsEqual(key)) {
                        error = $"entity {keyName} != resource id. expect: {id}, was: {key.AsString()}";
                        return null;
                    }
                }
            }
            if (id != null) {
                error = null;
                return new List<JsonEntity> { new JsonEntity(value) };
            }
            // read entity array from request body
            using (var pooled = pool.EntityProcessor.Get()) {
                var processor = pooled.instance;
                var entities = processor.ReadJsonArray(value, out error);
                if (error != null) {
                    return null;
                }
                return entities;
            }
        }
        
        internal static async Task PutEntities(
            RequestContext      context,
            ShortString         database,
            ShortString         container,
            string              id,
            JsonValue           value,
            NameValueCollection queryParams)
        {
            var keyName     = queryParams["keyName"];
            if (!HasQueryKey(queryParams, "create", out bool create, out string error)) {
                context.WriteError("PUT failed", error, 400);
                return;
            }
            var type        = create ? TaskType.create : TaskType.upsert;

            if (database.IsEqual(context.hub.database.nameShort)) {
                database = default;
            }
            if (keyName == null) {
                var db = context.hub.FindDatabase(database);
                if (db != null) {
                    var cont = db.GetOrCreateContainer(container);
                    keyName = cont?.keyName;
                }
            }
            var entities = Body2JsonValues(context, id, keyName, value, out error);
            if (error != null) {
                context.WriteError("PUT error", error, 400);
                return;
            }
            SyncRequestTask task;
            switch (type) {
                case TaskType.upsert: task  = new UpsertEntities { container = container, keyName = keyName, entities = entities }; break;
                case TaskType.create: task  = new CreateEntities { container = container, keyName = keyName, entities = entities }; break;
                default:
                    throw new InvalidOperationException($"Invalid PUT type: {type}");
            }
            var hub             = context.hub;
            var syncRequest     = CreateSyncRequest(context, database, task, out var syncContext);
            var executionType   = hub.InitSyncRequest(syncRequest);
            ExecuteSyncResult syncResult;
            switch (executionType) {
                case Async: syncResult = await hub.ExecuteRequestAsync (syncRequest, syncContext).ConfigureAwait(false); break;
                case Queue: syncResult = await hub.QueueRequestAsync   (syncRequest, syncContext).ConfigureAwait(false); break;
                default:    syncResult =       hub.ExecuteRequest      (syncRequest, syncContext);                       break;
            }
            var restResult  = CreateRestResult(context, syncResult);
            if (restResult.taskResult == null)
                return;
            var taskResult  = (ITaskResultError)restResult.taskResult;
            var resultError = taskResult.Error;
            if (resultError != null) {
                context.WriteError("PUT error", resultError.message, 500);
                return;
            }
            List<EntityError> entityErrors;
            switch (type) {
                case TaskType.upsert: entityErrors = ((UpsertEntitiesResult)taskResult).errors;   break;
                case TaskType.create: entityErrors = ((CreateEntitiesResult)taskResult).errors;   break;
                default:
                    throw new InvalidOperationException($"Invalid PUT type: {type}");
            }
            if (entityErrors != null) {
                var sb = new StringBuilder();
                FormatEntityErrors (entityErrors, sb);
                context.WriteError("PUT errors", sb.ToString(), 400);
                return;
            }
            context.WriteString("PUT successful", "text/plain", 200);
        }
        
        internal static async Task MergeEntities(
            RequestContext      context,
            ShortString         database,
            ShortString         container,
            string              id,
            JsonValue           patch,
            NameValueCollection queryParams)
        {
            var keyName     = queryParams["keyName"];
            if (database.IsEqual(context.hub.database.nameShort))
                database = default;
            var patches = Body2JsonValues(context, id, keyName, patch, out string error);
            if (error != null) {
                context.WriteError("PATCH error", error, 400);
                return;
            }
            var hub             = context.hub;
            var task            = new MergeEntities { container = container, keyName = keyName, patches = patches };
            var syncRequest     = CreateSyncRequest(context, database, task, out var syncContext);
            var executionType   = hub.InitSyncRequest(syncRequest);
            ExecuteSyncResult syncResult;
            switch (executionType) {
                case Async: syncResult = await hub.ExecuteRequestAsync (syncRequest, syncContext).ConfigureAwait(false); break;
                case Queue: syncResult = await hub.QueueRequestAsync   (syncRequest, syncContext).ConfigureAwait(false); break;
                default:    syncResult =       hub.ExecuteRequest      (syncRequest, syncContext);                       break;
            }
            var restResult  = CreateRestResult(context, syncResult);
            if (restResult.taskResult == null)
                return;
            var mergeResult = (MergeEntitiesResult)restResult.taskResult;
            var resultError = mergeResult.Error;
            if (resultError != null) {
                context.WriteError("PATCH error", resultError.message, 500);
                return;
            }
            var entityErrors = mergeResult.errors;
            if (entityErrors != null) {
                var sb = new StringBuilder();
                FormatEntityErrors (entityErrors, sb);
                context.WriteError("PATCH errors", sb.ToString(), 400);
                return;
            }
            context.WriteString("PATCH successful", "text/plain", 200);
        }
        
        private static void FormatEntityErrors(List<EntityError> entityErrors, StringBuilder sb) {
            foreach (var error in entityErrors) {
                sb.Append("\n| ");
                sb.Append(error.type);
                sb.Append(": [");
                error.id.AppendTo(sb);
                sb.Append("], ");
                sb.Append(error.message);
            }
        }
        
        // ----------------------------------------- command / message -----------------------------------------
        internal static async Task Command(RequestContext context, ShortString database, string command, JsonValue param) {
            var hub             = context.hub;
            var sendCommand     = new SendCommand { name = new ShortString(command), param = param };
            var syncRequest     = CreateSyncRequest(context, database, sendCommand, out var syncContext);
            var executionType   = hub.InitSyncRequest(syncRequest);
            ExecuteSyncResult syncResult;
            switch (executionType) {
                case Async: syncResult = await hub.ExecuteRequestAsync (syncRequest, syncContext).ConfigureAwait(false); break;
                case Queue: syncResult = await hub.QueueRequestAsync   (syncRequest, syncContext).ConfigureAwait(false); break;
                default:    syncResult =       hub.ExecuteRequest      (syncRequest, syncContext);                       break;
            }
            var restResult  = CreateRestResult(context, syncResult);
            if (restResult.taskResult == null)
                return;
            var sendResult  = (SendCommandResult)restResult.taskResult;
            var resultError = sendResult.Error;
            if (resultError != null) {
                context.WriteError("send error", resultError.message, 500);
                return;
            }
            context.Write(sendResult.result, "application/json", 200);
        }
        
        internal static async Task Message(RequestContext context, ShortString database, string message, JsonValue param) {
            var hub             = context.hub;
            var sendMessage     = new SendMessage { name = new ShortString(message), param = param };
            var syncRequest     = CreateSyncRequest(context, database, sendMessage, out var syncContext);
            var executionType   = hub.InitSyncRequest(syncRequest);
            ExecuteSyncResult syncResult;
            switch (executionType) {
                case Async: syncResult = await hub.ExecuteRequestAsync (syncRequest, syncContext).ConfigureAwait(false); break;
                case Queue: syncResult = await hub.QueueRequestAsync   (syncRequest, syncContext).ConfigureAwait(false); break;
                default:    syncResult =       hub.ExecuteRequest      (syncRequest, syncContext);                       break;
            }
            var restResult  = CreateRestResult(context, syncResult);
            if (restResult.taskResult == null)
                return;
            var sendResult  = (SendMessageResult)restResult.taskResult;
            var resultError = sendResult.Error;
            if (resultError != null) {
                context.WriteError("message error", resultError.message, 500);
                return;
            }
            context.WriteString("\"received\"", "application/json", 200);
        }


        // ----------------------------------------- utils -----------------------------------------
        private static SyncRequest CreateSyncRequest (RequestContext context, in ShortString database, SyncRequestTask task, out SyncContext syncContext) {
            var tasks   = new ListOne<SyncRequestTask> { task };
            var userId  = context.headers.Cookie("fliox-user");
            var token   = context.headers.Cookie("fliox-token");
            var clientId= context.headers.Header("fliox-client");
            var syncRequest = new SyncRequest {
                database    = database,
                tasks       = tasks,
                userId      = new ShortString(userId),
                token       = new ShortString(token),
                clientId    = new ShortString(clientId)
            };
            var hub         = context.hub;
            syncContext     = new SyncContext(hub.sharedEnv, null, context.memoryBuffer) { Host = context.host }; // new context per request
            return syncRequest;
        }
        
        private static RestResult CreateRestResult (RequestContext context, ExecuteSyncResult result)
        {
            var error = result.error;
            if (error != null) {
                var status = error.type == ErrorResponseType.BadRequest ? 400 : 500;
                context.WriteError("sync error", error.message, status);
                return default;
            }
            var syncResponse    = result.success;
            var taskResult      = syncResponse.tasks[0];
            if (taskResult is TaskErrorResult errorResult) {
                int status;
                switch (errorResult.type) {
                    case TaskErrorType.InvalidTask:           status = 400;   break;
                    case TaskErrorType.PermissionDenied:      status = 403;   break;
                    case TaskErrorType.DatabaseError:         status = 500;   break;
                    case TaskErrorType.FilterError:           status = 400;   break;
                    case TaskErrorType.ValidationError:       status = 400;   break;
                    case TaskErrorType.CommandError:          status = 400;   break;
                    case TaskErrorType.None:                  status = 500;   break;
                    case TaskErrorType.UnhandledException:    status = 500;   break;
                    case TaskErrorType.NotImplemented:        status = 501;   break;
                    case TaskErrorType.SyncError:             status = 500;   break;
                    default:                                  status = 500;   break;
                }
                var errorMessage    = errorResult.message;
                var stacktrace      = errorResult.stacktrace;
                // append new line to stacktrace to avoid annoying scrolling in monaco editor when clicking below stacktrace
                var message         = stacktrace == null ? errorMessage : $"{errorMessage}\n{stacktrace}\n";
                context.WriteError(errorResult.type.ToString(), message, status);
                return default;
            }
            if (!syncResponse.clientId.IsNull()) {
                context.AddHeader("fliox-client", syncResponse.clientId.AsString());
            }
            return new RestResult (syncResponse, taskResult);
        }
    }
}
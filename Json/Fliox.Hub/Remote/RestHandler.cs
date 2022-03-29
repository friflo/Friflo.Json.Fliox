// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Validation;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query.Ops;
using Friflo.Json.Fliox.Transform.Query.Parser;

// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
namespace Friflo.Json.Fliox.Hub.Remote
{
    internal class RestHandler : IRequestHandler
    {
        private     const       string      RestBase = "/rest";
        
        public bool IsMatch(RequestContext context) {
            return RequestContext.IsBasePath(RestBase, context.route);
        }
            
        public async Task HandleRequest(RequestContext context) {
            var route = context.route;
            if (route.Length == RestBase.Length) {
                // --------------    GET            /rest
                if (context.method == "GET") { 
                    await Command(context, ClusterDB.Name, Std.HostCluster, new JsonValue()).ConfigureAwait(false); 
                    return;
                }
                context.WriteError("invalid request", "access to root only applicable with GET", 400);
                return;
            }
            var pool            = context.Pool;
            var method          = context.method;
            var queryParams     = HttpUtility.ParseQueryString(context.query);
            var command         = queryParams["command"];
            var message         = queryParams["message"];
            var isGet           = method == "GET";
            var isPost          = method == "POST";
            var resourcePath    = route.Substring(RestBase.Length + 1);
            var res             = new Resource(resourcePath);
            
            // ------------------    GET            /rest/database?command=...   /database?message=...
            //                       POST           /rest/database?command=...   /database?message=...
            if ((command != null || message != null) && (isGet || isPost)) {
                if (res.length != 1) {
                    context.WriteError(GetErrorType(command), $"messages & commands operate on database. was: {resourcePath}", 400);
                    return;
                }
                var database = res.database;
                if (database == EntityDatabase.MainDB)
                    database = null;
                JsonValue param;
                if (isPost) {
                    param = await JsonValue.ReadToEndAsync(context.body).ConfigureAwait(false);
                } else {
                    var queryValue = queryParams["param"];
                    param = new JsonValue(queryValue);
                }
                if (!IsValidJson(pool, param, out string error)) {
                    context.WriteError(GetErrorType(command), error, 400);
                    return;
                }
                if (command != null) {
                    await Command(context, database, command, param).ConfigureAwait(false); 
                    return;
                }
                await Message(context, database, message, param).ConfigureAwait(false);
                return;
            }

            if (res.error != null) {
                context.WriteError("invalid path /database/container/id", res.error, 400);
                return;
            }
            
            // ------------------    POST           /rest/database/container/bulk-get
            //                       POST           /rest/database/container/bulk-delete
            if (isPost && res.length == 3) {
                bool getEntities    = false;
                var bulk            = res.id;
                switch (bulk) {
                    case "bulk-get":
                        getEntities = true;
                        break;
                    case "bulk-delete":
                        break;
                    default:
                        context.WriteError($"post failed", "invalid bulk parameter: {bulk}", 400);
                        return;
                }
                JsonKey[] keys      = null;
                using (var pooled = pool.ObjectMapper.Get()) {
                    var reader  = pooled.instance.reader;
                    keys    = reader.Read<JsonKey[]>(context.body);
                    if (reader.Error.ErrSet) {
                        context.WriteError("invalid id list", reader.Error.ToString(), 400);
                        return;
                    }
                }
                if (getEntities) {
                    await GetEntitiesById (context, res.database, res.container, keys).ConfigureAwait(false);
                    return;
                }
                await DeleteEntities(context, res.database, res.container, keys).ConfigureAwait(false);
                return;
            }
            
            if (isGet) {
                // --------------    GET            /rest/database
                if (res.length == 1) {
                    await Command(context, res.database, Std.Containers, new JsonValue()).ConfigureAwait(false); 
                    return;
                }
                // --------------    GET            /rest/database/container
                if (res.length == 2) {
                    var idsParam = queryParams["ids"];
                    if (idsParam != null) {
                        var ids     = idsParam == "" ? Array.Empty<string>() : idsParam.Split(',');
                        var keys    = GetKeysFromIds(ids);
                        await GetEntitiesById (context, res.database, res.container, keys).ConfigureAwait(false);
                        return;
                    }
                    await QueryEntities(context, res.database, res.container, queryParams).ConfigureAwait(false);
                    return;
                }
                // --------------    GET            /rest/database/container/id
                if (res.length == 3) {
                    await GetEntity(context, res.database, res.container, res.id).ConfigureAwait(false);    
                    return;
                }
                context.WriteError("invalid request", "expect: /database/container/id", 400);
                return;
            }
            
            var isDelete = method == "DELETE";
            if (isDelete) {
                // --------------    DELETE         /rest/database/container/id
                if (res.length == 3) {
                    var keys = new [] { new JsonKey(res.id) };
                    await DeleteEntities(context, res.database, res.container, keys).ConfigureAwait(false);
                    return;
                }
                context.WriteError("invalid request", "expect: /database/container/id", 400);
                return;
            }
            // ------------------    PUT            /rest/database/container        ?create
            //                       PUT            /rest/database/container/id     ?create
            if (method == "PUT") {
                int len = res.length; 
                if (len != 2 && len != 3) {
                    context.WriteError("invalid PUT", "expect: /database/container or /database/container/id", 400);
                    return;
                }
                var value = await JsonValue.ReadToEndAsync(context.body).ConfigureAwait(false);
                if (!IsValidJson(pool, value, out string error)) {
                    context.WriteError("PUT failed", error, 400);
                    return;
                }
                var keyName     = queryParams["keyName"];
                var resource2   = len == 3 ? res.id : null;
                if (!HasQueryKey(queryParams, "create", out bool create, out error)) {
                    context.WriteError("PUT failed", error, 400);
                    return;
                }
                var type        = create ? TaskType.create : TaskType.upsert;
                await PutEntities(context, res.database, res.container, resource2, keyName, value, type).ConfigureAwait(false);
                return;
            }
            context.WriteError("invalid path/method", route, 400);
        }
        
        
        // -------------------------------------- helper methods --------------------------------------
        private static JsonKey[] GetKeysFromIds(string[] ids) {
            var keys = new JsonKey[ids.Length];
            for (int n = 0; n < ids.Length; n++) {
                keys[n] = new JsonKey(ids[n]);
            }
            return keys;
        }
        
        private static bool IsValidJson (Pool pool, JsonValue value, out string error) {
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
        
        private static string GetErrorType (string command) {
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
        
        // -------------------------------------- resource access  --------------------------------------
        private async Task GetEntitiesById(RequestContext context, string database, string container, JsonKey[] keys) {
            if (database == EntityDatabase.MainDB)
                database = null;
            var readEntitiesSet = new ReadEntitiesSet ();
            readEntitiesSet.ids.EnsureCapacity(keys.Length);
            foreach (var id in keys) {
                readEntitiesSet.ids.Add(id);    
            }
            var readEntities = new ReadEntities {
                container   = container,
                sets        = new List<ReadEntitiesSet> { readEntitiesSet }
            };
            var restResult = await ExecuteTask(context, database, readEntities).ConfigureAwait(false);
            
            if (restResult.taskResult == null)
                return;
            var readResult  = (ReadEntitiesResult)restResult.taskResult;
            var resultSet   = readResult.sets[0];
            var resultError = resultSet.Error;
            if (resultError != null) {
                context.WriteError("read error", resultError.message, 500);
                return;
            }
            var entityMap   = restResult.syncResponse.resultMap[container].entityMap;
            var entities    = new List<JsonValue>(entityMap.Count);
            foreach (var pair in entityMap) {
                entities.Add(pair.Value.Json);
            }
            context.AddHeader("count", entities.Count.ToString()); // added to simplify debugging experience
            var pool = context.Pool;
            using (var pooled = pool.ObjectMapper.Get()) {
                var writer = pooled.instance.writer;
                var entitiesJson = writer.Write(entities);
                context.Write(new JsonValue(entitiesJson), 0, "application/json", 200);
            }
        }
        
        private async Task QueryEntities(RequestContext context, string database, string container, NameValueCollection queryParams) {
            if (database == EntityDatabase.MainDB)
                database = null;
            var filter = CreateFilterTree(context, queryParams);
            if (filter.IsNull())
                return;
            if (!TryParseParamAsInt(context, "maxCount", queryParams, out int? maxCount))
                return;
            if (!TryParseParamAsInt(context, "limit",    queryParams, out int? limit))
                return;
            var cursor          = queryParams["cursor"];
            var queryEntities   = new QueryEntities{ container = container, filterTree = filter, maxCount = maxCount, cursor = cursor, limit = limit };
            var restResult      = await ExecuteTask(context, database, queryEntities).ConfigureAwait(false);
            
            if (restResult.taskResult == null)
                return;
            var queryResult  = (QueryEntitiesResult)restResult.taskResult;
            var resultError = queryResult.Error;
            if (resultError != null) {
                context.WriteError("query error", resultError.message, 500);
                return;
            }
            var taskResult  = (QueryEntitiesResult)restResult.syncResponse.tasks[0];
            if (taskResult.cursor != null) {
                context.AddHeader("cursor", taskResult.cursor);
            }
            var entityMap   = restResult.syncResponse.resultMap[container].entityMap;
            var entities    = new List<JsonValue>(entityMap.Count);
            foreach (var pair in entityMap) {
                entities.Add(pair.Value.Json);
            }
            context.AddHeader("count", entities.Count.ToString()); // added to simplify debugging experience
            var pool = context.Pool;
            using (var pooled = pool.ObjectMapper.Get()) {
                var writer = pooled.instance.writer;
                var entityArray = writer.WriteAsArray(entities);
                var response = new JsonValue(entityArray);
                context.Write(response, 0, "application/json", 200);
            }
        }
        
        /// enforce "o" as lambda argument
        private const string DefaultParam = "o";
        private const string InvalidFilter = "invalid filter";
        
        private JsonValue CreateFilterTree(RequestContext context, NameValueCollection queryParams) {
            var sharedCache         = context.SharedCache;
            var filterValidation    = sharedCache.GetValidationType(typeof(FilterOperation));
            var pool                = context.Pool;
            using (var pooled = pool.ObjectMapper.Get()) {
                var mapper = pooled.instance;
                var filter = CreateFilter(context, queryParams, mapper, filterValidation.Type);
                if (filter == null)
                    return new JsonValue();
                var filterJson = mapper.writer.Write(filter);
                return new JsonValue(filterJson);
            }
        }
        
        private FilterOperation CreateFilter(RequestContext context, NameValueCollection queryParams, ObjectMapper mapper, ValidationType filterValidation) {
            // --- handle filter expression
            var filter = queryParams["filter"];
            if (filter != null) {
                var env   = new QueryEnv(DefaultParam); 
                var op    = Operation.Parse(filter, out string error, env);
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
        
        private async Task GetEntity(RequestContext context, string database, string container, string id) {
            if (database == EntityDatabase.MainDB)
                database = null;
            var entityId        = new JsonKey(id);
            var readEntitiesSet = new ReadEntitiesSet ();
            readEntitiesSet.ids.Add(entityId);
            var readEntities = new ReadEntities {
                container   = container,
                sets        = new List<ReadEntitiesSet> { readEntitiesSet }
            };
            var restResult = await ExecuteTask(context, database, readEntities).ConfigureAwait(false);
            
            if (restResult.taskResult == null)
                return;
            var readResult  = (ReadEntitiesResult)restResult.taskResult;
            var resultSet   = readResult.sets[0];
            var resultError = resultSet.Error;
            if (resultError != null) {
                context.WriteError("read error", resultError.message, 500);
                return;
            }
            var content     = restResult.syncResponse.resultMap[container].entityMap[entityId];
            var entityError = content.Error;
            if (entityError != null) {
                context.WriteError("entity error", $"{entityError.type} - {entityError.message}", 404);
                return;
            }
            var entityStatus = content.Json.IsNull() ? 404 : 200;
            context.Write(content.Json, 0, "application/json", entityStatus);
        }
        
        private async Task DeleteEntities(RequestContext context, string database, string container, JsonKey[] keys) {
            if (database == EntityDatabase.MainDB)
                database = null;
            var deleteEntities  = new DeleteEntities { container = container };
            deleteEntities.ids.EnsureCapacity(keys.Length);
            foreach (var key in keys) {
                deleteEntities.ids.Add(key);
            }
            var restResult  = await ExecuteTask(context, database, deleteEntities).ConfigureAwait(false);
            
            if (restResult.taskResult == null)
                return;
            var deleteResult  = (DeleteEntitiesResult)restResult.taskResult;
            var resultError = deleteResult.Error;
            if (resultError != null) {
                context.WriteError("delete error", resultError.message, 500);
                return;
            }
            var entityErrors = restResult.syncResponse.deleteErrors;
            if (entityErrors != null) {
                var sb = new StringBuilder();
                FormatEntityErrors (entityErrors, sb);
                context.WriteError("DELETE errors", sb.ToString(), 400);
                return;
            }
            context.WriteString("deleted successful", "text/plain", 200);
        }
        
        private async Task PutEntities(RequestContext context, string database, string container, string id, string keyName, JsonValue value, TaskType type) {
            if (database == EntityDatabase.MainDB)
                database = null;
            var             pool = context.Pool;
            List<JsonValue> entities;
            if (id != null) {
                entities = new List<JsonValue> {value};
            } else {
                using (var pooled = pool.EntityProcessor.Get()) {
                    var processor = pooled.instance;
                    entities = processor.ReadJsonArray(value, out string error);
                    if (error != null) {
                        context.WriteError("PUT error", error, 400);
                        return;
                    }
                }
            }
            var entityId = new JsonKey(id);
            keyName = keyName ?? "id";
            if (id != null) {
                using (var pooled = pool.EntityProcessor.Get()) {
                    var processor = pooled.instance;
                    if (!processor.GetEntityKey(value, keyName, out JsonKey key, out string entityError)) {
                        context.WriteError("PUT error", entityError, 400);
                        return;
                    }
                    if (!entityId.IsEqual(key)) {
                        context.WriteError("PUT error", $"entity {keyName} != resource id. expect: {id}, was: {key.AsString()}", 400);
                        return;
                    }
                }
            }
            SyncRequestTask task;
            switch (type) {
                case TaskType.upsert: task  = new UpsertEntities { container = container, keyName = keyName, entities = entities }; break;
                case TaskType.create: task  = new CreateEntities { container = container, keyName = keyName, entities = entities }; break;
                default:
                    throw new InvalidOperationException($"Invalid PUT type: {type}");
            }
            var result = await ExecuteTask(context, database, task).ConfigureAwait(false);
            
            if (result.taskResult == null)
                return;
            var upsertResult    = (ICommandResult)result.taskResult;
            var resultError     = upsertResult.Error;
            if (resultError != null) {
                context.WriteError("PUT error", resultError.message, 500);
                return;
            }
            Dictionary<string, EntityErrors> entityErrors;
            switch (type) {
                case TaskType.upsert: entityErrors = result.syncResponse.upsertErrors; break;
                case TaskType.create: entityErrors = result.syncResponse.createErrors; break;
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
        
        private static void FormatEntityErrors(Dictionary<string, EntityErrors> entityErrors, StringBuilder sb) {
            foreach (var pair in entityErrors) {
                var errors = pair.Value.errors;
                foreach (var errorPair in errors) {
                    var error = errorPair.Value;
                    sb.Append("\n| ");
                    sb.Append(error.type);
                    sb.Append(": [");
                    sb.Append(error.id);
                    sb.Append("], ");
                    sb.Append(error.message);
                }
            }
        }
        
        // ----------------------------------------- command / message -----------------------------------------
        private async Task Command(RequestContext context, string database, string command, JsonValue param) {
            var sendCommand = new SendCommand { name = command, param = param };
            var restResult  = await ExecuteTask(context, database, sendCommand).ConfigureAwait(false);
            
            if (restResult.taskResult == null)
                return;
            var sendResult  = (SendCommandResult)restResult.taskResult;
            var resultError = sendResult.Error;
            if (resultError != null) {
                context.WriteError("send error", resultError.message, 500);
                return;
            }
            context.Write(sendResult.result, 0, "application/json", 200);
        }
        
        private async Task Message(RequestContext context, string database, string message, JsonValue param) {
            var sendMessage = new SendMessage { name = message, param = param };
            var restResult  = await ExecuteTask(context, database, sendMessage).ConfigureAwait(false);
            
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
        private async Task<RestResult> ExecuteTask (RequestContext context, string database, SyncRequestTask task) {
            var tasks   = new List<SyncRequestTask> { task };
            var userId  = context.cookies["fliox-user"];
            var token   = context.cookies["fliox-token"];
            var synRequest = new SyncRequest {
                database    = database,
                tasks       = tasks,
                userId      = new JsonKey(userId),
                token       = token
            };
            var hub             = context.hub;
            var sharedCache     = context.SharedCache;
            var localPool       = new Pool(hub.sharedEnv);
            var executeContext  = new ExecuteContext(localPool, null, sharedCache);
            var result = await hub.ExecuteSync(synRequest, executeContext).ConfigureAwait(false);
            
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
                    case TaskErrorResultType.InvalidTask:           status = 400;   break;
                    case TaskErrorResultType.PermissionDenied:      status = 403;   break;
                    case TaskErrorResultType.DatabaseError:         status = 500;   break;
                    case TaskErrorResultType.FilterError:           status = 400;   break;
                    case TaskErrorResultType.ValidationError:       status = 400;   break;
                    case TaskErrorResultType.CommandError:          status = 400;   break;
                    case TaskErrorResultType.None:                  status = 500;   break;
                    case TaskErrorResultType.UnhandledException:    status = 500;   break;
                    case TaskErrorResultType.NotImplemented:        status = 501;   break;
                    case TaskErrorResultType.SyncError:             status = 500;   break;
                    default:                                        status = 500;   break;
                }
                var errorMessage    = errorResult.message;
                var stacktrace      = errorResult.stacktrace;
                // append new line to stacktrace to avoid annoying scrolling in monaco editor when clicking below stacktrace
                var message         = stacktrace == null ? errorMessage : $"{errorMessage}\n{stacktrace}\n";
                context.WriteError(errorResult.type.ToString(), message, status);
                return default;
            }
            return new RestResult { taskResult = taskResult, syncResponse = syncResponse };
        }
    }
    
    internal struct RestResult {
        internal    SyncResponse    syncResponse;
        internal    SyncTaskResult  taskResult;
    } 

    internal readonly struct Resource {
        internal  readonly  string  database;
        internal  readonly  string  container;
        internal  readonly  string  id;

        internal  readonly  int     length;
        internal  readonly  string  error;

        internal Resource (string resourcePath) {
            var resources   = resourcePath.Split('/');
            length          = resources.Length;
            if (resources[length - 1] == "")
                length--;
            database    = length > 0 ? resources[0] : null;
            container   = length > 1 ? resources[1] : null;
            id          = length > 2 ? resources[2] : null;
            if (database == "") { 
                error = "empty database path";
                return;
            }
            if (container == "") { 
                error = "empty container path";
                return;
            }
            if (id == "") { 
                error = "empty id path";
            }
            error = null;
        }
    }
}
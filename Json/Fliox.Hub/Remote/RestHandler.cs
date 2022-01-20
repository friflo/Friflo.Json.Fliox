// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Web;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query.Ops;
using Friflo.Json.Fliox.Transform.Query.Parser;

// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
namespace Friflo.Json.Fliox.Hub.Remote
{
    public class RestHandler : IRequestHandler
    {
        private     const       string      RestBase = "/rest";
        private     readonly    FlioxHub    hub;
        private     readonly    Pool        pool;
        
        public RestHandler (FlioxHub hub) {
            this.hub    = hub;
            pool        = hub.sharedEnv.Pool;
        }
        
        public bool IsMatch(RequestContext context) {
            return RequestContext.IsBasePath(RestBase, context.path);
        }
            
        public async Task HandleRequest(RequestContext context) {
            var path    = context.path;
            if (path.Length == RestBase.Length) {
                // ------------------    GET            (no path)
                if (context.method == "GET") { 
                    await Command(context, ClusterDB.Name, StdCommand.DbHubCluster, new JsonValue()).ConfigureAwait(false); 
                    return;
                }
                context.WriteError("invalid request", "access to root only applicable with GET", 400);
                return;
            }
            var method          = context.method;
            var queryParams     = HttpUtility.ParseQueryString(context.query);
            var command         = queryParams["command"];
            var message         = queryParams["message"];
            var isGet           = method == "GET";
            var isPost          = method == "POST";
            var resourcePath    = path.Substring(RestBase.Length + 1);
            
            // ------------------    GET / POST     /database?command=...   /database?message=...
            if ((command != null || message != null) && (isGet || isPost)) {
                var database = resourcePath;
                if (database.IndexOf('/') != -1) {
                    context.WriteError(GetErrorType(command), $"messages & commands operate on database. was: {database}", 400);
                    return;
                }
                if (database == EntityDatabase.MainDB)
                    database = null;
                JsonValue value;
                if (isPost) {
                    value = await JsonValue.ReadToEndAsync(context.body).ConfigureAwait(false);
                } else {
                    var queryValue = queryParams["value"];
                    value = new JsonValue(queryValue);
                }
                if (!IsValidJson(pool, value, out string error)) {
                    context.WriteError(GetErrorType(command), error, 400);
                    return;
                }
                if (command != null) {
                    await Command(context, database, command, value).ConfigureAwait(false);
                    return;
                }
                await Message(context, database, message, value).ConfigureAwait(false);
                return;
            }
            var resource = resourcePath.Split('/');
            var resourceError = GetResourceError(resource);
            if (resourceError != null) {
                context.WriteError("invalid path /database/container/id", resourceError, 400);
                return;
            }

            // ------------------    GET            /database
            if (isGet && resource.Length == 1) {
                await Command(context, resource[0], StdCommand.DbContainers, new JsonValue()).ConfigureAwait(false); 
                return;
            }
            // ------------------    GET            /database/container
            if (isGet && resource.Length == 2) {
                var idsParam = queryParams["ids"];
                if (idsParam != null) {
                    var ids = idsParam.Split(',');
                    await GetEntitiesById (context, resource[0], resource[1], ids).ConfigureAwait(false);
                    return;
                }
                await GetEntities(context, resource[0], resource[1], queryParams).ConfigureAwait(false);
                return;
            }
            // ------------------    GET            /database/container/id
            if (isGet) {
                if (resource.Length == 3) {
                    await GetEntity(context, resource[0], resource[1], resource[2]).ConfigureAwait(false);    
                    return;
                }
                context.WriteError("invalid request", "expect: /database/container/id", 400);
                return;
            }
            
            // ------------------    DELETE         /database/container/id  or /database/container?ids=id1,id2,...
            var isDelete = method == "DELETE";
            if (isDelete) {
                if (resource.Length == 3) {
                    await DeleteEntity(context, resource[0], resource[1], new []{resource[2]}).ConfigureAwait(false);
                    return;
                }
                if (resource.Length == 2) {
                    var idsParam = queryParams["ids"];
                    var ids = idsParam.Split(',');
                    await DeleteEntity(context, resource[0], resource[1], ids).ConfigureAwait(false);
                    return;
                }
                context.WriteError("invalid request", "expect: /database/container?ids=id1,id2,... or /database/container/id", 400);
                return;
            }
            // ------------------    PUT            /database/container/id  or  /database/container
            if (method == "PUT") {
                int len = resource.Length; 
                if (len != 2 && len != 3) {
                    context.WriteError("invalid PUT", "expect: /database/container or /database/container/id", 400);
                    return;
                }
                var value = await JsonValue.ReadToEndAsync(context.body).ConfigureAwait(false);
                if (!IsValidJson(pool, value, out string error)) {
                    context.WriteError("PUT error", error, 400);
                    return;
                }
                var keyName = queryParams["keyName"];
                var resource2 = len == 3 ? resource[2] : null;
                await UpsertEntity(context, resource[0], resource[1], resource2, keyName, value).ConfigureAwait(false);
                return;
            }
            context.WriteError("invalid path/method", path, 400);
        }
        
        private static string GetResourceError(string[] resource) {
            if (resource[0] == "")
                return "missing database path";
            if (resource.Length == 2 && resource[1] == "")
                return "missing container path";
            if (resource.Length == 3 && resource[2] == "")
                return "missing id path";
            return null;
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
        
        // -------------------------------------- resource access  --------------------------------------
        private async Task GetEntitiesById(RequestContext context, string database, string container, string[] ids) {
            if (database == EntityDatabase.MainDB)
                database = null;
            var readEntitiesSet = new ReadEntitiesSet ();
            readEntitiesSet.ids.EnsureCapacity(ids.Length);
            foreach (var id in ids) {
                readEntitiesSet.ids.Add(new JsonKey(id));    
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
            using (var pooled = pool.ObjectMapper.Get()) {
                var writer = pooled.instance.writer;
                var entitiesJson = writer.Write(entities);
                context.Write(new JsonValue(entitiesJson), 0, "application/json", 200);
            }
        }
        
        private async Task GetEntities(RequestContext context, string database, string container, NameValueCollection queryParams) {
            if (database == EntityDatabase.MainDB)
                database = null;
            var filter = CreateFilter(context, queryParams);
            if (filter == null)
                return;
            var queryEntities   = new QueryEntities{ container = container, filterTree = filter };
            var restResult      = await ExecuteTask(context, database, queryEntities).ConfigureAwait(false);
            
            if (restResult.taskResult == null)
                return;
            var queryResult  = (QueryEntitiesResult)restResult.taskResult;
            var resultError = queryResult.Error;
            if (resultError != null) {
                context.WriteError("query error", resultError.message, 500);
                return;
            }
            var entityMap     = restResult.syncResponse.resultMap[container].entityMap;
            var entities = new List<JsonValue>(entityMap.Count);
            foreach (var pair in entityMap) {
                entities.Add(pair.Value.Json);
            }
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
        
        private FilterOperation CreateFilter(RequestContext context, NameValueCollection queryParams) {
            // --- handle filter expression
            var filter = queryParams["filter"];
            if (filter != null) {
                var env         = new QueryEnv(DefaultParam); 
                var filterOp    = Operation.Parse(filter, out string error, env);
                if (error != null) {
                    context.WriteError(InvalidFilter, error, 400);
                    return null;
                }
                if (filterOp is FilterOperation op) {
                    return op;
                }
                context.WriteError(InvalidFilter, "filter must be boolean operation", 400);
                return null;
            }
            // --- handle filter tree
            var queryFilter = queryParams["filter-tree"];
            if (queryFilter == null) {
                return Operation.FilterTrue;
            }
            using (var pooled = pool.ObjectMapper.Get()) {
                var reader = pooled.instance.reader;
                var filterOp = reader.Read<FilterOperation>(queryFilter);
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
        
        private async Task DeleteEntity(RequestContext context, string database, string container, string[] ids) {
            if (database == EntityDatabase.MainDB)
                database = null;
            var deleteEntities  = new DeleteEntities { container = container };
            deleteEntities.ids.EnsureCapacity(ids.Length);
            foreach (var id in ids) {
                var entityId = new JsonKey(id);
                deleteEntities.ids.Add(entityId);
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
            context.WriteString("deleted successful", "text/plain");
        }
        
        private async Task UpsertEntity(RequestContext context, string database, string container, string id, string keyName, JsonValue value) {
            if (database == EntityDatabase.MainDB)
                database = null;
            List<JsonValue> entities;
            if (id != null) {
                entities = new List<JsonValue> {value};
            } else {
                using (var pooled = pool.ObjectMapper.Get()) {
                    var reader = pooled.instance.reader;
                    entities = reader.Read<List<JsonValue>>(value);
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
            var upsertEntities  = new UpsertEntities {
                container   = container,
                keyName     = keyName,
                entities    = entities
            };
            var restResult  = await ExecuteTask(context, database, upsertEntities).ConfigureAwait(false);
            
            if (restResult.taskResult == null)
                return;
            var upsertResult  = (UpsertEntitiesResult)restResult.taskResult;
            var resultError = upsertResult.Error;
            if (resultError != null) {
                context.WriteError("PUT error", resultError.message, 500);
                return;
            }
            var upsertErrors = restResult.syncResponse.upsertErrors;
            if (upsertErrors != null) {
                var error = upsertErrors[container].errors[entityId];
                context.WriteError("PUT error", error.message, 400);
                return;
            }
            context.WriteString("PUT successful", "text/plain");
        }
        
        // ----------------------------------------- command / message -----------------------------------------
        private async Task Command(RequestContext context, string database, string command, JsonValue value) {
            var sendCommand = new SendCommand { name    = command, value   = value };
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
        
        private async Task Message(RequestContext context, string database, string message, JsonValue value) {
            var sendMessage = new SendMessage { name = message, value   = value };
            var restResult  = await ExecuteTask(context, database, sendMessage).ConfigureAwait(false);
            
            if (restResult.taskResult == null)
                return;
            var sendResult  = (SendMessageResult)restResult.taskResult;
            var resultError = sendResult.Error;
            if (resultError != null) {
                context.WriteError("message error", resultError.message, 500);
                return;
            }
            context.WriteString("received", "text/plain");
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
            var localPool       = new Pool(hub.sharedEnv);
            var messageContext  = new MessageContext(localPool, null);
            var result = await hub.ExecuteSync(synRequest, messageContext).ConfigureAwait(false);
            
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
                    case TaskErrorResultType.None:                  status = 500;   break;
                    case TaskErrorResultType.UnhandledException:    status = 500;   break;
                    case TaskErrorResultType.NotImplemented:        status = 501;   break;
                    case TaskErrorResultType.SyncError:             status = 500;   break;
                    default:                                        status = 500;   break;
                }
                context.WriteError(errorResult.type.ToString(), errorResult.message, status);
                return default;
            }
            return new RestResult { taskResult = taskResult, syncResponse = syncResponse };
        }
    }
    
    internal struct RestResult {
        internal    SyncResponse    syncResponse;
        internal    SyncTaskResult  taskResult;
    } 
}
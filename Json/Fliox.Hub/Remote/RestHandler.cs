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

// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
namespace Friflo.Json.Fliox.Hub.Remote
{
    public class RestHandler : IRequestHandler
    {
        private     const string    RestBase = "/rest";
        private     readonly        FlioxHub    hub;
        
        public RestHandler (FlioxHub    hub) {
            this.hub = hub;
        }
            
        public async Task<bool> HandleRequest(RequestContext context) {
            var path    = context.path;
            if (!path.StartsWith(RestBase))
                return false;
            if (path.Length == RestBase.Length) {
                // ------------------    GET            (no path)
                if (context.method == "GET") { 
                    await Command(context, ClusterDB.Name, StdCommand.DbList, new JsonValue()); 
                    return true;
                }
                context.WriteError("invalid request", "access to root only applicable with GET", 400);
                return true;
            }
            if (path[RestBase.Length] != '/') {
                return false;
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
                    return true;
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
                if (!IsValidJson(hub.sharedEnv, value, out string error)) {
                    context.WriteError(GetErrorType(command), error, 400);
                    return true;
                }
                if (command != null) {
                    await Command(context, database, command, value);
                    return true;
                }
                await Message(context, database, message, value);
                return true;
            }
            var resource = resourcePath.Split('/');
            var resourceError = GetResourceError(resource);
            if (resourceError != null) {
                context.WriteError("invalid path /database/container/id", resourceError, 400);
                return true;
            }
            var isDelete = method == "DELETE";

            // ------------------    GET            /database
            if (isGet && resource.Length == 1) {
                await Command(context, resource[0], StdCommand.DbContainers, new JsonValue()); 
                return true;
            }
            // ------------------    GET            /database/container
            if (isGet && resource.Length == 2) {
                await GetEntities(context, resource[0], resource[1], queryParams);
                return true;
            }
            // ------------------    GET / DELETE   /database/container/id
            if (isGet || isDelete) {
                if (resource.Length == 3) {
                    if (isGet) {
                        await GetEntity(context, resource[0], resource[1], resource[2]);    
                        return true;
                    }
                    await DeleteEntity(context, resource[0], resource[1], resource[2]);
                    return true;
                }
                context.WriteError("invalid request", "expect: /database/container/id", 400);
                return true;
            }
            // ------------------    PUT            /database/container
            if (method == "PUT") {
                if (resource.Length != 3) {
                    context.WriteError("invalid PUT", "expect: /database/container/id", 400);
                    return true;
                }
                var value = await JsonValue.ReadToEndAsync(context.body).ConfigureAwait(false);
                if (!IsValidJson(hub.sharedEnv, value, out string error)) {
                    context.WriteError("PUT error", error, 400);
                    return true;
                }
                var keyName = queryParams["keyName"];
                await UpsertEntity(context, resource[0], resource[1], resource[2], keyName, value);
                return true;
            }
            return false;
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
        
        private static bool IsValidJson (SharedEnv sharedEnv, JsonValue value, out string error) {
            error = null;
            if (value.IsNull())
                return true;
            using (var pooled = sharedEnv.Pool.TypeValidator.Get()) {
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
        private async Task GetEntities(RequestContext context, string database, string container, NameValueCollection queryParams) {
            if (database == EntityDatabase.MainDB)
                database = null;
            var filter = CreateFilter(context, queryParams);
            if (filter == null)
                return;
            var queryEntities   = new QueryEntities{ container = container, filter = filter };
            var restResult      = await ExecuteTask(context, database, queryEntities);
            
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
            using (var pooled = hub.sharedEnv.Pool.ObjectMapper.Get()) {
                var writer = pooled.instance.writer;
                var entityArray = writer.WriteAsArray(entities);
                var response = new JsonValue(entityArray);
                context.Write(response, 0, "application/json", 200);
            }
        }
        
        private FilterOperation CreateFilter(RequestContext context, NameValueCollection queryParams) {
            // reserved query parameter "filter" for alternative short query expression
            var queryFilter = queryParams["query-filter"];
            if (queryFilter == null)
                return Operation.FilterTrue;
            using (var pooled = hub.sharedEnv.Pool.ObjectMapper.Get()) {
                var reader = pooled.instance.reader;
                var filter = reader.Read<FilterOperation>(queryFilter);
                if (!reader.Error.ErrSet)
                    return filter;
                context.WriteError("query filter error", reader.Error.ToString(), 400);
                return null;
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
            var restResult = await ExecuteTask(context, database, readEntities);
            
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
        
        private async Task DeleteEntity(RequestContext context, string database, string container, string id) {
            if (database == EntityDatabase.MainDB)
                database = null;
            var entityId        = new JsonKey(id);
            var deleteEntities  = new DeleteEntities { container = container };
            deleteEntities.ids.Add(entityId);
            var restResult  = await ExecuteTask(context, database, deleteEntities);
            
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
            var entityId = new JsonKey(id);
            keyName = keyName ?? "id";
            using (var pooled = hub.sharedEnv.Pool.EntityProcessor.Get()) {
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
            var upsertEntities  = new UpsertEntities {
                container   = container,
                keyName     = keyName,
                entities    = new List<JsonValue> {value} 
            };
            var restResult  = await ExecuteTask(context, database, upsertEntities);
            
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
            var restResult  = await ExecuteTask(context, database, sendCommand);
            
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
            var restResult  = await ExecuteTask(context, database, sendMessage);
            
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
            var pool            = new Pool(hub.sharedEnv);
            var messageContext  = new MessageContext(pool, null);
            var result = await hub.ExecuteSync(synRequest, messageContext);
            
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
                    case TaskErrorResultType.None:                  status = 500;   break;
                    case TaskErrorResultType.UnhandledException:    status = 500;   break;
                    case TaskErrorResultType.NotImplemented:        status = 500;   break;
                    case TaskErrorResultType.SyncError:             status = 500;   break;
                    default:                                        status = 500;   break;
                }
                context.WriteError("task error", $"{errorResult.type} - {errorResult.message}", status);
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
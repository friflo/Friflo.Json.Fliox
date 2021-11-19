// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Remote
{
    public class RestHandler : IRequestHandler
    {
        private     const string    RestBase = "/rest/";
        private     readonly        FlioxHub    hub;
        
        public RestHandler (FlioxHub    hub) {
            this.hub = hub;
        }
            
        public async Task<bool> HandleRequest(RequestContext context) {
            var path = context.path;
            if (!path.StartsWith(RestBase))
                return false;
            var queryKeyValues  = HttpUtility.ParseQueryString(context.query);
            var command         = queryKeyValues["command"];
            var message         = queryKeyValues["message"];
            var isGet           = context.method == "GET";
            var isPost          = context.method == "POST";
            
            if ((command != null || message != null) && (isGet || isPost)) {
                var database = path.Substring(RestBase.Length);
                if (database.IndexOf('/') != -1) {
                    context.WriteError(GetErrorType(command), $"database must not contain /. database: {database}", 400);
                    return true;
                }
                if (database == "default")
                    database = null;
                JsonValue value;
                if (isPost) {
                    value = await JsonValue.ReadToEndAsync(context.body).ConfigureAwait(false);
                } else {
                    var queryValue = queryKeyValues["value"];
                    value = new JsonValue(queryValue);
                }
                if (!value.IsNull()) {
                    using (var pooled = hub.sharedEnv.Pool.TypeValidator.Get()) {
                        var validator = pooled.instance;
                        if (!validator.ValidateJson(value, out string error)) {
                            context.WriteError(GetErrorType(command), error, 400);
                            return true;
                        }
                    }
                }
                if (command != null) {
                    await HandleCommand(context, database, command, value);
                    return true;
                }
                await HandleMessage(context, database, message, value);
                return true;
            }
            if (isGet) {
                await HandleGet(context);
                return true;
            }
            return false;
        }
        
        // ----------------------------------------- POST -----------------------------------------
        private Task HandleGet(RequestContext context) {
            var path    = context.path.Substring(RestBase.Length);
            var section = path.Split('/');
            if (section.Length == 3) {
                return HandleGetEntity(context, section[0], section[1], section[2]);
            }
            context.WriteError("invalid GET request", "expect: /database/container/id", 400);
            return Task.CompletedTask;
        }
        
        private async Task HandleGetEntity(RequestContext context, string database, string container, string id) {
            if (database == "default")
                database = null;
            var entityId        = new JsonKey(id);
            var readEntitiesSet = new ReadEntitiesSet ();
            readEntitiesSet.ids.Add(entityId);
            var readEntities = new ReadEntities {
                container   = container,
                reads       = new List<ReadEntitiesSet> { readEntitiesSet }
            };
            var restResult = await ExecuteTask(context, database, readEntities); 
            if (restResult.taskResult == null)
                return;

            var readResult  = (ReadEntitiesResult)restResult.taskResult;
            var resultSet   = readResult.reads[0];
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
        
        // ----------------------------------------- command / message -----------------------------------------
        private static string GetErrorType (string command) {
            return command != null ? "command error" : "message error";
        }
        
        private async Task HandleCommand(RequestContext context, string database, string command, JsonValue value) {
            var sendCommand = new SendCommand {
                name    = command,
                value   = value
            };
            var restResult = await ExecuteTask(context, database, sendCommand); 
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
        
        private async Task HandleMessage(RequestContext context, string database, string message, JsonValue value) {
            var sendCommand = new SendMessage {
                name    = message,
                value   = value
            };
            var restResult = await ExecuteTask(context, database, sendCommand); 
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
            var synRequest = new SyncRequest {
                database    = database,
                tasks       = tasks,
                userId      = new JsonKey("admin"),
                token       = "admin"
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
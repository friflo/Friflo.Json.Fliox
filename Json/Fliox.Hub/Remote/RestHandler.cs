// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
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
            if (!context.path.StartsWith(RestBase))
                return false;
            var query   = context.query;
            var command = query.StartsWith("?command=") ? query.Substring("?command=".Length) : null;
            var message = query.StartsWith("?message=") ? query.Substring("?message=".Length) : null;
            var isGet   = context.method == "GET";
            var isPost  = context.method == "POST";
            
            if (isGet || isPost) {
                if (command != null) {
                    await HandleCommand(context, command);
                    return true;
                }
                if (message != null) {
                    await HandleMessage(context, message);
                    return true;
                }
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
            var readError   = resultSet.Error;
            if (readError != null) {
                context.WriteError("read error", readError.message, 500);
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
        
        // ----------------------------------------- command -----------------------------------------
        private Task HandleCommand(RequestContext context, string command) {
            context.WriteError("invalid command", command, 400);
            return Task.CompletedTask;
        }
        
        // ----------------------------------------- message -----------------------------------------
        private Task HandleMessage(RequestContext context, string message) {
            context.WriteError("invalid message", message, 400);
            return Task.CompletedTask;
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
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
            
        public Task<bool> HandleRequest(RequestContext context) {
            if (!context.path.StartsWith(RestBase))
                return Task.FromResult(false);
            if (context.method == "GET") {
                return HandleGet(context);
            }
            return Task.FromResult(false);
        }
        
        private async Task<bool> HandleGet(RequestContext context) {
            var path    = context.path.Substring(RestBase.Length);
            var section = path.Split('/');
            if (section.Length < 3) {
                context.WriteError("request error", "GET expect: /database/container/id", 400);
                return true;
            }
            var database    = section[0];
            if (database == "default")
                database = null;
            var container       = section[1];
            var entityId        = new JsonKey(section[2]);
            var readEntitiesSet = new ReadEntitiesSet ();
            readEntitiesSet.ids.Add(entityId);
            var readEntities = new ReadEntities {
                container   = container,
                reads       = new List<ReadEntitiesSet> { readEntitiesSet }
            };
            var restResult = await ExecuteTask(context, database, readEntities); 
            if (restResult.taskResult == null)
                return true;

            var readResult  = (ReadEntitiesResult)restResult.taskResult;
            var resultSet   = readResult.reads[0];
            var readError   = resultSet.Error;
            if (readError != null) {
                context.WriteError("read error", readError.message, 500);
                return true;
            }
            var content     = restResult.syncResponse.resultMap[container].entityMap[entityId];
            var entityError = content.Error;
            if (entityError != null) {
                context.WriteError("entity error", $"{entityError.type} - {entityError.message}", 404);
                return true;
            }
            var entityStatus = content.Json.IsNull() ? 404 : 200;
            context.Write(content.Json, 0, "application/json", entityStatus);
            return true;
        }
        
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
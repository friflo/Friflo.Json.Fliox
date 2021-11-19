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
                context.WriteString("GET expect: /database/container/id", "application/json", 400);
                return true;
            }
            var database    = section[0];
            if (database == "default")
                database = null;
            var container   = section[1];
            var entityId    = new JsonKey(section[2]);
            var readEntitiesSet = new ReadEntitiesSet ();
            readEntitiesSet.ids.Add(entityId);
            var tasks   = new List<SyncRequestTask> {
                new ReadEntities {
                    container   = container,
                    reads       = new List<ReadEntitiesSet> { readEntitiesSet }
                }
            };
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
                context.WriteString(error.message, "application/json", 400);
                return true;
            }
            var success     = result.success;
            var taskResult  = success.tasks[0];
            if (taskResult is TaskErrorResult errorResult) {
                context.WriteString(errorResult.message, "application/json", 400);
                return true;
            }
            var readResult  = (ReadEntitiesResult)success.tasks[0];
            var resultSet   = readResult.reads[0];
            var readError   = resultSet.Error;
            if (readError != null) {
                context.WriteString(readError.message, "application/json", 400);
                return true;
            }
            var content     = success.resultMap[container].entityMap[entityId];
            var entityError = content.Error;
            if (entityError != null) {
                context.WriteString(entityError.message, "application/json", 200);
                return true;
            }
            context.Write(content.Json, 0, "application/json", 200);
            return true;
        }
    }
}
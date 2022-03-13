// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public sealed class AuthorizeTaskType : AuthorizerDatabase {
        private  readonly   TaskType    type;
        
        public   override   string      ToString() => $"database: {dbLabel}, type: {type.ToString()}";

        public AuthorizeTaskType(TaskType type, string database) : base (database) {
            this.type       = type;    
        }
        
        public override bool Authorize(SyncRequestTask task, ExecuteContext executeContext) {
            if (!AuthorizeDatabase(executeContext))
                return false;
            return task.TaskType == type;
        }
    }
}
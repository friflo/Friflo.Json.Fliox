// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    public sealed class AuthorizeTaskType : IAuthorizer {
        private  readonly   AuthorizeDatabase   authorizeDatabase;
        private  readonly   TaskType            type;
        
        public   override   string      ToString() => $"database: {authorizeDatabase.dbLabel}, type: {type.ToString()}";

        public AuthorizeTaskType(TaskType type, string database) {
            authorizeDatabase   = new AuthorizeDatabase(database);
            this.type           = type;    
        }
        
        public bool Authorize(SyncRequestTask task, ExecuteContext executeContext) {
            if (!authorizeDatabase.Authorize(executeContext))
                return false;
            return task.TaskType == type;
        }
    }
}
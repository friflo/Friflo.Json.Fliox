// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database
{
    public class TaskHandler
    {
        private static bool AuthorizeTask(DatabaseTask task, MessageContext messageContext, out TaskResult error) {
            if (messageContext.Authorize(task, messageContext)) {
                error = null;
                return true;
            }
            var message = "not authorized";
            var authError = messageContext.authState.Error; 
            if (authError != null) {
                message = $"{message} ({authError})";
            }
            error = DatabaseTask.PermissionDenied(message);
            return false;
        }
        
        public virtual Task<TaskResult> ExecuteTask (DatabaseTask task, EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            if (!AuthorizeTask(task, messageContext, out var error)) {
                return Task.FromResult(error);
            }
            Task<TaskResult> result = task.Execute(database, response, messageContext);
            return result;
        }
    }
}
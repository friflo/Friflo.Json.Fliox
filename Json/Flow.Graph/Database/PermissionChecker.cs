// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database
{
    public abstract class PermissionChecker
    {
        public abstract Task<PermissionResult> GrantPermission (EntityDatabase database, MessageContext messageContext);
        
        public static TaskErrorResult PermissionDenied(in PermissionResult result, int taskIndex) {
            var message = result.message ?? "permission denied";
            var taskResult = new TaskErrorResult{
                type        = TaskErrorResultType.PermissionDenied,
                message     = $"{message}. tasks[{taskIndex}]"
            };
            return taskResult;
        }
    }
    
    public struct PermissionResult
    {
        /// need to be set to true if permission is granted
        public bool     granted;
        /// optional message if permission is not <see cref="granted"/>
        public string   message;
    }
}
// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;

namespace Friflo.Json.Flow.Sync
{
    // ----------------------------------- task -----------------------------------
    public class Message : DatabaseTask
    {
        public              string          message;
            
        internal override   TaskType        TaskType    => TaskType.message;
        public   override   string          ToString()  => message;

        internal override Task<TaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            TaskResult result = new MessageResult{message = message};
            return Task.FromResult(result); 
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    public class MessageResult : TaskResult, ICommandResult
    {
        public              string          message;
        
        public CommandError                 Error { get; set; }

        internal override   TaskType        TaskType => TaskType.message;
    }
}
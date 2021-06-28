// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Mapper.Map.Val;

namespace Friflo.Json.Flow.Sync
{
    // ----------------------------------- task -----------------------------------
    public class Message : DatabaseTask
    {
        public              string          tag;
        public              JsonValue       value;
            
        internal override   TaskType        TaskType    => TaskType.message;
        public   override   string          ToString()  => tag;

        internal override Task<TaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            TaskResult result = new MessageResult{tag = tag};
            return Task.FromResult(result); 
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    public class MessageResult : TaskResult, ICommandResult
    {
        public              string          tag;
        
        public CommandError                 Error { get; set; }

        internal override   TaskType        TaskType => TaskType.message;
    }
}
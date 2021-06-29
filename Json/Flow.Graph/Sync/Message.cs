// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Mapper.Map.Val;

namespace Friflo.Json.Flow.Sync
{
    // ----------------------------------- task -----------------------------------
    public class SendMessage : DatabaseTask
    {
        public              string          name;
        public              JsonValue       value;
            
        internal override   TaskType        TaskType    => TaskType.message;
        public   override   string          ToString()  => name;

        internal override Task<TaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            TaskResult result = new SendMessageResult{ name = name };
            return Task.FromResult(result); 
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    public class SendMessageResult : TaskResult, ICommandResult
    {
        // todo should be removed
        public              string          name;
        
        public CommandError                 Error { get; set; }

        internal override   TaskType        TaskType => TaskType.message;
    }
}
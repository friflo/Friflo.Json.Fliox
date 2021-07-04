// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Graph;
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
            if (name == null)
                return Task.FromResult<TaskResult>(MissingField(nameof(name)));
            var messageResult = new SendMessageResult();
            if (name == StdMessage.Echo) {
                messageResult.result = value;
            }
            TaskResult result = messageResult;
            return Task.FromResult(result);
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    public class SendMessageResult : TaskResult, ICommandResult
    {
        /// <summary>
        /// By default it echos <see cref="SendMessage.value"/>.
        /// If using a custom <see cref="TaskHandler"/> it can be used to return the request specific result.
        /// </summary>
        public              JsonValue       result;
        
        public CommandError                 Error { get; set; }

        internal override   TaskType        TaskType => TaskType.message;
    }
}
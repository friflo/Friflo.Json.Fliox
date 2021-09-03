// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using Friflo.Json.Fliox.Db.Database;
using Friflo.Json.Fliox.Db.Graph;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Db.Sync
{
    // ----------------------------------- task -----------------------------------
    public class SendMessage : DatabaseTask
    {
        [Fri.Required]  public  string          name;
        [Fri.Required]  public  JsonValue       value;
            
        internal override       TaskType        TaskType => TaskType.message;
        public   override       string          TaskName => $"name: '{name}'";

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
// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Graph;
using Friflo.Json.Fliox.DB.NoSQL;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Sync
{
    // ----------------------------------- task -----------------------------------
    public class SendMessage : SyncRequestTask
    {
        [Fri.Required]  public  string          name;
        [Fri.Required]  public  JsonValue       value;
            
        internal override       TaskType        TaskType => TaskType.message;
        public   override       string          TaskName => $"name: '{name}'";

        internal override Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            if (name == null)
                return Task.FromResult<SyncTaskResult>(MissingField(nameof(name)));
            var messageResult = new SendMessageResult();
            if (name == StdMessage.Echo) {
                messageResult.result = value;
            }
            SyncTaskResult result = messageResult;
            return Task.FromResult(result);
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    public class SendMessageResult : SyncTaskResult, ICommandResult
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
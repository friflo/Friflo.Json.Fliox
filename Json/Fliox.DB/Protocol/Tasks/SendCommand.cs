// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Client;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Host.Internal;
using Friflo.Json.Fliox.DB.Protocol.Models;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    public sealed class SendCommand : SyncRequestTask
    {
        [Fri.Required]  public  string          name;
                        public  JsonValue       value;
            
        internal override       TaskType        TaskType => TaskType.command;
        public   override       string          TaskName => $"name: '{name}'";

        internal override Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            if (name == null)
                return Task.FromResult<SyncTaskResult>(MissingField(nameof(name)));
            SyncTaskResult result = new SendCommandResult();
            return Task.FromResult(result);
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    public sealed class SendCommandResult : SyncTaskResult, ICommandResult
    {
        /// <summary>
        /// By default it echos <see cref="SendCommand.value"/>.
        /// If using a custom <see cref="TaskHandler"/> it can be used to return the request specific result.
        /// </summary>
        public              JsonValue       result;
        
        public CommandError                 Error { get; set; }

        internal override   TaskType        TaskType => TaskType.command;
    }
}
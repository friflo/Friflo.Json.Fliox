// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Models;

namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    /// <summary>
    /// Used as base type for <see cref="SendMessage"/> or <see cref="SendCommand"/> to specify the command / message
    /// <see cref="name"/> and <see cref="param"/>
    /// </summary>
    public abstract class SyncMessageTask : SyncRequestTask
    {
        /// <summary>command / message name</summary>
        [Required]  public  string          name;
        /// <summary>command / message parameter. Can be null or absent</summary>
                    public  JsonValue       param;
                    public  List<JsonKey>   targetUsers;
                    public  List<JsonKey>   targetClients;
       // todo add  public  List<JsonKey>   targetGroups;
                        
        public   override   string          TaskName => $"name: '{name}'";
    }
    
    /// <summary>
    /// Send a database message with the given <see cref="SyncMessageTask.param"/><br/> 
    /// </summary>
    public sealed class SendMessage : SyncMessageTask
    {
        internal override       TaskType        TaskType => TaskType.message;

        internal override async Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            if (name == null)
                return MissingField(nameof(name));
            if (database.handler.TryGetMessage(name, out var callback)) {
                var result  = await callback.InvokeDelegate(name, param, syncContext).ConfigureAwait(false); // todo could be synchronous call
                if (result.error != null) {
                    return new TaskErrorResult (TaskErrorResultType.CommandError, result.error);
                }
            }
            return new SendMessageResult();
        }
    }

    // ----------------------------------- task result -----------------------------------
    public abstract class SyncMessageResult : SyncTaskResult, ICommandResult
    {
        [Ignore]    public  CommandError    Error { get; set; }
    }
    
    /// <summary>
    /// Result of a <see cref="SendMessage"/> task
    /// </summary>
    public sealed class SendMessageResult : SyncMessageResult
    {
        internal override   TaskType        TaskType => TaskType.message;
    }
}
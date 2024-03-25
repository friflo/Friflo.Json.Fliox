// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
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
    /// <see cref="name"/> and <see cref="param"/>. <br/>
    /// In case <see cref="users"/> or <see cref="clients"/> is set the Hub forward the message as an event only to the
    /// given <see cref="users"/> or <see cref="clients"/>. 
    /// </summary>
    public abstract class SyncMessageTask : SyncRequestTask
    {
        /// <summary>command / message name</summary>
        [Required]  public  ShortString         name;
        /// <summary>command / message parameter. Can be null or absent</summary>
                    public  JsonValue           param;
        /// <summary>if set the Hub forward the message as an event only to given <see cref="users"/></summary>
                    public  List<ShortString>   users;
        /// <summary>if set the Hub forward the message as an event only to given <see cref="clients"/></summary>
                    public  List<ShortString>   clients;
        /// <summary>if set the Hub forward the message as an event only to given <see cref="groups"/></summary>
                    public  List<ShortString>   groups;
        
        [Ignore]   internal MessageDelegate     callback;
        
        /// <summary>
        /// return true to execute this task synchronous. <br/>
        /// return false to execute task asynchronous
        /// </summary>
        public override bool PreExecute(in PreExecute execute) {
            if (name.IsNull()) {
                intern.executionType = ExecutionType.Sync; // execute error synchronously. error: missing field: {name}
                return true; 
            }
            if (execute.db.service.TryGetMessage(name, out callback)) {
                var isSync = callback.IsSynchronous(execute);
                intern.executionType = isSync ? ExecutionType.Sync : ExecutionType.Async;
                return isSync;
            }
            intern.executionType = ExecutionType.Sync; // execute error synchronously. error: no command handler for: '{name}'
            return true;
        }

        public   override   string          TaskName => $"name: '{name}'";
    }
    
    /// <summary>
    /// Send a database message with the given <see cref="SyncMessageTask.param"/>. <br/>
    /// In case <see cref="SyncMessageTask.users"/> or <see cref="SyncMessageTask.clients"/> is set the Hub forward
    /// the message as an event only to the given <see cref="SyncMessageTask.users"/> or <see cref="SyncMessageTask.clients"/>. 
    /// </summary>
    public sealed class SendMessage : SyncMessageTask
    {
        public   override       TaskType        TaskType => TaskType.message;

        public override async Task<SyncTaskResult> ExecuteAsync(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            if (name.IsNull())
                return MissingField(nameof(name));
            if (callback != null) {
                await callback.InvokeDelegateAsync(this, syncContext).ConfigureAwait(false);
            }
            return SendMessageResult.Create(syncContext);
        }
        
        public override SyncTaskResult Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            if (name.IsNull())
                return MissingField(nameof(name));
            if (callback != null) {
                callback.InvokeDelegate(this, syncContext);
            }
            return SendMessageResult.Create(syncContext);
        }
    }

    // ----------------------------------- task result -----------------------------------
    public abstract class SyncMessageResult : SyncTaskResult, ITaskResultError
    {
        [Ignore]    public  TaskExecuteError    Error { get; set; }
    }
    
    /// <summary>
    /// Result of a <see cref="SendMessage"/> task
    /// </summary>
    public sealed class SendMessageResult : SyncMessageResult
    {
        internal override   TaskType        TaskType    => TaskType.message;
        internal override   bool            Failed      => false;
        
        public static SyncMessageResult Create(SyncContext syncContext) {
            return syncContext.syncPools?.messageResultPool.Create() ?? new SendMessageResult();
        }
    }
}
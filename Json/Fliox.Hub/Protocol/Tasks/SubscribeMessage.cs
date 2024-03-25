// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;

namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    /// <summary>
    /// Subscribe to commands and messages sent to a database by their <see cref="name"/><br/>
    /// Unsubscribe by setting <see cref="remove"/> to true 
    /// </summary>
    public sealed class SubscribeMessage : SyncRequestTask
    {
        /// <summary>subscribe a specific message: 'std.Echo', multiple messages by prefix: 'std.*', all messages: '*'</summary>
        [Required]  public  string      name;
        /// <summary>if true a previous added subscription is removed. Otherwise added</summary>
                    public  bool?       remove;
        
        public   override   TaskType    TaskType    => TaskType.subscribeMessage;
        public   override   string      TaskName    => $"name: '{name}'";

        public override Task<SyncTaskResult> ExecuteAsync(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            return Task.FromResult(Execute(database, response, syncContext));
        }
        
        public override SyncTaskResult Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            var hub             = syncContext.Hub;
            var eventDispatcher = hub.EventDispatcher;
            if (eventDispatcher == null)
                return InvalidTask("Hub has no EventDispatcher");
            if (name == null)
                return MissingField(nameof(name));
            
            if (!hub.Authenticator.EnsureValidClientId(hub.ClientController, syncContext, out string error))
                return InvalidTask(error);
            
            var eventReceiver   = syncContext.eventReceiver;
            var user            = syncContext.User;
            if (!eventDispatcher.SubscribeMessage(database.nameShort, this, user, syncContext.clientId, eventReceiver, out error))
                return InvalidTask(error);
            
            return new SubscribeMessageResult();
        }
        
        internal static ShortString GetPrefix (string name) {
            var prefix = name.EndsWith("*") ? name.Substring(0, name.Length - 1) : null; 
            return new ShortString(prefix);
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    /// <summary>
    /// Result of a <see cref="SubscribeMessage"/> task
    /// </summary>
    public sealed class SubscribeMessageResult : SyncTaskResult
    {
        internal override   TaskType    TaskType    => TaskType.subscribeMessage;
        internal override   bool        Failed      => false;
    }
}
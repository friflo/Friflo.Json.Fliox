// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// A <see cref="MessageTask"/> contains the message / command (<b>name</b> and <b>param</b>) sent to
    /// an <see cref="EntityDatabase"/> by <see cref="FlioxClient.SendMessage"/>
    /// </summary>
    /// <remarks>
    /// The <see cref="EntityDatabase"/> forward the message (or command) as en event to all clients subscribed to the message. <br/>
    /// If sending the message to the <see cref="EntityDatabase"/> is successful <see cref="SyncTask.Success"/> is true. <br/>
    /// <i>Notes:</i>
    /// <list type="bullet">
    ///   <item> Messages in contrast to commands return no result. </item>
    ///   <item> The result of a command is available via <see cref="CommandTask{TResult}.Result"/> </item>
    ///   <item> The response of messages and commands provide no information that they are received as events by subscribed clients. </item>
    /// </list>
    /// </remarks>
    public class MessageTask : SyncTask
    {
        /// <summary>
        /// Restrict the clients receiving the message as an event in case they setup a subscription with <see cref="FlioxClient.SubscribeMessage"/>.
        /// </summary>
        /// <remarks>
        /// A default <see cref="EventTargets"/> instance is not restricted to specific target users, clients or groups. <br/>
        /// So a message is forwarded by the Hub as an event to all clients subscribed to the message. <br/>
        /// </remarks>
        public              EventTargets    EventTargets { get; set; }
        internal            EventTargets    GetOrCreateTargets() => EventTargets ?? (EventTargets = new EventTargets());
        
        internal  readonly  ShortString     name;
        protected readonly  JsonValue       param;
        
        [DebuggerBrowsable(Never)]
        internal            TaskState       state;
        internal  override  TaskState       State       => state;
        
        public    override  string          Details     => $"MessageTask (name: {name.AsString()})";
        internal  override  TaskType        TaskType    => TaskType.message;

        
        internal MessageTask(in ShortString name, in JsonValue param) {
            this.name       = name;
            this.param      = param;
        }
        
        internal override SyncRequestTask CreateRequestTask(in CreateTaskContext context) {
            var targets = EventTargets;
            return new SendMessage {
                name        = name,
                param       = param,
                intern      = new SyncTaskIntern(this),
                users       = targets?.users,
                clients     = targets?.clients,
                groups      = targets?.groups
            };
        }
    }
}

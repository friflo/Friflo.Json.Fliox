// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.Protocol
{
    // ----------------------------------- request -----------------------------------
    /// <summary>
    /// A <see cref="SyncRequest"/> is sent to a <see cref="Host.FlioxHub"/> targeting a specific <see cref="database"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="SyncRequest"/> contains a list of <see cref="tasks"/> used to execute container operations or database commands.
    /// The <see cref="Host.FlioxHub"/> returns a <see cref="SyncResponse"/> containing the results for each task.
    /// </remarks>
    public sealed class SyncRequest : ProtocolRequest
    {
        /// <summary>
        /// Identify the user performing a sync request.
        /// In case using of using <see cref="DB.UserAuth.UserAuthenticator"/> the <see cref="userId"/> and <see cref="token"/>
        /// are use for user authentication.
        /// </summary>
        [Serialize                                ("user")]
                    public  ShortString             userId;
                    public  ShortString             token;
        /// <summary>
        /// <see cref="eventAck"/> is used to ensure (change) events are delivered reliable.
        /// A client set <see cref="eventAck"/> to the last received <see cref="EventMessage.seq"/> in case
        /// it has subscribed to database changes by a <see cref="SubscribeChanges"/> task.
        /// Otherwise <see cref="eventAck"/> is null.
        /// </summary>
        [Serialize                                ("ack")]
                    public  int?                    eventAck;
        /// <summary>list of tasks either container operations or database commands / messages</summary>
        [Required]  public  ListOne<SyncRequestTask> tasks;
        /// <summary>database name the <see cref="tasks"/> apply to. null to access the default database</summary>
        [Serialize                                ("db")]
                    public  ShortString             database;
        /// <summary>optional JSON value - can be used to describe a request</summary>
                    public  JsonValue               info;
        
        [Ignore]   internal SyncRequestIntern       intern;
        
        internal override   MessageType             MessageType => MessageType.sync;

        public   override   string                  ToString()  => GetString();
        
        private string GetString() {
            var sb = new StringBuilder();
            if (database.IsNull()) {
                sb.Append("(default db)");    
            } else {
                database.AppendTo(sb);
            }
            sb.Append(": ");
            if (tasks.Count == 0) {
                sb.Append("no tasks");
            } else {
                foreach (var task in tasks.GetReadOnlySpan()) {
                    sb.Append(task.TaskType);
                    sb.Append(" - ");
                    sb.Append(task.TaskName);
                    sb.Append(", ");
                }
                sb.Length -= 2;
            }
            return sb.ToString(); 
        }
    }
    
    /// <summary>
    /// Contain fields assigned in <see cref="FlioxHub.InitSyncRequest"/> by using the public fields of the <see cref="SyncRequest"/>
    /// </summary>
    internal struct SyncRequestIntern {
        internal    ExecutionType   executionType;
        internal    string          error;
        internal    EntityDatabase  db;
        internal    PreAuthType     preAuthType;
        internal    User            preAuthUser;
        internal    bool            executeSync;
    }
}

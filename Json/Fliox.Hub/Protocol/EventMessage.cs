// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Fliox.Hub.Protocol
{
    // ----------------------------------- event -----------------------------------
    /// <summary>
    /// Contains a set of <see cref="SyncEvent"/>'s. It is send as a push message to clients to deliver the events
    /// subscribed by these clients.
    /// </summary>
    public sealed class EventMessage : ProtocolEvent
    {
        /// <summary>
        /// Each <see cref="SyncEvent"/> corresponds to a <see cref="SyncRequest"/> and contains the subscribed
        /// messages and container changes in its <see cref="SyncEvent.tasks"/> field
        /// </summary>
        public                  List<SyncEvent>     events;
        
        internal override       MessageType         MessageType => MessageType.ev;
        
        internal static EventMessage Clone(EventMessage ev) {
            return new EventMessage {
                dstClientId = ev.dstClientId,
                events      = new List<SyncEvent>(ev.events),
            };
        } 
    }

    /// <summary>
    /// A <see cref="SyncEvent"/> corresponds to a <see cref="SyncRequest"/> and contains the subscribed
    /// messages and container changes in its <see cref="SyncEvent.tasks"/> field
    /// </summary>
    public struct SyncEvent
    {
        /// <summary>
        /// Increasing event sequence number starting with 1 for a specific target client <see cref="ProtocolEvent.dstClientId"/>.
        /// Each target client (subscriber) has its own sequence.
        /// </summary>
                    public      int                     seq;
        /// <summary>
        /// The user which caused the event. Specifically the user which made a database change or sent a message / command.
        /// The user client is not preserved by en extra property as a use case for this is not obvious.
        /// </summary>
        [Serialize                                    ("src")]
        [Required]  public      JsonKey                 srcUserId;
        
        /// <summary>
        /// Is true if the receiving client is the origin of the event
        /// </summary>
                    public      bool?                   isOrigin;
        
        /// <summary>The database the <see cref="tasks"/> refer to</summary>
        [Required]  public      string                  db;

        /// <summary>
        /// Contains the events an application subscribed. These are:<br/>
        /// <see cref="CreateEntities"/>, 
        /// <see cref="UpsertEntities"/>, 
        /// <see cref="DeleteEntities"/>,
        /// <see cref="SendMessage"/>, 
        /// <see cref="SendCommand"/>
        /// </summary>
                    public      List<SyncRequestTask>   tasks;

        /// Used for optimization. Either <see cref="tasks"/> or <see cref="tasksJson"/> is set
        [Ignore]    internal    JsonValue[]             tasksJson;
        
        public   override       string                  ToString()  => GetEventInfo().ToString();
        
        public EventInfo    GetEventInfo() {
            var info = new EventInfo();
            foreach (var task in tasks) {
                switch (task.TaskType) {
                    case TaskType.create:
                        var create = (CreateEntities)task;
                        info.changes.creates += create.entities.Count;
                        break;
                    case TaskType.upsert:
                        var upsert = (UpsertEntities)task;
                        info.changes.upserts += upsert.entities.Count;
                        break;
                    case TaskType.delete:
                        var delete = (DeleteEntities)task;
                        info.changes.deletes += delete.ids.Count;
                        break;
                    case TaskType.merge:
                        var merge = (MergeEntities)task;
                        info.changes.merges  += merge.patches.Count;
                        break;
                    case TaskType.message:
                    case TaskType.command:
                        info.messages++;
                        break;
                }
            }
            return info;
        }
    }
    

    /// <summary>
    /// <see cref="EventInfo"/> is never de-/serialized.
    /// It purpose is to get all aggregated information about a <see cref="SyncEvent"/> by  by <see cref="SyncEvent.GetEventInfo"/>.
    /// </summary>
    public struct EventInfo {
        public              ChangeInfo  changes;
        public              int         messages;
        
        public int Count => changes.Count + messages;
        
        public override string ToString() => $"creates: {changes.creates}, upserts: {changes.upserts}, deletes: {changes.deletes}, merges: {changes.merges}, messages: {messages}";
        
        public void Clear() {
            changes.Clear();
            messages = 0;
        }
    }

    
    /// <summary>
    /// <see cref="ChangeInfo"/> is never de-/serialized.
    /// It purpose is to get aggregated change information about a <see cref="SyncEvent"/> by <see cref="SyncEvent.GetEventInfo"/>.
    /// </summary>
    public struct ChangeInfo {
        public  int creates;
        public  int upserts;
        public  int deletes;
        public  int merges;
        
        public int Count => creates + upserts + deletes + merges;
        
        public override string ToString() => FormatToString();
        
        private string FormatToString() {
            var sb = new StringBuilder();
            AppendTo(sb);
            return sb.ToString();
        }
        
        internal void AppendTo(StringBuilder sb) {
            sb.Append("creates: ");   sb.Append(creates);
            sb.Append(", upserts: "); sb.Append(upserts);
            sb.Append(", deletes: "); sb.Append(deletes);
            sb.Append(", merges: ");  sb.Append(merges);
        }
        
        public void Clear() {
            creates = 0;
            upserts = 0;
            deletes = 0;
            merges  = 0;
        }
        
        public void Add(ChangeInfo changeInfo) {
            creates += changeInfo.creates;
            upserts += changeInfo.upserts;
            deletes += changeInfo.deletes;
            merges  += changeInfo.merges;
        }
    }
}
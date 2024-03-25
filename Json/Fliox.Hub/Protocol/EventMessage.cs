// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
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
        /// Increasing event sequence number starting with 1 for a specific target client <see cref="ProtocolEvent.dstClientId"/>.
        /// Each target client (subscriber) has its own sequence.
        /// </summary>
        public                  int                 seq;
        /// <summary>
        /// Each <see cref="SyncEvent"/> corresponds to a <see cref="SyncRequest"/> and contains the subscribed
        /// messages and container changes in its <see cref="SyncEvent.tasks"/> field
        /// </summary>
        [Serialize                                ("ev")]
        public                  List<SyncEvent>     events;
        
        internal override       MessageType         MessageType => MessageType.ev;
    }
    
    /// <summary>mimic an <see cref="EventMessage"/></summary>
    public struct RawEventMessage
    {
        public  JsonValue           msg; // "ev";
        public  int                 seq;
        public  List<RawSyncEvent>  ev;
    }
    
    /// <summary>mimic a <see cref="SyncEvent"/></summary>
    public struct RawSyncEvent
    {
        public  List<RawSyncTask>   tasks;
    }
    
    /// <summary>mimic a <see cref="SyncRequestTask"/></summary>
    public struct RawSyncTask
    {
        public  JsonValue           task; // create, upsert, merge or delete
        public  JsonValue           cont; // container name
        public  JsonValue           set;  // serialized entities
    }

    /// <summary>
    /// A <see cref="SyncEvent"/> corresponds to a <see cref="SyncRequest"/> and contains the subscribed
    /// messages and container changes in its <see cref="SyncEvent.tasks"/> field
    /// </summary>
    public struct SyncEvent
    {
        /// <summary>
        /// The user which caused the event. Specifically the user which made a database change or sent a message / command.<br/>
        /// By default it is set always. If not required set <see cref="EventDispatcher.SendEventUserId"/> to false.
        /// </summary>
        [Serialize                                    ("usr")]
                    public      ShortString             usr;
        
        /// <summary>
        /// The client which caused the event. Specifically the client which made a database change or sent a message / command.<br/>
        /// By default it set only if the subscriber is the origin of the event to enable ignoring the event.<br/>
        /// It is set in any case if <see cref="EventDispatcher.SendEventClientId"/> is true.
        /// </summary>
        [Serialize                                    ("clt")]
                    public      ShortString             clt;
        
        /// <summary>
        /// The database the <see cref="tasks"/> refer to<br/>
        /// <see cref="db"/> is null if the event refers to the default <see cref="FlioxHub.database"/>
        /// </summary>
                    public      ShortString             db;

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
        [Ignore]    internal    List<JsonValue>         tasksJson;
        
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
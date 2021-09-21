// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Friflo.Json.Fliox.DB.Protocol
{
    // ----------------------------------- event -----------------------------------
    public class SubscriptionEvent : ProtocolEvent
    {
        /// <summary>
        /// Contains the events an application subscribed. These are:
        /// <list type="bullet">
        ///   <item><see cref="CreateEntities"/></item>
        ///   <item><see cref="UpsertEntities"/></item>
        ///   <item><see cref="DeleteEntities"/></item>
        ///   <item><see cref="PatchEntities"/></item>
        ///   <item><see cref="SendMessage"/></item>
        /// </list>
        /// </summary>
        public              List<SyncRequestTask>  tasks;
        
        internal override   MessageType         MessageType => MessageType.subscription;
        public   override   string              ToString()  => GetEventInfo().ToString();
        
        public EventInfo    GetEventInfo() {
            var info = new EventInfo(new ChangeInfo());
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
                    case TaskType.patch:
                        var patch = (PatchEntities)task;
                        info.changes.patches += patch.patches.Count;
                        break;
                    case TaskType.message:
                        info.messages++;
                        break;
                }
            }
            return info;
        }

        public ChangeInfo GetChangeInfo() {
            var eventInfo = GetEventInfo();
            return eventInfo.changes;
        }
    }
    

    /// <summary>
    /// <see cref="EventInfo"/> is never de-/serialized.
    /// It purpose is to get all aggregated information about a <see cref="SubscriptionEvent"/> by  by <see cref="SubscriptionEvent.GetEventInfo"/>.
    /// </summary>
    public struct EventInfo {
        public  readonly    ChangeInfo  changes;
        public              int         messages;
        
        public EventInfo(ChangeInfo changes) {
            this.changes    = changes ?? new ChangeInfo();
            messages        = 0;
        }
        
        public int Count => changes.Count + messages;
        
        public override string ToString() => $"(creates: {changes.creates}, upserts: {changes.upserts}, deletes: {changes.deletes}, patches: {changes.patches}, messages: {messages})";
        
        public void Clear() {
            changes.Clear();
            messages = 0;
        }
    }

    
    /// <summary>
    /// <see cref="ChangeInfo"/> is never de-/serialized.
    /// It purpose is to get aggregated change information about a <see cref="SubscriptionEvent"/> by <see cref="SubscriptionEvent.GetChangeInfo"/>.
    /// </summary>
    public class ChangeInfo {
        public  int creates;
        public  int upserts;
        public  int deletes;
        public  int patches;
        
        public int Count => creates + upserts + deletes + patches;
        
        public override string ToString() => $"(creates: {creates}, upserts: {upserts}, deletes: {deletes}, patches: {patches})";
        
        public void Clear() {
            creates = 0;
            upserts = 0;
            deletes = 0;
            patches = 0;
        }
        
        public void Add(ChangeInfo changeInfo) {
            creates += changeInfo.creates;
            upserts += changeInfo.upserts;
            deletes += changeInfo.deletes;
            patches += changeInfo.patches;
        }
    }
}
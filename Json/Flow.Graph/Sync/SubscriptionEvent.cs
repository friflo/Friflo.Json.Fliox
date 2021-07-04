// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Friflo.Json.Flow.Sync
{
    // ----------------------------------- event -----------------------------------
    public class SubscriptionEvent : DatabaseEvent
    {
        /// <summary>
        /// Contains the events an application subscribed. These are:
        /// <list type="bullet">
        ///   <item><see cref="CreateEntities"/></item>
        ///   <item><see cref="UpdateEntities"/></item>
        ///   <item><see cref="DeleteEntities"/></item>
        ///   <item><see cref="PatchEntities"/></item>
        ///   <item><see cref="SendMessage"/></item>
        /// </list>
        /// </summary>
        public              List<DatabaseTask>  tasks;
        
        internal override   DatabaseEventType   EventType   => DatabaseEventType.subscription;
        public   override   string              ToString()  => GetChangeInfo().ToString();
        
        public EventInfo    GetEventInfo() {
            var info = new EventInfo(new ChangeInfo());
            foreach (var task in tasks) {
                switch (task.TaskType) {
                    case TaskType.create:
                        var create = (CreateEntities)task;
                        info.changes.creates += create.entities.Count;
                        break;
                    case TaskType.update:
                        var update = (UpdateEntities)task;
                        info.changes.updates += update.entities.Count;
                        break;
                    case TaskType.delete:
                        var delete = (DeleteEntities)task;
                        info.changes.deletes += delete.ids.Count;
                        break;
                    case TaskType.patch:
                        var patch = (PatchEntities)task;
                        info.changes.patches += patch.patches.Count;
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
    

    public struct EventInfo {
        public  ChangeInfo  changes;
        public  int         messages;
        
        public EventInfo(ChangeInfo changes) {
            this.changes    = changes ?? new ChangeInfo();
            messages        = 0;
        }
        
        public int Count => changes.Count;
        
        public override string ToString() => $"(creates: {changes.creates}, updates: {changes.updates}, deletes: {changes.deletes}, patches: {changes.patches}, messages: {messages})";
        
        public void Clear() {
            changes.Clear();
        }
    }

    
    /// <summary>
    /// <see cref="ChangeInfo"/> is never de-/serialized.
    /// It purpose is to get aggregated information about a <see cref="SubscriptionEvent"/> by <see cref="SubscriptionEvent.GetChangeInfo"/>
    /// </summary>
    public class ChangeInfo {
        public  int creates;
        public  int updates;
        public  int deletes;
        public  int patches;
        
        public int Count => creates + updates + deletes + patches;
        
        public override string ToString() => $"(creates: {creates}, updates: {updates}, deletes: {deletes}, patches: {patches})";
        
        public void Clear() {
            creates = 0;
            updates = 0;
            deletes = 0;
            patches = 0;
        }
        
        public void Add(ChangeInfo changeInfo) {
            creates += changeInfo.creates;
            updates += changeInfo.updates;
            deletes += changeInfo.deletes;
            patches += changeInfo.patches;
        }
    }
}
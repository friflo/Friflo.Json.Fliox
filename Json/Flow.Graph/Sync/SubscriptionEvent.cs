// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Friflo.Json.Flow.Sync
{
    // ----------------------------------- event -----------------------------------
    public class SubscriptionEvent : DatabaseEvent
    {
        public              List<DatabaseTask>  tasks;
        
        internal override   DatabaseEventType   EventType   => DatabaseEventType.change;
        public   override   string              ToString()  => GetChangeInfo().ToString();

        public ChangeInfo GetChangeInfo() {
            var changeInfo = new ChangeInfo();
            foreach (var task in tasks) {
                switch (task.TaskType) {
                    case TaskType.create:
                        var create = (CreateEntities)task;
                        changeInfo.creates += create.entities.Count;
                        break;
                    case TaskType.update:
                        var update = (UpdateEntities)task;
                        changeInfo.updates += update.entities.Count;
                        break;
                    case TaskType.delete:
                        var delete = (DeleteEntities)task;
                        changeInfo.deletes += delete.ids.Count;
                        break;
                    case TaskType.patch:
                        var patch = (PatchEntities)task;
                        changeInfo.patches += patch.patches.Count;
                        break;
                }
            }
            return changeInfo;
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
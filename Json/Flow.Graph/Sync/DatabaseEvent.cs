// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Flow.Sync
{
    [Fri.Discriminator("type")]
    [Fri.Polymorph(typeof(ChangesEvent), Discriminant = "changes")]
    public abstract class DatabaseEvent {
        public              string              clientId {get; set;}
        internal abstract   DatabaseEventType   EventType { get; }
    }

    public class ChangesEvent : DatabaseEvent
    {
        public              List<DatabaseTask>  tasks;
        
        internal override   DatabaseEventType   EventType   => DatabaseEventType.changes;
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
    
    
    // ReSharper disable InconsistentNaming
    public enum DatabaseEventType {
        changes
    }
    
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
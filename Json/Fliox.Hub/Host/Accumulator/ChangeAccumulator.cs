// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Utils;

// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.Hub.Host.Accumulator
{
    /// <summary>
    ///  Accumulate the entity change events for a specific <see cref="EntityDatabase"/> 
    /// </summary>
    internal sealed class ChangeAccumulator
    {
        private  readonly   Dictionary<SmallString, ContainerChanges>   containers;
        private  readonly   MessageBufferQueue<ValueChange>             changes;
        private  readonly   List<MessageItem<ValueChange>>              events;
        internal readonly   MemoryBuffer                                valueBuffer;
        internal readonly   List<JsonValue>                             changeTasks;
        internal readonly   ChangeEventTask                             taskBuffer;
        

        internal ChangeAccumulator() {
            containers  = new Dictionary<SmallString, ContainerChanges>();
            changes     = new MessageBufferQueue<ValueChange>();
            events      = new List<MessageItem<ValueChange>>();
            valueBuffer = new MemoryBuffer(1024);
            changeTasks = new List<JsonValue>();
            taskBuffer  = new ChangeEventTask();
        }

        internal void AddTask(SyncRequestTask task)
        {
            switch (task.TaskType) {
                case TaskType.create:
                    lock (changes) {
                        var create = (CreateEntities)task;
                        if (!containers.TryGetValue(create.containerSmall, out var container)) {
                            container = AddContainer(create.containerSmall);
                        }
                        foreach (var entity in create.entities) {
                            changes.AddTail(entity.value, new ValueChange(container, TaskType.create));
                        }
                        break;
                    }
                case TaskType.upsert:
                    lock (changes) {
                        var create = (UpsertEntities)task;
                        if (!containers.TryGetValue(create.containerSmall, out var container)) {
                            container = AddContainer(create.containerSmall);
                        }
                        foreach (var entity in create.entities) {
                            changes.AddTail(entity.value, new ValueChange(container, TaskType.upsert));
                        }
                        break;
                    }
                case TaskType.merge:
                    lock (changes) {
                        var merge = (MergeEntities)task;
                        if (!containers.TryGetValue(merge.containerSmall, out var container)) {
                            container = AddContainer(merge.containerSmall);
                        }
                        foreach (var entity in merge.patches) {
                            changes.AddTail(entity.value, new ValueChange(container, TaskType.merge));
                        }
                        break;
                    }
            }
        }
        
        private ContainerChanges AddContainer(in SmallString name) {
            var container = new ContainerChanges(name);
            containers.Add(name, container);
            return container;
        }

        internal void AccumulateEvents(EventSubClient[] subClients, ObjectWriter writer)
        {
            lock (changes) {
                changes.DequeMessages(events);
            }
            valueBuffer.Reset();
            changeTasks.Clear();
            var context = new AccumulatorContext(this, writer);
            foreach (var ev in events) {
                ev.meta.container.AddEvent(ev, context);
            }
            foreach (var subClient in subClients) {
                foreach (var task in changeTasks) {
                    
                }
            }
        }
    }
}
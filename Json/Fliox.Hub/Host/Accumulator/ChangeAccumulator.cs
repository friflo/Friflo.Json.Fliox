// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Utils;

// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
// ReSharper disable SwapViaDeconstruction
namespace Friflo.Json.Fliox.Hub.Host.Accumulator
{
    /// <summary>
    ///  Accumulate the entity change events for a specific <see cref="EntityDatabase"/> 
    /// </summary>
    internal sealed class ChangeAccumulator
    {
        private  readonly   Dictionary<SmallString, ContainerChanges>   containers;
        private             TaskBuffer                                  writeBuffer;
        private             TaskBuffer                                  readBuffer;
        internal readonly   MemoryBuffer                                rawTaskBuffer;
        internal readonly   List<JsonValue>                             rawTasks;
        internal readonly   WriteTaskModel                              writeTaskModel;
        internal readonly   DeleteTaskModel                             deleteTaskModel;

        internal ChangeAccumulator() {
            containers      = new Dictionary<SmallString, ContainerChanges>();
            writeBuffer     = new TaskBuffer();
            readBuffer      = new TaskBuffer();
            rawTaskBuffer   = new MemoryBuffer(1024);
            rawTasks        = new List<JsonValue>();
            writeTaskModel  = new WriteTaskModel();
            deleteTaskModel = new DeleteTaskModel();
        }

        internal void AddSyncTask(SyncRequestTask task)
        {
            switch (task.TaskType) {
                case TaskType.create:
                    lock (containers) {
                        var create = (CreateEntities)task;
                        AddWriteTask(create.containerSmall, TaskType.create, create.entities);
                        break;
                    }
                case TaskType.upsert:
                    lock (containers) {
                        var upsert = (UpsertEntities)task;
                        AddWriteTask(upsert.containerSmall, TaskType.upsert, upsert.entities);
                        break;
                    }
                case TaskType.merge:
                    lock (containers) {
                        var merge = (MergeEntities)task;
                        AddWriteTask(merge.containerSmall, TaskType.merge, merge.patches);
                        break;
                    }
                case TaskType.delete:
                    lock (containers) {
                        var delete = (DeleteEntities)task;
                        AddDeleteTask(delete.containerSmall, delete.ids);
                        break;
                    }
            }
        }
        
        private void AddWriteTask(in SmallString name, TaskType taskType, List<JsonEntity> entities) {
            if (!containers.TryGetValue(name, out var container)) {
                container = new ContainerChanges(name);
                containers.Add(name, container);
            }
            var values = writeBuffer.values;
            writeBuffer.tasks.Add(new ChangeTask(container, taskType, values.Count, entities.Count));
            foreach (var entity in entities) {
                var value = writeBuffer.valueBuffer.Add(entity.value);
                values.Add(value);
            }
        }
        
        private void AddDeleteTask(in SmallString name, List<JsonKey> ids) {
            if (!containers.TryGetValue(name, out var container)) {
                container = new ContainerChanges(name);
                containers.Add(name, container);
            }
            var keys = writeBuffer.keys;
            writeBuffer.tasks.Add(new ChangeTask(container, TaskType.delete, keys.Count, ids.Count));
            keys.AddRange(ids);
        }

        internal void AccumulateTasks(EventSubClient[] subClients, ObjectWriter writer)
        {
            lock (containers) {
                var temp    = writeBuffer;
                writeBuffer = readBuffer;
                readBuffer  = temp;
                writeBuffer.Clear();
            }
            rawTaskBuffer.Reset();
            rawTasks.Clear();
            var context = new AccumulatorContext(this, writer);
            foreach (var task in readBuffer.tasks) {
                task.container.AddChangeTask(task, readBuffer, context);
            }
            foreach (var subClient in subClients) {
                foreach (var task in rawTasks) {
                    
                }
            }
        }
    }
}
// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Utils;

// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
// ReSharper disable SwapViaDeconstruction
namespace Friflo.Json.Fliox.Hub.Host.Accumulator
{
    /// <summary>
    ///  Accumulate the entity change events for a specific <see cref="EntityDatabase"/> 
    /// </summary>
    public sealed class ChangeAccumulator
    {
        private  readonly   SmallString                                 database;
        private  readonly   Dictionary<SmallString, ContainerChanges>   containers;
        private  readonly   HashSet<ContainerChanges>                   changedContainers;
        private             TaskBuffer                                  writeBuffer;
        private             TaskBuffer                                  readBuffer;
        internal readonly   MemoryBuffer                                rawTaskBuffer;
        internal readonly   WriteTaskModel                              writeTaskModel;
        internal readonly   DeleteTaskModel                             deleteTaskModel;
        private  readonly   SyncEvent                                   syncEvent;


        public ChangeAccumulator(in SmallString database) {
            this.database       = database;
            syncEvent           = new SyncEvent {
                db                  = database.value,
                tasksJson           = new List<JsonValue>()
            };
            containers          = new Dictionary<SmallString, ContainerChanges>();
            changedContainers   = new HashSet<ContainerChanges>();
            writeBuffer         = new TaskBuffer();
            readBuffer          = new TaskBuffer();
            rawTaskBuffer       = new MemoryBuffer(1024);
            writeTaskModel      = new WriteTaskModel();
            deleteTaskModel     = new DeleteTaskModel();
        }

        public void AddSyncTask(SyncRequestTask task)
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
                foreach (var pair in containers) {
                    pair.Value.Reset();
                }
            }
            changedContainers.Clear();
            rawTaskBuffer.Reset();
            var context = new AccumulatorContext(this, writer);
            foreach (var task in readBuffer.tasks) {
                task.container.AddChangeTask(task, readBuffer, context);
                changedContainers.Add(task.container);
            }
            foreach (var container in changedContainers) {
                container.AddAccumulatedRawTask(context);
                container.currentType = TaskType.error;
            }
            foreach (var subClient in subClients) {
                if (!subClient.databaseSubs.TryGetValue(database, out var databaseSubs)) {
                    continue;
                }
                syncEvent.tasksJson.Clear();
                foreach (var container in changedContainers) {
                    foreach (var rawTask in container.rawTasks) {
                        foreach (var changeSub in databaseSubs.changeSubs) {
                            if ((changeSub.changes & rawTask.change) == 0) {
                                continue;
                            }
                            if (!changeSub.container.IsEqual(container.name)) {
                                continue;
                            }
                            syncEvent.tasksJson.Add(rawTask.value);
                        }
                    }
                }
                if (syncEvent.tasksJson.Count > 0) {
                    var rawSyncEvent = RemoteUtils.SerializeSyncEvent(syncEvent, writer);
                    subClient.EnqueueEvent(rawSyncEvent);
                }
            }
        }
    }
}
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

// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable ParameterTypeCanBeEnumerable.Local
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
namespace Friflo.Json.Fliox.Hub.Host.Accumulator
{
    /// <summary>
    ///  Accumulate the entity change events for a specific <see cref="EntityDatabase"/> 
    /// </summary>
    public sealed class ChangeAccumulator
    {
        private  readonly   Dictionary<EntityDatabase, DatabaseChanges> databaseChangesMap;
        private  readonly   List<DatabaseChanges>                       databaseChangesList;
        private  readonly   HashSet<ContainerChanges>                   containerChangesSet;
        internal readonly   MemoryBuffer                                rawTaskBuffer;
        internal readonly   WriteTaskModel                              writeTaskModel;
        internal readonly   DeleteTaskModel                             deleteTaskModel;
        private  readonly   SyncEvent                                   syncEvent;
        private  readonly   Dictionary<DatabaseSubs, JsonValue>         rawSyncEvents;
        
        public ChangeAccumulator() {
            syncEvent           = new SyncEvent { tasksJson = new List<JsonValue>() };
            databaseChangesMap  = new Dictionary<EntityDatabase, DatabaseChanges>();
            databaseChangesList = new List<DatabaseChanges>();
            containerChangesSet = new HashSet<ContainerChanges>();
            rawTaskBuffer       = new MemoryBuffer(1024);
            writeTaskModel      = new WriteTaskModel();
            deleteTaskModel     = new DeleteTaskModel();
            rawSyncEvents       = new Dictionary<DatabaseSubs, JsonValue>();
        }
        
        public void AddDatabase(EntityDatabase database) {
            var databaseChanges = new DatabaseChanges(database.name);
            lock (databaseChangesMap) {
                databaseChangesMap.Add(database, databaseChanges);
            }
        } 

        internal bool  AddSyncTask(EntityDatabase database, SyncRequestTask task)
        {
            switch (task.TaskType) {
                case TaskType.create:
                    lock (databaseChangesMap) {
                        if (!databaseChangesMap.TryGetValue(database, out var databaseChanges))
                            return false;
                        var create = (CreateEntities)task;
                        AddWriteTask(databaseChanges, create.containerSmall, TaskType.create, create.entities);
                        return true;
                    }
                case TaskType.upsert:
                    lock (databaseChangesMap) {
                        if (!databaseChangesMap.TryGetValue(database, out var databaseChanges))
                            return false;
                        var upsert = (UpsertEntities)task;
                        AddWriteTask(databaseChanges, upsert.containerSmall, TaskType.upsert, upsert.entities);
                        return true;
                    }
                case TaskType.merge:
                    lock (databaseChangesMap) {
                        if (!databaseChangesMap.TryGetValue(database, out var databaseChanges))
                            return false;
                        var merge = (MergeEntities)task;
                        AddWriteTask(databaseChanges, merge.containerSmall, TaskType.merge, merge.patches);
                        break;
                    }
                case TaskType.delete:
                    lock (databaseChangesMap) {
                        if (!databaseChangesMap.TryGetValue(database, out var databaseChanges))
                            return false;
                        var delete = (DeleteEntities)task;
                        AddDeleteTask(databaseChanges, delete.containerSmall, delete.ids);
                        return true;
                    }
            }
            return false;
        }
        
        private static void AddWriteTask(
            DatabaseChanges     databaseChanges,
            in SmallString      name,
            TaskType            taskType,
            List<JsonEntity>    entities)
        {
            var containers = databaseChanges.containers;
            if (!containers.TryGetValue(name, out var container)) {
                container = new ContainerChanges(name);
                containers.Add(name, container);
            }
            var writeBuffer = databaseChanges.writeBuffer;
            var values      = writeBuffer.values;
            var valueBuffer = writeBuffer.valueBuffer;
            writeBuffer.tasks.Add(new ChangeTask(container, taskType, values.Count, entities.Count));
            foreach (var entity in entities) {
                var value = valueBuffer.Add(entity.value);
                values.Add(value);
            }
        }
        
        private static void AddDeleteTask(
            DatabaseChanges     databaseChanges,
            in SmallString      name,
            List<JsonKey>       ids)
        {
            var containers = databaseChanges.containers;
            if (!containers.TryGetValue(name, out var container)) {
                container = new ContainerChanges(name);
                containers.Add(name, container);
            }
            var writeBuffer = databaseChanges.writeBuffer;
            var keys        = writeBuffer.keys;
            writeBuffer.tasks.Add(new ChangeTask(container, TaskType.delete, keys.Count, ids.Count));
            keys.AddRange(ids);
        }

        internal void AccumulateTasks(DatabaseSubsMap databaseSubsMap, ObjectWriter writer)
        {
            databaseChangesList.Clear();
            lock (databaseChangesMap) {
                foreach (var pair in databaseChangesMap) {
                    var databaseChanges = pair.Value;
                    databaseChanges.SwapBuffers();
                    databaseChangesList.Add(pair.Value);
                }
            }
            var context = new AccumulatorContext(this, writer);
            foreach (var databaseChanges in databaseChangesList)
            {
                containerChangesSet.Clear();
                rawTaskBuffer.Reset();
                var readBuffer  = databaseChanges.readBuffer;
                foreach (var task in readBuffer.tasks) {
                    task.containerChanges.AddChangeTask(task, readBuffer, context);
                    containerChangesSet.Add(task.containerChanges);
                }
                if (containerChangesSet.Count == 0) {
                    continue;
                }
                foreach (var container in containerChangesSet) {
                    container.AddAccumulatedRawTask(context);
                    container.currentType = TaskType.error;
                }
                var clientDbSubs = databaseSubsMap.map[databaseChanges.dbName];
                EnqueueSyncEvents(clientDbSubs, writer);
                foreach (var pair in databaseChanges.containers) {
                    pair.Value.Reset();
                }
            }
        }
        
        /// <summary>
        /// Create a serialized <see cref="SyncEvent"/>'s for the passed <paramref name="clientDbSubs"/> array
        /// </summary>
        private void EnqueueSyncEvents(ClientDbSubs[] clientDbSubs, ObjectWriter writer) {
            if (clientDbSubs.Length == 0) {
                return;
            }
            foreach (var containerChanges in containerChangesSet) {
                rawSyncEvents.Clear();
                foreach (var clientDbSub in clientDbSubs) {
                    var databaseSubs = clientDbSub.subs;
                    if (rawSyncEvents.TryGetValue(databaseSubs, out var rawSyncEvent)) {
                        if (rawSyncEvent.IsNull()) {
                            continue;
                        }
                        clientDbSub.client.EnqueueEvent(rawSyncEvent);
                        continue;
                    }
                    rawSyncEvent = EnqueueSyncEvent(containerChanges, clientDbSub, writer);
                    rawSyncEvents.Add(databaseSubs, rawSyncEvent);
                }
            }
        }
        
        /// <summary>
        /// Create a serialized <see cref="SyncEvent"/> for the passed <paramref name="container"/>
        /// and <paramref name="clientDbSubs"/> <br/>
        /// Return default if no <see cref="SyncEvent"/> was created 
        /// </summary>
        private JsonValue EnqueueSyncEvent(
            ContainerChanges    container,
            in ClientDbSubs     clientDbSubs,
            ObjectWriter        writer)
        {
            syncEvent.tasksJson.Clear();
            foreach (var changeSub in clientDbSubs.subs.changeSubs) {
                if (!changeSub.container.IsEqual(container.name)) {
                    continue;
                }
                foreach (var rawTask in container.rawTasks) {
                    if ((changeSub.changes & rawTask.change) == 0) {
                        continue;
                    }
                    syncEvent.tasksJson.Add(rawTask.value);
                }
            }
            if (syncEvent.tasksJson.Count == 0) {
                return default;
            }
            var rawSyncEvent = RemoteUtils.SerializeSyncEvent(syncEvent, writer);
            clientDbSubs.client.EnqueueEvent(rawSyncEvent);
            return rawSyncEvent;
        }
    }
}
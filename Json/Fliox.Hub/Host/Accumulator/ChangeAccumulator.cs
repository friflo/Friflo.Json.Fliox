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
        private  readonly   List<ClientSubs>                            clientSubsList;
        private  readonly   HashSet<ContainerChanges>                   containerChangesSet;
        internal readonly   MemoryBuffer                                rawTaskBuffer;
        internal readonly   WriteTaskModel                              writeTaskModel;
        internal readonly   DeleteTaskModel                             deleteTaskModel;
        private  readonly   SyncEvent                                   syncEvent;
        
        public ChangeAccumulator() {
            syncEvent           = new SyncEvent { tasksJson = new List<JsonValue>() };
            databaseChangesMap  = new Dictionary<EntityDatabase, DatabaseChanges>();
            databaseChangesList = new List<DatabaseChanges>();
            clientSubsList      = new List<ClientSubs>();
            containerChangesSet = new HashSet<ContainerChanges>();
            rawTaskBuffer       = new MemoryBuffer(1024);
            writeTaskModel      = new WriteTaskModel();
            deleteTaskModel     = new DeleteTaskModel();
        }
        
        public void AddDatabase(EntityDatabase database) {
            var databaseChanges = new DatabaseChanges(database.name);
            lock (databaseChangesMap) {
                databaseChangesMap.Add(database, databaseChanges);
            }
        } 

        public void AddSyncTask(EntityDatabase database, SyncRequestTask task)
        {
            switch (task.TaskType) {
                case TaskType.create:
                    lock (databaseChangesMap) {
                        if (!databaseChangesMap.TryGetValue(database, out var databaseChanges))
                            return;
                        var create = (CreateEntities)task;
                        AddWriteTask(databaseChanges, create.containerSmall, TaskType.create, create.entities);
                        break;
                    }
                case TaskType.upsert:
                    lock (databaseChangesMap) {
                        if (!databaseChangesMap.TryGetValue(database, out var databaseChanges))
                            return;
                        var upsert = (UpsertEntities)task;
                        AddWriteTask(databaseChanges, upsert.containerSmall, TaskType.upsert, upsert.entities);
                        break;
                    }
                case TaskType.merge:
                    lock (databaseChangesMap) {
                        if (!databaseChangesMap.TryGetValue(database, out var databaseChanges))
                            return;
                        var merge = (MergeEntities)task;
                        AddWriteTask(databaseChanges, merge.containerSmall, TaskType.merge, merge.patches);
                        break;
                    }
                case TaskType.delete:
                    lock (databaseChangesMap) {
                        if (!databaseChangesMap.TryGetValue(database, out var databaseChanges))
                            return;
                        var delete = (DeleteEntities)task;
                        AddDeleteTask(databaseChanges, delete.containerSmall, delete.ids);
                        break;
                    }
            }
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
            writeBuffer.tasks.Add(new ChangeTask(container, taskType, values.Count, entities.Count));
            foreach (var entity in entities) {
                var value = writeBuffer.valueBuffer.Add(entity.value);
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

        internal void AccumulateTasks(EventSubClient[] subClients, ObjectWriter writer)
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
                EnqueueSyncEvents(databaseChanges.dbName, subClients, writer);
                foreach (var pair in databaseChanges.containers) {
                    pair.Value.Reset();
                }
            }
        }
        
        private void EnqueueSyncEvents(in SmallString database, EventSubClient[] subClients, ObjectWriter writer) {
            clientSubsList.Clear();
            foreach (var subClient in subClients) {
                if (!subClient.databaseSubs.TryGetValue(database, out var databaseSubs))
                    continue;
                clientSubsList.Add(new ClientSubs(subClient, databaseSubs));
            }
            foreach (var containerChanges in containerChangesSet) {
                var syncEventContainerTasks = containerChanges.CreateSyncEventAllTasks(syncEvent, writer);
                foreach (var clientSubs in clientSubsList) {
                    if (EnqueueIndividualSyncEvent(containerChanges, clientSubs, writer)) {
                        continue;
                    }
                    clientSubs.client.EnqueueEvent(syncEventContainerTasks);
                }
            }
        }
        
        private bool EnqueueIndividualSyncEvent(
            ContainerChanges    container,
            in ClientSubs       clientSubs,
            ObjectWriter        writer)
        {
            syncEvent.tasksJson.Clear();
            foreach (var changeSub in clientSubs.changesSubs) {
                if (!changeSub.container.IsEqual(container.name)) {
                    continue;
                }
                if (changeSub.changes == AllChanges) {
                    return false;
                }
                foreach (var rawTask in container.rawTasks) {
                    if ((changeSub.changes & rawTask.change) == 0) {
                        continue;
                    }
                    syncEvent.tasksJson.Add(rawTask.value);
                }
            }
            if (syncEvent.tasksJson.Count > 0) {
                var rawSyncEvent = RemoteUtils.SerializeSyncEvent(syncEvent, writer);
                clientSubs.client.EnqueueEvent(rawSyncEvent);
            }
            return true;
        }
        
        private const EntityChange AllChanges = EntityChange.create | EntityChange.upsert | EntityChange.merge | EntityChange.delete;
    }
}
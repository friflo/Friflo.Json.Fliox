// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ParameterTypeCanBeEnumerable.Local
// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
namespace Friflo.Json.Fliox.Hub.Host.Event.Collector
{
    /// <summary>
    /// Collect (and accumulate) entity container changes - create, upsert, merge and delete - for registered
    /// <see cref="EntityDatabase"/>'s <br/>
    /// </summary>
    internal sealed class ChangeCollector
    {
        /// <summary>Thread safe map used to collect the <see cref="DatabaseChanges"/> for each database</summary>
        private  readonly   Dictionary<EntityDatabase, DatabaseChanges> databaseChangesMap;
        internal            int                                         DatabaseCount { get; private set; }

        
        internal ChangeCollector() {
            databaseChangesMap  = new Dictionary<EntityDatabase, DatabaseChanges>();
        }
        
        internal void AddDatabase(EntityDatabase database) {
            var databaseChanges = new DatabaseChanges(database.name);
            lock (databaseChangesMap) {
                databaseChangesMap.Add(database, databaseChanges);
                DatabaseCount = databaseChangesMap.Count;
            }
        }
        
        internal void RemoveDatabase(EntityDatabase database) {
            lock (databaseChangesMap) {
                databaseChangesMap.Remove(database);
                DatabaseCount = databaseChangesMap.Count;
            }
        }
        
        internal void GetDatabaseChanges(List<DatabaseChanges> databaseChangesList) {
            databaseChangesList.Clear();
            lock (databaseChangesMap) {
                foreach (var pair in databaseChangesMap) {
                    var databaseChanges = pair.Value;
                    databaseChanges.SwapBuffers();
                    databaseChangesList.Add(databaseChanges);
                }
            }
        }
        
        /// <summary>
        /// Store a change task in the <see cref="ChangeCollector"/> <br/>
        /// Return true if the given <paramref name="task"/> is stored. Otherwise false.
        /// </summary>
        internal bool  StoreTask(EntityDatabase database, SyncRequestTask task)
        {
            switch (task.TaskType) {
                case TaskType.create:
                    lock (databaseChangesMap) {
                        if (!databaseChangesMap.TryGetValue(database, out var databaseChanges))
                            return false;
                        var create = (CreateEntities)task;
                        StoreWriteTask(databaseChanges, create.entityContainer, TaskType.create, create.entities);
                        return true;
                    }
                case TaskType.upsert:
                    var upsert = (UpsertEntities)task;
                    if (upsert.users != null) {
                        return false;
                    }
                    lock (databaseChangesMap) {
                        if (!databaseChangesMap.TryGetValue(database, out var databaseChanges))
                            return false;
                        StoreWriteTask(databaseChanges, upsert.entityContainer, TaskType.upsert, upsert.entities);
                        return true;
                    }
                case TaskType.merge:
                    var merge = (MergeEntities)task;
                    if (merge.users != null) {
                        return false;
                    }
                    lock (databaseChangesMap) {
                        if (!databaseChangesMap.TryGetValue(database, out var databaseChanges))
                            return false;
                        StoreWriteTask(databaseChanges, merge.entityContainer, TaskType.merge, merge.patches);
                        return true;
                    }
                case TaskType.delete:
                    lock (databaseChangesMap) {
                        if (!databaseChangesMap.TryGetValue(database, out var databaseChanges))
                            return false;
                        var delete = (DeleteEntities)task;
                        StoreDeleteTask(databaseChanges, delete.entityContainer, delete.ids);
                        return true;
                    }
            }
            return false;
        }
        
        /// <summary> Store the entities of a create, upsert or merge tasks </summary>
        private static void StoreWriteTask(
            DatabaseChanges     databaseChanges,
            EntityContainer     entityContainer,
            TaskType            taskType,
            List<JsonEntity>    entities)
        {
            var containers = databaseChanges.containers;
            if (!containers.TryGetValue(entityContainer.nameSmall, out var container)) {
                container = new ContainerChanges(entityContainer);
                containers.Add(entityContainer.nameSmall, container);
            }
            var writeBuffer = databaseChanges.writeBuffer;
            var values      = writeBuffer.values;
            var valueBuffer = writeBuffer.valueBuffer;
            writeBuffer.changeTasks.Add(new ChangeTask(container, taskType, values.Count, entities.Count));
            foreach (var entity in entities) {
                values.Add(valueBuffer.Add(entity.value));
            }
        }
        
        /// <summary> Store the entity ids of a delete task </summary>
        private static void StoreDeleteTask(
            DatabaseChanges     databaseChanges,
            EntityContainer     entityContainer,
            List<JsonKey>       ids)
        {
            var containers = databaseChanges.containers;
            if (!containers.TryGetValue(entityContainer.nameSmall, out var container)) {
                container = new ContainerChanges(entityContainer);
                containers.Add(entityContainer.nameSmall, container);
            }
            var writeBuffer = databaseChanges.writeBuffer;
            var keys        = writeBuffer.keys;
            writeBuffer.changeTasks.Add(new ChangeTask(container, TaskType.delete, keys.Count, ids.Count));
            keys.AddRange(ids);
        }
    }
}
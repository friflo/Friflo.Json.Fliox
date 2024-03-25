// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ParameterTypeCanBeEnumerable.Local
// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
namespace Friflo.Json.Fliox.Hub.Host.Event.Collector
{
    /// <summary>
    /// Collect entity container changes - create, upsert, merge and delete - for registered <see cref="EntityDatabase"/>'s <br/>
    /// Collected changes can be combined by the <see cref="ChangeCombiner"/> at any time.
    /// </summary>
    /// <remarks>
    /// <see cref="EventCollector"/> is thread safe
    /// </remarks>
    internal sealed class EventCollector
    {
        /// <summary>Thread safe map used to collect the <see cref="DatabaseChanges"/> for each database</summary>
        private  readonly   Dictionary<EntityDatabase, DatabaseChanges> databaseChangesMap;
        internal            int                                         DatabaseCount { get; private set; }

        internal EventCollector() {
            databaseChangesMap  = new Dictionary<EntityDatabase, DatabaseChanges>();
        }
        
        internal void AddDatabase(EntityDatabase database) {
            var databaseChanges = new DatabaseChanges(database.nameShort);
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
        /// Store a change task in the <see cref="EventCollector"/> <br/>
        /// Return true if the given <paramref name="task"/> is stored. Otherwise false.
        /// </summary>
        internal bool  StoreTask(EntityDatabase database, SyncRequestTask task, in ShortString user)
        {
            switch (task.TaskType) {
                case TaskType.create:
                    lock (databaseChangesMap) {
                        if (!databaseChangesMap.TryGetValue(database, out var databaseChanges))
                            return false;
                        var create = (CreateEntities)task;
                        StoreWriteTask(databaseChanges, create.entityContainer, TaskType.create, create.entities, user);
                        return true;
                    }
                case TaskType.upsert:
                    var upsert = (UpsertEntities)task;
                    lock (databaseChangesMap) {
                        if (!databaseChangesMap.TryGetValue(database, out var databaseChanges))
                            return false;
                        StoreWriteTask(databaseChanges, upsert.entityContainer, TaskType.upsert, upsert.entities, user);
                        return true;
                    }
                case TaskType.merge:
                    var merge = (MergeEntities)task;
                    lock (databaseChangesMap) {
                        if (!databaseChangesMap.TryGetValue(database, out var databaseChanges))
                            return false;
                        StoreWriteTask(databaseChanges, merge.entityContainer, TaskType.merge, merge.patches, user);
                        return true;
                    }
                case TaskType.delete:
                    lock (databaseChangesMap) {
                        if (!databaseChangesMap.TryGetValue(database, out var databaseChanges))
                            return false;
                        var delete = (DeleteEntities)task;
                        StoreDeleteTask(databaseChanges, delete.entityContainer, delete.ids, user);
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
            List<JsonEntity>    entities,
            in ShortString      user)
        {
            var containers = databaseChanges.containers;
            if (!containers.TryGetValue(entityContainer.nameShort, out var container)) {
                container = new ContainerChanges(entityContainer);
                containers.Add(entityContainer.nameShort, container);
            }
            var writeBuffer = databaseChanges.writeBuffer;
            var values      = writeBuffer.values;
            var valueBuffer = writeBuffer.valueBuffer;
            writeBuffer.changeTasks.Add(new ChangeTask(container, taskType, values.Count, entities.Count, user));
            foreach (var entity in entities) {
                values.Add(valueBuffer.Add(entity.value));
            }
        }
        
        /// <summary> Store the entity ids of a delete task </summary>
        private static void StoreDeleteTask(
            DatabaseChanges     databaseChanges,
            EntityContainer     entityContainer,
            ListOne<JsonKey>    ids,
            in ShortString      user)
        {
            var containers = databaseChanges.containers;
            if (!containers.TryGetValue(entityContainer.nameShort, out var container)) {
                container = new ContainerChanges(entityContainer);
                containers.Add(entityContainer.nameShort, container);
            }
            var writeBuffer = databaseChanges.writeBuffer;
            var keys        = writeBuffer.keys;
            writeBuffer.changeTasks.Add(new ChangeTask(container, TaskType.delete, keys.Count, ids.Count, user));
            keys.AddRange(ids);
        }
    }
}
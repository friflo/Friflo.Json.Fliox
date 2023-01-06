// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
namespace Friflo.Json.Fliox.Hub.Host.Event.Compact
{
    internal sealed partial class ChangeCompactor
    {
        /// <summary>
        /// Store a change task in the <see cref="ChangeCompactor"/> <br/>
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
                        StoreWriteTask(databaseChanges, create.containerSmall, TaskType.create, create.entities);
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
                        StoreWriteTask(databaseChanges, upsert.containerSmall, TaskType.upsert, upsert.entities);
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
                        StoreWriteTask(databaseChanges, merge.containerSmall, TaskType.merge, merge.patches);
                        return true;
                    }
                case TaskType.delete:
                    lock (databaseChangesMap) {
                        if (!databaseChangesMap.TryGetValue(database, out var databaseChanges))
                            return false;
                        var delete = (DeleteEntities)task;
                        StoreDeleteTask(databaseChanges, delete.containerSmall, delete.ids);
                        return true;
                    }
            }
            return false;
        }
        
        /// <summary> Store the entities of a create, upsert or merge tasks </summary>
        private static void StoreWriteTask(
            DatabaseChanges     databaseChanges,
            in SmallString      containerName,
            TaskType            taskType,
            List<JsonEntity>    entities)
        {
            var containers = databaseChanges.containers;
            if (!containers.TryGetValue(containerName, out var container)) {
                container = new ContainerChanges(containerName);
                containers.Add(containerName, container);
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
            in SmallString      containerName,
            List<JsonKey>       ids)
        {
            var containers = databaseChanges.containers;
            if (!containers.TryGetValue(containerName, out var container)) {
                container = new ContainerChanges(containerName);
                containers.Add(containerName, container);
            }
            var writeBuffer = databaseChanges.writeBuffer;
            var keys        = writeBuffer.keys;
            writeBuffer.changeTasks.Add(new ChangeTask(container, TaskType.delete, keys.Count, ids.Count));
            keys.AddRange(ids);
        }
    }
}
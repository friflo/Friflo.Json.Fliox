// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Host.Accumulator
{
    internal sealed class ContainerChanges
    {
        internal readonly   SmallString     name;
        internal            TaskType        currentType;
        private  readonly   List<JsonValue> values;
        private  readonly   List<JsonKey>   keys;
        internal readonly   List<RawTask>   rawTasks;

        internal ContainerChanges(in SmallString name) {
            this.name   = name;
            currentType = TaskType.error;
            values      = new List<JsonValue>();
            keys        = new List<JsonKey>();
            rawTasks    = new List<RawTask>();
        }
        
        internal void Reset() {
            rawTasks.Clear();
        }
        
        private static readonly JsonValue Upsert = new JsonValue("\"upsert\"");
        private static readonly JsonValue Create = new JsonValue("\"create\"");
        private static readonly JsonValue Merge  = new JsonValue("\"merge\"");
        private static readonly JsonValue Delete = new JsonValue("\"delete\"");
        
        internal void AddChangeTask(in ChangeTask changeTask, TaskBuffer readBuffer, in AccumulatorContext context)
        {
            if (changeTask.taskType != currentType) {
                AddAccumulatedRawTask(context);
                currentType = changeTask.taskType;
            }
            switch(changeTask.taskType) {
                case TaskType.create:
                case TaskType.upsert:
                case TaskType.merge:
                    for (int n = 0; n < changeTask.count; n++) {
                        values.Add(readBuffer.values[changeTask.start + n]);
                    }
                    break;
                case TaskType.delete:
                    for (int n = 0; n < changeTask.count; n++) {
                        keys.Add(readBuffer.keys[changeTask.start + n]);
                    }
                    break;
            }
        }
        
        internal void AddAccumulatedRawTask(in AccumulatorContext context)
        {
            var acc = context.accumulator;
            switch (currentType) {
                case TaskType.upsert: {
                    acc.writeTaskModel.Set(Upsert, name, values);
                    var rawTask = new JsonValue(context.writer.WriteAsBytes(acc.writeTaskModel));
                    rawTasks.Add(new RawTask(EntityChange.upsert, acc.rawTaskBuffer.Add(rawTask)));
                    values.Clear();
                    break;
                }
                case TaskType.create: {
                    acc.writeTaskModel.Set(Create, name, values);
                    var rawTask = new JsonValue(context.writer.WriteAsBytes(acc.writeTaskModel));
                    rawTasks.Add(new RawTask(EntityChange.create, acc.rawTaskBuffer.Add(rawTask)));
                    values.Clear();
                    break;
                }
                case TaskType.merge: {
                    acc.writeTaskModel.Set(Merge, name, values);
                    var rawTask = new JsonValue(context.writer.WriteAsBytes(acc.writeTaskModel));
                    rawTasks.Add(new RawTask(EntityChange.merge, acc.rawTaskBuffer.Add(rawTask)));
                    values.Clear();
                    break;
                }
                case TaskType.delete: {
                    acc.deleteTaskModel.Set(Delete, name, keys);
                    var rawTask = new JsonValue(context.writer.WriteAsBytes(acc.deleteTaskModel));
                    rawTasks.Add(new RawTask(EntityChange.delete, acc.rawTaskBuffer.Add(rawTask)));
                    keys.Clear();
                    break;
                }
            }
        }
        
        internal JsonValue CreateSyncEventAllTasks(SyncEvent syncEvent, ObjectWriter writer)
        {
            if (rawTasks.Count == 0) {
                return default;
            }
            syncEvent.tasksJson.Clear();
            foreach (var rawTask in rawTasks) {
                syncEvent.tasksJson.Add(rawTask.value);
            }
            return RemoteUtils.SerializeSyncEvent(syncEvent, writer);
        }
    }
}
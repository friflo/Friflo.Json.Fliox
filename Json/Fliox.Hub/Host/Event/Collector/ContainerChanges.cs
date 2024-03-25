// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.Host.Event.Collector
{
    /// <summary>
    /// Used to collect of changes - create, upsert, merge and delete - of a specific <see cref="EntityContainer"/>
    /// </summary>
    internal sealed class ContainerChanges
    {
        internal readonly   ShortString     name;
        internal            TaskType        currentType;
        private  readonly   List<JsonValue> values;
        private  readonly   List<JsonKey>   keys;
        internal readonly   List<RawTask>   rawTasks;

        public   override   string          ToString() => name.AsString();

        internal ContainerChanges(EntityContainer entityContainer) {
            name        = entityContainer.nameShort;
            currentType = default;
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
        
        internal void AddChangeTask(in ChangeTask changeTask, TaskBuffer readBuffer, in CombinerContext context)
        {
            if (changeTask.taskType != currentType) {
                AddAccumulatedRawTask(context);
                currentType = changeTask.taskType;
            }
            switch(changeTask.taskType) {
                case TaskType.create:
                case TaskType.upsert:
                case TaskType.merge:
                    var entityValues = readBuffer.values;
                    for (int n = 0; n < changeTask.count; n++) {
                        values.Add(entityValues[changeTask.start + n]);
                    }
                    break;
                case TaskType.delete:
                    var entityKeys = readBuffer.keys;
                    for (int n = 0; n < changeTask.count; n++) {
                        keys.Add(entityKeys[changeTask.start + n]);
                    }
                    break;
            }
        }
        
        internal void AddAccumulatedRawTask(in CombinerContext context)
        {
            var combiner = context.combiner;
            switch (currentType) {
                case TaskType.upsert: {
                    combiner.writeTaskModel.Set(Upsert, name, values);
                    var rawTask = new JsonValue(context.writer.WriteAsBytes(combiner.writeTaskModel));
                    rawTasks.Add(new RawTask(EntityChange.upsert, combiner.rawTaskBuffer.Add(rawTask)));
                    values.Clear();
                    break;
                }
                case TaskType.create: {
                    combiner.writeTaskModel.Set(Create, name, values);
                    var rawTask = new JsonValue(context.writer.WriteAsBytes(combiner.writeTaskModel));
                    rawTasks.Add(new RawTask(EntityChange.create, combiner.rawTaskBuffer.Add(rawTask)));
                    values.Clear();
                    break;
                }
                case TaskType.merge: {
                    combiner.writeTaskModel.Set(Merge, name, values);
                    var rawTask = new JsonValue(context.writer.WriteAsBytes(combiner.writeTaskModel));
                    rawTasks.Add(new RawTask(EntityChange.merge, combiner.rawTaskBuffer.Add(rawTask)));
                    values.Clear();
                    break;
                }
                case TaskType.delete: {
                    combiner.deleteTaskModel.Set(Delete, name, keys);
                    var rawTask = new JsonValue(context.writer.WriteAsBytes(combiner.deleteTaskModel));
                    rawTasks.Add(new RawTask(EntityChange.delete, combiner.rawTaskBuffer.Add(rawTask)));
                    keys.Clear();
                    break;
                }
            }
        }
    }
}
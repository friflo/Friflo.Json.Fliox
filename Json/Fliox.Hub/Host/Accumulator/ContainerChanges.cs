// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.Host.Accumulator
{
    internal sealed class ContainerChanges
    {
        private  readonly   SmallString     name;
        private             TaskType        currentType;
        private  readonly   List<JsonValue> values;
        private  readonly   List<JsonKey>   keys;

        internal ContainerChanges(in SmallString name) {
            this.name   = name;
            values      = new List<JsonValue>();
            keys        = new List<JsonKey>();
        }
        
        private static readonly JsonValue Upsert = new JsonValue("\"upsert\"");
        private static readonly JsonValue Create = new JsonValue("\"create\"");
        private static readonly JsonValue Merge  = new JsonValue("\"merge\"");
        private static readonly JsonValue Delete = new JsonValue("\"delete\"");
        
        internal void AddChangeTask(in ChangeTask changeTask, TaskBuffer readBuffer, in AccumulatorContext context)
        {
            if (changeTask.taskType == currentType) {
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
                return;
            }
            AddRawTask(changeTask, context);
        }
        
        private void AddRawTask(in ChangeTask changeTask, in AccumulatorContext context)
        {
            var acc = context.accumulator;
            switch (currentType) {
                case TaskType.upsert: {
                    acc.writeTaskModel.Set(Upsert, name, values);
                    var rawTask = new JsonValue(context.writer.WriteAsBytes(acc.writeTaskModel));
                    acc.rawTasks.Add(acc.rawTaskBuffer.Add(rawTask));
                    values.Clear();
                    break;
                }
                case TaskType.create: {
                    acc.writeTaskModel.Set(Create, name, values);
                    var rawTask = new JsonValue(context.writer.WriteAsBytes(acc.writeTaskModel));
                    acc.rawTasks.Add(acc.rawTaskBuffer.Add(rawTask));
                    values.Clear();
                    break;
                }
                case TaskType.merge: {
                    acc.writeTaskModel.Set(Merge, name, values);
                    var rawTask = new JsonValue(context.writer.WriteAsBytes(acc.writeTaskModel));
                    acc.rawTasks.Add(acc.rawTaskBuffer.Add(rawTask));
                    values.Clear();
                    break;
                }
                case TaskType.delete: {
                    acc.deleteTaskModel.Set(Delete, name, keys);
                    var rawTask = new JsonValue(context.writer.WriteAsBytes(acc.deleteTaskModel));
                    acc.rawTasks.Add(acc.rawTaskBuffer.Add(rawTask));
                    keys.Clear();
                    break;
                }
            }
            currentType = changeTask.taskType;
        }
    }
}
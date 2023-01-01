// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Utils;

namespace Friflo.Json.Fliox.Hub.Host.Accumulator
{
    internal sealed class ContainerChanges
    {
        private  readonly   SmallString     name;
        private             TaskType        currentType;
        private  readonly   List<JsonValue> entities    = new List<JsonValue>();

        internal ContainerChanges(in SmallString name) {
            this.name  = name;
        }
        
        private static readonly JsonValue Upsert = new JsonValue("\"upsert\"");
        private static readonly JsonValue Create = new JsonValue("\"create\"");
        private static readonly JsonValue Merge  = new JsonValue("\"merge\"");
        
        internal void AddEvent(in MessageItem<ValueChange> ev, in AccumulatorContext context) {
            var accumulator = context.accumulator;
            var task        = accumulator.taskBuffer;

            if (ev.meta.taskType != currentType) {
                switch (currentType) {
                    case TaskType.upsert: {
                        task.Set(Upsert, name.value, entities);
                        var rawTask = new JsonValue(context.writer.WriteAsBytes(task));
                        accumulator.changeTasks.Add(accumulator.valueBuffer.Add(rawTask));
                        entities.Clear();
                        break;
                    }
                    case TaskType.create: {
                        task.Set(Create, name.value, entities);
                        var rawTask = new JsonValue(context.writer.WriteAsBytes(task));
                        accumulator.changeTasks.Add(accumulator.valueBuffer.Add(rawTask));
                        entities.Clear();
                        break;
                    }
                    case TaskType.merge: {
                        task.Set(Merge, name.value, entities);
                        var rawTask = new JsonValue(context.writer.WriteAsBytes(task));
                        accumulator.changeTasks.Add(accumulator.valueBuffer.Add(rawTask));
                        entities.Clear();
                        break;
                    }
                }
                currentType = ev.meta.taskType;
                entities.Add(ev.value);
            }
        }
    }
}
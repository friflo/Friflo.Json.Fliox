// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Utils;

namespace Friflo.Json.Fliox.Hub.Host.Accumulator
{
    internal sealed class WriteTaskModel
    {
        internal    JsonValue       task;
        internal    string          cont;
        internal    List<JsonValue> set;
        
        internal void Set(in JsonValue taskType, in SmallString container, List<JsonValue> entities) {
            task    = taskType;
            cont    = container.value;
            set     = entities;
        }
    }
    
    internal sealed class DeleteTaskModel
    {
        internal    JsonValue       task;
        internal    string          cont;
        internal    List<JsonKey>   ids;
        
        internal void Set(in JsonValue taskType, in SmallString container, List<JsonKey> keys) {
            task    = taskType;
            cont    = container.value;
            ids     = keys;
        }
    }
    
    internal readonly struct ChangeTask {
        internal readonly TaskType          taskType;
        internal readonly ContainerChanges  container;
        internal readonly int               start;
        internal readonly int               count;

        internal ChangeTask(ContainerChanges container, TaskType taskType, int start, int count) {
            this.taskType   = taskType;
            this.container  = container;
            this.start      = start;
            this.count      = count;
        }
    }
    
    internal sealed class TaskBuffer {
        internal readonly List<ChangeTask>  tasks       = new List<ChangeTask>();
        internal readonly List<JsonValue>   values      = new List<JsonValue>();
        internal readonly MemoryBuffer      valueBuffer = new MemoryBuffer(1024);
        internal readonly List<JsonKey>     keys        = new List<JsonKey>();
        
        internal void Clear() {
            tasks.Clear();
            values.Clear();
            valueBuffer.Reset();
            keys.Clear();
        }
    }
    
    internal readonly struct AccumulatorContext
    {
        internal readonly ObjectWriter      writer;
        internal readonly ChangeAccumulator accumulator;
        
        internal AccumulatorContext(ChangeAccumulator accumulator, ObjectWriter writer) {
            this.accumulator    = accumulator;
            this.writer         = writer;
        }
    }
}
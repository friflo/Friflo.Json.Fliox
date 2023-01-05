// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Utils;

namespace Friflo.Json.Fliox.Hub.Host.Accumulator
{
    internal readonly struct ChangeTask {
        internal readonly TaskType          taskType;
        internal readonly ContainerChanges  containerChanges;
        internal readonly int               start;
        internal readonly int               count;

        internal ChangeTask(ContainerChanges containerChanges, TaskType taskType, int start, int count) {
            this.taskType           = taskType;
            this.containerChanges   = containerChanges;
            this.start              = start;
            this.count              = count;
        }
    }
    
    internal sealed class TaskBuffer {
        internal readonly List<ChangeTask>  changeTasks = new List<ChangeTask>();
        internal readonly List<JsonValue>   values      = new List<JsonValue>();
        internal readonly MemoryBuffer      valueBuffer = new MemoryBuffer(1024);
        internal readonly List<JsonKey>     keys        = new List<JsonKey>();
        
        internal void Clear() {
            changeTasks.Clear();
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
    
    internal readonly struct RawTask
    {
        internal readonly EntityChange  change;
        internal readonly JsonValue     value;
        
        internal RawTask(EntityChange change, in JsonValue value) {
            this.change     = change;
            this.value      = value;
        }
    }
}
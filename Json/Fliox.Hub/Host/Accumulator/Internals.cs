// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Host.Accumulator
{
    internal sealed class ChangeEventTask
    {
        internal    JsonValue       task;
        internal    string          cont;
        internal    List<JsonValue> set;
        
        internal void Set(in JsonValue taskType, string container, List<JsonValue> entities) {
            task    = taskType;
            cont    = container;
            set     = entities;
        }
    }
    
    internal readonly struct ValueChange {
        internal readonly TaskType          taskType;
        internal readonly ContainerChanges  container;

        internal ValueChange(ContainerChanges container, TaskType taskType) {
            this.taskType   = taskType;
            this.container  = container;
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
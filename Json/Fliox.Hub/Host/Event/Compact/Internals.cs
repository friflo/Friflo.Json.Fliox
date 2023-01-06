// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Utils;

namespace Friflo.Json.Fliox.Hub.Host.Event.Compact
{
    internal readonly struct ChangeTask {
        internal readonly ContainerChanges  containerChanges;
        internal readonly TaskType          taskType;
        internal readonly int               start;
        internal readonly int               count;

        public   override string            ToString() => $"{taskType} count: {count}";

        internal ChangeTask(ContainerChanges containerChanges, TaskType taskType, int start, int count) {
            this.containerChanges   = containerChanges;
            this.taskType           = taskType;
            this.start              = start;
            this.count              = count;
        }
    }
    
    internal sealed class TaskBuffer {
        internal readonly   List<ChangeTask>    changeTasks = new List<ChangeTask>();
        internal readonly   List<JsonValue>     values      = new List<JsonValue>();
        internal readonly   MemoryBuffer        valueBuffer = new MemoryBuffer(1024);
        internal readonly   List<JsonKey>       keys        = new List<JsonKey>();

        public   override   string ToString() => $"changes: {changeTasks.Count}, values: {values.Count}, keys: {keys.Count}";

        internal void Clear() {
            changeTasks.Clear();
            values.Clear();
            valueBuffer.Reset();
            keys.Clear();
        }
    }
    
    internal readonly struct CompactorContext
    {
        internal readonly   ChangeCompactor compactor;
        internal readonly   ObjectWriter    writer;
        
        internal CompactorContext(ChangeCompactor compactor, ObjectWriter writer) {
            this.compactor  = compactor;
            this.writer     = writer;
        }
    }
    
    internal readonly struct RawTask
    {
        internal readonly   EntityChange    change;
        internal readonly   JsonValue       value;
        
        public   override   string          ToString() => $"{change}";
        
        internal RawTask(EntityChange change, in JsonValue value) {
            this.change     = change;
            this.value      = value;
        }
    }
}
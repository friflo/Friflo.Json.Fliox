// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Utils;

namespace Friflo.Json.Fliox.Hub.Host.Event.Collector
{
    internal readonly struct ChangeTask
    {
        /// <summary>Used to collect all changes of a specific container</summary>
        internal readonly   ContainerChanges    containerChanges;
        /// <summary>The type of container mutation: create, upsert, merge or delete</summary>
        internal readonly   TaskType            taskType;
        /// <summary>start position in either <see cref="TaskBuffer.values"/> or <see cref="TaskBuffer.keys"/></summary>
        internal readonly   int                 start;
        /// <summary>item count in either <see cref="TaskBuffer.values"/> or <see cref="TaskBuffer.keys"/></summary>
        internal readonly   int                 count;
        internal readonly   ShortString         user;

        public   override   string              ToString() => $"{taskType} '{containerChanges.name.AsString()}' count: {count}";

        internal ChangeTask(ContainerChanges containerChanges, TaskType taskType, int start, int count, in ShortString user) {
            this.containerChanges   = containerChanges;
            this.taskType           = taskType;
            this.start              = start;
            this.count              = count;
            this.user               = user;
        }
    }
    
    internal sealed class TaskBuffer
    {
        internal readonly   List<ChangeTask>    changeTasks = new List<ChangeTask>();
        /// <summary>store entities of create, upsert and merge tasks</summary>
        internal readonly   List<JsonValue>     values      = new List<JsonValue>();
        internal readonly   MemoryBuffer        valueBuffer = new MemoryBuffer(1024);
        /// <summary>store deleted entities of a delete tasks</summary>
        internal readonly   List<JsonKey>       keys        = new List<JsonKey>();

        public   override   string ToString() => $"changes: {changeTasks.Count}, values: {values.Count}, keys: {keys.Count}";

        internal void Clear() {
            changeTasks.Clear();
            values.Clear();
            valueBuffer.Reset();
            keys.Clear();
        }
    }
    
    internal readonly struct CombinerContext
    {
        internal readonly   ChangeCombiner  combiner;
        internal readonly   ObjectWriter    writer;
        
        internal CombinerContext(ChangeCombiner combiner, ObjectWriter writer) {
            this.combiner   = combiner;
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
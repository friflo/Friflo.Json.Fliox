// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Flow.Graph.Internal;
using Friflo.Json.Flow.Mapper.Map.Val;

namespace Friflo.Json.Flow.Graph
{
    public class MessageTask : SyncTask
    {
        internal readonly   string      tag;
        internal readonly   JsonValue   value;
        
        internal            string      result;
        
        internal            TaskState   state;
        internal override   TaskState   State       => state;
        
        public   override   string      Details     => $"MessageTask (message: {tag})";
        
        
        public              string      Result      => IsOk("MessageTask.Result", out Exception e) ? result : throw e;
        
        internal MessageTask(string tag, string value) {
            this.tag = tag;
            this.value = new JsonValue {json = value };
        }
        
    }
}
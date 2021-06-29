// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Flow.Graph.Internal;
using Friflo.Json.Flow.Mapper.Map.Val;

namespace Friflo.Json.Flow.Graph
{
    public class SendMessageTask : SyncTask
    {
        internal readonly   string      name;
        internal readonly   JsonValue   value;
        
        internal            string      result;
        
        internal            TaskState   state;
        internal override   TaskState   State       => state;
        
        public   override   string      Details     => $"MessageTask (name: {name})";
        
        
        public              string      Result      => IsOk("MessageTask.Result", out Exception e) ? result : throw e;
        
        internal SendMessageTask(string name, string value) {
            this.name = name;
            this.value = new JsonValue {json = value };
        }
        
    }
}
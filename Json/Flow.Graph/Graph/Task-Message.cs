// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Flow.Graph.Internal;

namespace Friflo.Json.Flow.Graph
{
    public class MessageTask : SyncTask
    {
        internal readonly   string      message;
        internal            string      result;
        
        internal            TaskState   state;
        internal override   TaskState   State       => state;
        
        public   override   string      Details     => $"MessageTask (message: {message})";
        
        
        public              string      Result      => IsOk("MessageTask.Result", out Exception e) ? result : throw e;
        
        internal MessageTask(string message) {
            this.message = message;
        }
        
    }
}
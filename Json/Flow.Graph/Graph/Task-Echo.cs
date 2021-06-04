// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Flow.Graph.Internal;

namespace Friflo.Json.Flow.Graph
{
    public class EchoTask : SyncTask
    {
        internal readonly   string      message;
        internal            string      result;
        
        internal            TaskState   state;
        internal override   TaskState   State       => state;
        
        internal override   string      Label       => name ?? $"EchoTask (message: {message})";
        public   override   string      ToString()  => Label;
        
        public              EchoTask    TaskName (string name) { this.name = name; return this; }
        
        public              string      Result      => IsOk("EchoTask.Result", out Exception e) ? result : throw e;
        
        internal EchoTask(string message) {
            this.message = message;
        }
        
    }
}
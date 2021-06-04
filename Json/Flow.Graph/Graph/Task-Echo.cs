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
        
        public   override   string      Details     => $"EchoTask (message: {message})";
        public   override   string      ToString()  => GetLabel();
        
        public              EchoTask    TaskName (string name) { this.name = name; return this; }
        
        public              string      Result      => IsOk("EchoTask.Result", out Exception e) ? result : throw e;
        
        internal EchoTask(string message) {
            this.message = message;
        }
        
    }
}
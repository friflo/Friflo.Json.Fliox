// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{

    public sealed class CloseCursorsTask : SyncTask
    {
        internal readonly   List<string>    cursors;
        
        [DebuggerBrowsable(Never)]
        internal            TaskState       state;
        internal override   TaskState       State       => state;
        internal            int             count;
        
        public   override   string          Details     => GetDetails();
        internal override   TaskType        TaskType    => TaskType.closeCursors;
        public              int             Count       => IsOk("CloseCursorsTask.Count", out Exception e) ? count : throw e;


        internal CloseCursorsTask(IEnumerable<string> cursors, Set entitySet) : base(entitySet) {
            this.cursors    = cursors != null ? new List<string>(cursors) : null;
        }
        
        private string GetDetails() {
            if (cursors == null)
                return $"CloseCursorsTask (container: {taskSet.name}, cursors: all)";
            return $"CloseCursorsTask (container: {taskSet.name}, cursors: {count})";
        }
        
        internal override SyncRequestTask CreateRequestTask(in CreateTaskContext context) {
            return taskSet.CloseCursors(this);
        }
    }
}


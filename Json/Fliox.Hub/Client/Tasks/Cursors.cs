// Copyright (c) Ullrich Praetz. All rights reserved.
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
        [DebuggerBrowsable(Never)]
        private readonly    SyncSet         syncSet;
        
        public   override   string          Details     => GetDetails();
        internal override   TaskType        TaskType    => TaskType.closeCursors;
        public              int             Count       => IsOk("CloseCursorsTask.Count", out Exception e) ? count : throw e;


        internal CloseCursorsTask(IEnumerable<string> cursors, SyncSet syncSet) : base(syncSet.EntitySet.name) {
            this.cursors    = cursors != null ? new List<string>(cursors) : null;
            this.syncSet    = syncSet;
        }
        
        private string GetDetails() {
            if (cursors == null)
                return $"CloseCursorsTask (container: {syncSet.EntitySet.name}, cursors: all)";
            return $"CloseCursorsTask (container: {syncSet.EntitySet.name}, cursors: {count})";
        }
        
        internal override SyncRequestTask CreateRequestTask(in CreateTaskContext context) {
            return syncSet.CloseCursors(this);
        }
    }
}


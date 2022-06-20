// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Transform;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    public sealed class SubscribeChangesTask<T> : SyncTask where T : class
    {
        [DebuggerBrowsable(Never)]
        internal            TaskState       state;
        internal            List<Change>    changes;
        internal            FilterOperation filter;
        private             string          filterLinq; // use as string identifier of a filter
        [DebuggerBrowsable(Never)]
        private readonly    SyncSetBase<T>  syncSet;
            
        internal override   TaskState       State   => state;
        public   override   string          Details => $"SubscribeChangesTask<{typeof(T).Name}> (filter: {filterLinq})";
        internal override   TaskType        TaskType=> TaskType.subscribeChanges;
        
        internal  SubscribeChangesTask(SyncSetBase<T> syncSet) {
            this.syncSet    = syncSet;
        }
            
        internal void Set(IEnumerable<Change> changes, FilterOperation filter) {
            this.changes    = changes != null ? changes.ToList() : new List<Change>();
            this.filter     = filter;
            this.filterLinq = filter.Linq;
        }
        
        internal override SyncRequestTask CreateRequestTask() {
            return syncSet.SubscribeChanges(this);
        }
    }
    
    public sealed class SubscribeMessageTask : SyncTask
    {
        internal readonly   string      name;
        internal readonly   bool?       remove;
        [DebuggerBrowsable(Never)]
        internal            TaskState   state;
            
        internal override   TaskState   State   => state;
        public   override   string      Details => $"SubscribeMessageTask (name: {name})";
        internal override   TaskType    TaskType=> TaskType.subscribeMessage;
        

        internal SubscribeMessageTask(string name, bool? remove) {
            this.name   = name;
            this.remove = remove;
        }
    }
}
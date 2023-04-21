// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Transform;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    // ----------------------------------------- AggregateTask -----------------------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public abstract class AggregateTask : SyncTask
    {
        [DebuggerBrowsable(Never)]
        internal            TaskState       state;
        internal readonly   FilterOperation filter;
        internal readonly   string          filterLinq; // use as string identifier of a filter 
        internal            double?         result;
        [DebuggerBrowsable(Never)]
        private readonly    SyncSet         syncSet;

        internal override   TaskState       State       => state;
        internal override   TaskType        TaskType    => TaskType.aggregate;
        
        internal abstract   AggregateType   Type        { get; }

        internal AggregateTask(FilterOperation filter, SyncSet syncSet) {
            this.filter     = filter;
            this.filterLinq = filter.Linq;
            this.syncSet    = syncSet;
        }
        
        internal override SyncRequestTask CreateRequestTask(in CreateTaskContext context) {
            return syncSet.AggregateEntities(this, context);
        }
    }
    
    // ----------------------------------------- CountTask<> -----------------------------------------
    public sealed class CountTask<T> : AggregateTask where T : class
    {
        public              int             Result  => IsOk("CountTask.Result",  out Exception e) ? (int?)result ?? 0 : throw e;
        public   override   string          Details => $"CountTask<{typeof(T).Name}> (filter: {filterLinq})";
        internal override   AggregateType   Type    => AggregateType.count;

        internal CountTask(FilterOperation filter, SyncSet syncSet)
            : base(filter, syncSet)
        { }
    }
}


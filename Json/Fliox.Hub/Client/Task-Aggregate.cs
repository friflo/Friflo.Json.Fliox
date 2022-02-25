// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Fliox.Hub.Client
{
    // ----------------------------------------- AggregateTask -----------------------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public abstract class AggregateTask : SyncTask
    {
        internal            TaskState       state;
        internal readonly   FilterOperation filter;
        internal readonly   string          filterLinq; // use as string identifier of a filter 
        internal            double?         result;

        internal override   TaskState       State       => state;
        public              QueryFormat     DebugQuery  => filter.query;
        
        internal abstract   AggregateType   Type        { get; }

        internal AggregateTask(FilterOperation filter) {
            this.filter     = filter;
            this.filterLinq = filter.Linq;
        }
    }
    
    // ----------------------------------------- CountTask<> -----------------------------------------
    public sealed class CountTask<T> : AggregateTask where T : class
    {
        public              int             Result  => IsOk("CountTask.Result",  out Exception e) ? (int?)result ?? 0 : throw e;
        public   override   string          Details => $"CountTask<{typeof(T).Name}> (filter: {filterLinq})";
        internal override   AggregateType   Type    => AggregateType.count;

        internal CountTask(FilterOperation filter)
            : base(filter)
        { }
    }
}


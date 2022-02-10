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
    public sealed class AggregateTask<TKey, T> : SyncTask where T : class
    {
        internal            TaskState               state;
        internal readonly   AggregateType           type;
        internal readonly   FilterOperation         filter;
        internal readonly   string                  filterLinq; // use as string identifier of a filter 
        internal            double?                 result;

        public              double?                 Result          => IsOk("AggregateTask.Result",  out Exception e) ? result      : throw e;
            
        internal override   TaskState               State           => state;
        public   override   string                  Details         => $"AggregateTask<{typeof(T).Name}> (type: {type} filter: {filterLinq})";
        public              QueryFormat             DebugQuery      => filter.query;
        

        internal AggregateTask(AggregateType type, FilterOperation filter) {
            this.type       = type;
            this.filter     = filter;
            this.filterLinq = filter.Linq;
        }
    }
}


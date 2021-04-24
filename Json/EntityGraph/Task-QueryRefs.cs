// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Friflo.Json.EntityGraph
{
    // ----------------------------------------- QueryRefsTask -----------------------------------------
    public class QueryRefsTask
    {
        internal readonly   string      selector;
        internal readonly   Type        entityType;
        internal readonly   string      filterLinq;
        internal readonly   EntitySet   parentSet;

        private             string      DebugName => $"{parentSet.name}['{filterLinq}'] {selector}";
        public   override   string      ToString() => DebugName;
        
        internal QueryRefsTask(string filterLinq, EntitySet parentSet, string selector, Type entityType) {
            this.filterLinq         = filterLinq;
            this.parentSet          = parentSet;
            this.selector           = selector;
            this.entityType         = entityType;
        }
        
        protected Exception Error(string message) {
            return new TaskNotSyncedException($"{message} {DebugName}");
        }
    }

    public class QueryRefsTask<T> : QueryRefsTask where T : Entity
    {
        internal            bool                    synced;
        internal readonly   List<QueryRefsTask<T>>  results = new List<QueryRefsTask<T>>();
        
        public              List<QueryRefsTask<T>>  Results         => synced ? results         : throw Error("QueryRefsTask.Results requires Sync().");
        public              QueryRefsTask<T>        this[int index] => synced ? results[index]  : throw Error("QueryRefsTask[] requires Sync().");

        internal QueryRefsTask(string filterLinq, EntitySet parentSet, string selector, Type entityType) :
            base (filterLinq, parentSet, selector, entityType) { }
    }
    
}
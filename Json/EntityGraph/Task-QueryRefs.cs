// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Friflo.Json.EntityGraph
{
    // ----------------------------------------- QueryRefsTask -----------------------------------------
    public class QueryRefsTask
    {
        internal readonly   string      parentId;
        internal readonly   EntitySet   parentSet;
        internal readonly   string      label;

        private             string      DebugName => $"{parentSet.name}['{parentId}'] {label}";
        public   override   string      ToString() => DebugName;
        
        internal QueryRefsTask(string parentId, EntitySet parentSet, string label) {
            this.parentId           = parentId;
            this.parentSet          = parentSet;
            this.label              = label;
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

        internal QueryRefsTask(string parentId, EntitySet parentSet, string label) : base (parentId, parentSet, label) { }
    }
    
    internal class QueryRefsTaskMap
    {
        internal readonly   string                              selector;
        internal readonly   Type                                entityType;
        /// key: <see cref="QueryTask{T}.filterLinq"/>
        internal readonly   Dictionary<string, QueryRefsTask>   queryRefs = new Dictionary<string, QueryRefsTask>();
        
        internal QueryRefsTaskMap(string selector, Type entityType) {
            this.selector = selector;
            this.entityType = entityType;
        }
    }
}
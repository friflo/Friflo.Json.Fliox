// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Friflo.Json.EntityGraph
{
    // ----------------------------------------- ReadRefTask -----------------------------------------
    public class ReadRefsTask
    {
        internal readonly   string      parentId;
        internal readonly   EntitySet   parentSet;
        internal readonly   bool        singleResult;
        internal readonly   string      label;
        internal            bool        synced;

        internal            string      DebugName => $"{parentSet.name}['{parentId}'] {label}";
        public   override   string      ToString() => DebugName;
        
        internal ReadRefsTask(string parentId, EntitySet parentSet, string label, bool singleResult) {
            this.parentId       = parentId;
            this.parentSet      = parentSet;
            this.singleResult   = singleResult;
            this.label          = label;
        }
        
        protected Exception RequiresSyncError(string message) {
            return new TaskNotSyncedException($"{message} {DebugName}");
        }
        
        internal Exception AlreadySyncedError() {
            return new InvalidOperationException($"Used QueryTask is already synced. QueryTask<{parentSet.name}>, filter: {label}");
        }
    }
    
    public class ReadRefTask<T> : ReadRefsTask where T : Entity
    {
        internal    string      id;
        internal    T           entity;

        public      string      Id      => synced ? id      : throw RequiresSyncError("ReadRefTask.Id requires Sync().");
        public      T           Result  => synced ? entity  : throw RequiresSyncError("ReadRefTask.Result requires Sync().");

        internal ReadRefTask(string parentId, EntitySet parentSet, string label) : base (parentId, parentSet, label, true) { }
        

    }
    
    public class ReadRefsTask<T> : ReadRefsTask where T : Entity
    {
        internal readonly   Dictionary<string, T>   results = new Dictionary<string, T>();
            
        public              Dictionary<string, T>   Results         => synced ? results      : throw RequiresSyncError("ReadRefsTask.Results requires Sync().");
        public              T                       this[string id] => synced ? results[id]  : throw RequiresSyncError("ReadRefsTask[] requires Sync().");

        internal ReadRefsTask(string parentId, EntitySet parentSet, string label) : base (parentId, parentSet, label, false) { }
        

    }
    
    internal class ReadRefsTaskMap
    {
        internal readonly   string                           selector;
        internal readonly   Type                             entityType;
        /// key: <see cref="ReadTask{T}.id"/>
        internal readonly   Dictionary<string, ReadRefsTask> readRefs = new Dictionary<string, ReadRefsTask>();
        
        internal ReadRefsTaskMap(string selector, Type entityType) {
            this.selector = selector;
            this.entityType = entityType;
        }
    }
}
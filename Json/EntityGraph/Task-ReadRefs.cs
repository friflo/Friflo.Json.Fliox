// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Friflo.Json.EntityGraph
{
    // ----------------------------------------- ReadRefTask -----------------------------------------
    public class ReadRefsTask
    {
        internal readonly   string      parentId;
        internal readonly   EntitySet   parentSet;
        internal readonly   bool        singleResult;
        internal readonly   string      label;

        private             string      DebugName => $"{parentSet.name}['{parentId}'] {label}";
        public   override   string      ToString() => DebugName;
        
        internal ReadRefsTask(string parentId, EntitySet parentSet, string label, bool singleResult) {
            this.parentId           = parentId;
            this.parentSet          = parentSet;
            this.singleResult       = singleResult;
            this.label              = label;
        }
        
        protected Exception Error(string message) {
            return new TaskNotSyncedException($"{message} {DebugName}");
        }
    }
    
    public class ReadRefTask<T> : ReadRefsTask where T : Entity
    {
        internal    string      id;
        internal    T           entity;
        internal    bool        synced;

        public      string      Id      => synced ? id      : throw Error("ReadRefTask.Id requires Sync().");
        public      T           Result  => synced ? entity  : throw Error("ReadRefTask.Result requires Sync().");

        internal ReadRefTask(string parentId, EntitySet parentSet, string label) : base (parentId, parentSet, label, true) { }
    }
    
    public class ReadRefsTask<T> : ReadRefsTask where T : Entity
    {
        internal            bool        synced;
        internal readonly   List<T>     results = new List<T>();
            
        public              List<T>     Results         => synced ? results         : throw Error("ReadRefsTask.Results requires Sync().");
        public              T           this[int index] => synced ? results[index]  : throw Error("ReadRefsTask[] requires Sync().");

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
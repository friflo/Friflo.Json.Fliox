// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Friflo.Json.EntityGraph
{
    // ----------------------------------------- ReadRefTask -----------------------------------------
    public class ReadRefTask
    {
        internal readonly   string      parentId;
        internal readonly   EntitySet   parentSet;
        internal readonly   bool        singleResult;
        internal readonly   string      label;

        private             string      DebugName => $"{parentSet.name}['{parentId}'] {label}";
        public   override   string      ToString() => DebugName;
        
        internal ReadRefTask(string parentId, EntitySet parentSet, string label, bool singleResult) {
            this.parentId           = parentId;
            this.parentSet          = parentSet;
            this.singleResult       = singleResult;
            this.label              = label;
        }
        
        protected Exception Error(string message) {
            return new TaskNotSyncedException($"{message} {DebugName}");
        }
    }
    
    public class ReadRefTask<T> : ReadRefTask where T : Entity
    {
        internal    string      id;
        internal    T           entity;
        internal    bool        synced;

        public      string      Id      => synced ? id      : throw Error("ReadRefTask.Id requires Sync().");
        public      T           Result  => synced ? entity  : throw Error("ReadRefTask.Result requires Sync().");

        internal ReadRefTask(string parentId, EntitySet parentSet, string label) : base (parentId, parentSet, label, true) { }
    }
    
    public class ReadRefsTask<T> : ReadRefTask where T : Entity
    {
        internal            bool                    synced;
        internal readonly   List<ReadRefTask<T>>    results = new List<ReadRefTask<T>>();
        
        public              List<ReadRefTask<T>>    Results         => synced ? results         : throw Error("ReadRefsTask.Results requires Sync().");
        public              ReadRefTask<T>          this[int index] => synced ? results[index]  : throw Error("ReadRefsTask[] requires Sync().");

        internal ReadRefsTask(string parentId, EntitySet parentSet, string label) : base (parentId, parentSet, label, false) { }
    }
    
    internal class ReadRefTaskMap
    {
        internal readonly   string                          selector;
        internal readonly   Type                            entityType;
        internal readonly   Dictionary<string, ReadRefTask> readRefs = new Dictionary<string, ReadRefTask>();
        
        internal ReadRefTaskMap(string selector, Type entityType) {
            this.selector = selector;
            this.entityType = entityType;
        }
    }
}
// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
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
    [CLSCompliant(true)]
    public sealed class DeleteTask<TKey, T> : SyncTask where T : class
    {
        private  readonly   Set<TKey, T>    set;
        internal readonly   List<TKey>      keys;
        
        [DebuggerBrowsable(Never)]
        internal            TaskState       state;
        internal override   TaskState       State       => state;
        public   override   string          Details     => $"DeleteTask<{typeof(T).Name}> (entities: {keys.Count})";
        internal override   TaskType        TaskType    => TaskType.delete;


        internal DeleteTask(List<TKey> ids, Set<TKey, T> set) : base(set) {
            this.set  = set;
            this.keys       = ids;
        }

        public void Add(TKey key) {
            keys.Add(key);
        }
        
        public void AddRange(List<TKey> keys) {
            this.keys.AddRange(keys);
        }
        
        public void AddRange(ICollection<TKey> keys) {
            this.keys.AddRange(keys);
        }

        protected internal override void Reuse() {
            keys.Clear();
            state       = default;
            taskName    = null;
            set.deleteBuffer.Add(this);
        }

        internal override SyncRequestTask CreateRequestTask(in CreateTaskContext context) {
            return set.DeleteEntities(this);
        }
    }
    
    public sealed class DeleteAllTask<TKey, T> : SyncTask where T : class
    {
        private  readonly   Set<TKey, T>    set;
        [DebuggerBrowsable(Never)]
        internal            TaskState       state;
        internal override   TaskState       State       => state;

        public   override   string          Details     => $"DeleteAllTask<{typeof(T).Name}>";
        internal override   TaskType        TaskType    => TaskType.delete;

        internal DeleteAllTask(Set<TKey, T> set) : base(set) {
            this.set = set;
        }
        
        internal override SyncRequestTask CreateRequestTask(in CreateTaskContext context) {
            return new DeleteEntities {
                container   = set.nameShort,
                all         = true,
                intern      = new SyncTaskIntern(this) 
            };
        }
    }

}
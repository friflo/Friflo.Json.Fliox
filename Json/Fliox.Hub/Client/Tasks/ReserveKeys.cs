// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    public sealed class ReserveKeysTask<TKey, T> : SyncTask where T : class
    {
        internal            int             count;
        internal            long[]          keys;
        internal            Guid            token;
        public              int             Count       => count;
        private readonly    Set<TKey,T>     set;
        
        [DebuggerBrowsable(Never)]
        internal            TaskState       state;
        
        internal override   TaskState       State       => state;
        public   override   string          Details     => $"ReserveKeysTask<{typeof(TKey).Name},{typeof(T).Name}>(count: {count})";
        internal override   TaskType        TaskType    => TaskType.reserveKeys;
        
        public              long[]          Keys        => IsOk("ReserveKeysTask.Keys", out Exception e) ? keys : throw e;
        
        internal ReserveKeysTask(int count, Set<TKey,T> set) : base(set) {
            this.count  = count;
            this.set    = set;
        }
        
        internal override SyncRequestTask CreateRequestTask(in CreateTaskContext context) {
            return set.ReserveKeys(this);
        }
    }
}
// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.DB.Graph.Internal;

namespace Friflo.Json.Fliox.DB.Graph
{
    public class ReserveKeysTask<TKey, T> : SyncTask where T : class
    {
        internal            int         count;
        internal            int         startKey;
        internal            Guid        token;
        public              int         Count       => count;
        
        internal            TaskState   state;
        internal override   TaskState   State       => state;
        
        public   override   string      Details     => $"ReserveKeysTask<{typeof(TKey).Name},{typeof(T).Name}>(count: {count})";
        
        public              int         StartKey    => IsOk("ReserveKeysTask.StartKey", out Exception e) ? startKey : throw e;
        
        internal ReserveKeysTask(int count) {
            this.count  = count;
        }
    }
}
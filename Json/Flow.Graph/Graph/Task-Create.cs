// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Graph.Internal;

namespace Friflo.Json.Flow.Graph
{
    public abstract class WriteTask : SyncTask {
        internal            TaskState   state;
        internal override   TaskState   State      => state;

        internal abstract void GetIds(List<string> ids);
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class CreateTask<T> : WriteTask where T : class
    {
        private readonly    EntitySet2<T>   set;
        private readonly    List<T>         entities;

        public   override   string          Details     => $"CreateTask<{typeof(T).Name}> (#ids: {entities.Count})";
        
        
        internal CreateTask(List<T> entities, EntitySet2<T> set) {
            this.set        = set;
            this.entities   = entities;
        }
        
        public void Add(T entity) {
            if (entity == null)
                throw new ArgumentException($"CreateTask<{set.name}>.Add() entity must not be null.");
            var peer = set.CreatePeer(entity);
            set.syncSet2.AddCreate(peer);
            entities.Add(entity);
        }
        
        public void AddRange(ICollection<T> entities) {
            int n = 0;
            foreach (var entity in entities) {
                if (entity == null)
                    throw new ArgumentException($"CreateTask<{set.name}>.AddRange() entities[{n}] must not be null.");
                n++;
                var peer = set.CreatePeer(entity);
                set.syncSet2.AddCreate(peer);
            }
            this.entities.AddRange(entities);
        }

        internal override void GetIds(List<string> ids) {
            foreach (var entity in entities) {
                var id = set.GetEntityId(entity);
                ids.Add(id);    
            }
        }
    }
}
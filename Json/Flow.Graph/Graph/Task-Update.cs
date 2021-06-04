// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Friflo.Json.Flow.Graph
{
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class UpdateTask<T> : WriteTask where T : Entity
    {
        private readonly    EntitySet<T>    set;
        private  readonly   List<T>         entities;

        public   override   string          Details     => $"UpdateTask<{typeof(T).Name}> (#ids: {entities.Count})";
        public   override   string          ToString()  => GetLabel();
        
        
        internal UpdateTask(List<T> entities, EntitySet<T> set) {
            this.set        = set;
            this.entities   = entities;
        }
        
        public void Add(T entity) {
            if (entity == null)
                throw new ArgumentException($"UpdateTask<{set.name}>.Add() entity must not be null.");
            var peer = set.CreatePeer(entity);
            set.sync.AddUpdate(peer);
            entities.Add(entity);
        }
        
        public void AddRange(ICollection<T> entities) {
            var n = 0;
            foreach (var entity in entities) {
                if (entity == null)
                    throw new ArgumentException($"UpdateTask<{set.name}>.AddRange() entities[{n}] must not be null.");
                n++;
                var peer = set.CreatePeer(entity);
                set.sync.AddUpdate(peer);
            }
            this.entities.AddRange(entities);
        }
        
        internal override void GetIds(List<string> ids) {
            foreach (var entity in entities) {
                ids.Add(entity.id);    
            }
        }
    }
}
// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.DB.Client.Internal;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Client
{
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class UpsertTask<T> : WriteTask where T : class
    {
        private readonly    EntitySetBase<T>    set;
        private  readonly   List<T>             entities;

        public   override   string              Details     => $"UpsertTask<{typeof(T).Name}> (#keys: {entities.Count})";
        
        
        internal UpsertTask(List<T> entities, EntitySetBase<T> set) {
            this.set        = set;
            this.entities   = entities;
        }
        
        public void Add(T entity) {
            if (entity == null)
                throw new ArgumentException($"UpsertTask<{set.name}>.Add() entity must not be null.");
            var peer = set.CreatePeer(entity);
            var syncSet = set.GetSyncSetBase();
            syncSet.AddUpsert(peer);
            entities.Add(entity);
        }
        
        public void AddRange(ICollection<T> entities) {
            var n = 0;
            var syncSet = set.GetSyncSetBase();
            foreach (var entity in entities) {
                if (entity == null)
                    throw new ArgumentException($"UpsertTask<{set.name}>.AddRange() entities[{n}] must not be null.");
                n++;
                var peer = set.CreatePeer(entity);
                syncSet.AddUpsert(peer);
            }
            this.entities.AddRange(entities);
        }
        
        internal override void GetIds(List<JsonKey> ids) {
            foreach (var entity in entities) {
                var id = set.GetEntityId(entity);
                ids.Add(id);
            }
        }
    }
}
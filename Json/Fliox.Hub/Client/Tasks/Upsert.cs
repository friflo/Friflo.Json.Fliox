// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class UpsertTask<T> : WriteTask<T> where T : class
    {
        private  readonly   EntitySetBase<T>    set;
        private  readonly   SyncSetBase<T>      syncSet;
        private  readonly   List<T>             entities;
        private             UpsertEntities      upsertEntities;

        public   override   string              Details     => $"UpsertTask<{typeof(T).Name}> (entities: {entities.Count})";
        internal override   TaskType            TaskType    => TaskType.upsert;
        
        
        internal UpsertTask(List<T> entities, EntitySetBase<T> set, SyncSetBase<T> syncSet) {
            this.set        = set;
            this.syncSet    = syncSet;
            this.entities   = entities;
        }
        
        public void Add(T entity) {
            if (entity == null)
                throw new ArgumentException($"UpsertTask<{set.name}>.Add() entity must not be null.");
            var peer = set.CreatePeer(entity);
            AddPeer(peer, PeerState.Upsert);
            entities.Add(entity);
        }
        
        public void AddRange(List<T> entities) {
            var n = 0;
            foreach (var entity in entities) {
                if (entity == null)
                    throw new ArgumentException($"UpsertTask<{set.name}>.AddRange() entities[{n}] must not be null.");
                n++;
                var peer = set.CreatePeer(entity);
                AddPeer(peer, PeerState.Upsert);
            }
            this.entities.AddRange(entities);
        }
        
        public void AddRange(ICollection<T> entities) {
            var n = 0;
            foreach (var entity in entities) {
                if (entity == null)
                    throw new ArgumentException($"UpsertTask<{set.name}>.AddRange() entities[{n}] must not be null.");
                n++;
                var peer = set.CreatePeer(entity);
                AddPeer(peer, PeerState.Upsert);
            }
            this.entities.AddRange(entities);
        }
        
        protected internal override void Reuse() {
            upsertEntities.entities.Clear();
            set.upsertEntitiesBuffer.Add(upsertEntities);
                
            entities.Clear();
            peers.Clear();
            state       = default;
            taskName    = null;
            set.upsertBuffer.Add(this);
        }
        
        internal override void GetIds(List<JsonKey> ids) {
            foreach (var entity in entities) {
                var id = set.GetEntityId(entity);
                ids.Add(id);
            }
        }
        
        internal override SyncRequestTask CreateRequestTask(in CreateTaskContext context) {
            return upsertEntities = syncSet.UpsertEntities(this, context);
        }

    }
}
// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    [CLSCompliant(true)]
    public sealed class UpsertTask<T> : WriteTask<T> where T : class
    {
        private  readonly   Set<T>              set;

        public   override   string              Details     => $"UpsertTask<{typeof(T).Name}> (entities: {entities.Count})";
        internal override   TaskType            TaskType    => TaskType.upsert;
        
        
        internal UpsertTask(Set<T> set) : base(set) {
            this.set        = set;
        }
        
        private void AddEntity (T entity) {
            var id = set.TrackEntity(entity, PeerState.Upsert);
            entities.Add(new KeyEntity<T>(id, entity));   // sole place an entity is added
        }
        
        public void Add(T entity) {
            if (entity == null) {
                throw new ArgumentException($"UpsertTask<{set.name}>.Add() entity must not be null.");
            }
            AddEntity(entity);
        }
        
        public void AddRange(List<T> entities) {
            var n = 0;
            foreach (var entity in entities) {
                if (entity == null) {
                    throw new ArgumentException($"UpsertTask<{set.name}>.AddRange() entities[{n}] must not be null.");
                }
                n++;
                AddEntity(entity);
            }
        }
        
        public void AddRange(ICollection<T> entities) {
            var n = 0;
            foreach (var entity in entities) {
                if (entity == null) {
                    throw new ArgumentException($"UpsertTask<{set.name}>.AddRange() entities[{n}] must not be null.");
                }
                n++;
                AddEntity(entity);
            }
        }
        
        protected internal override void Reuse() {
            entities.Clear();
            state       = default;
            taskName    = null;
            set.upsertBuffer.Add(this);
        }
        
        internal override SyncRequestTask CreateRequestTask(in CreateTaskContext context) {
            return set.UpsertEntities(this, context);
        }
    }
}
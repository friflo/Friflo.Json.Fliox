// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

// ReSharper disable JoinNullCheckWithUsage
namespace Friflo.Json.Flow.Graph.Internal
{

    internal class PeerEntity { }
    
    // --- PeerEntity<>
    internal class PeerEntity<T> : PeerEntity where T : Entity
    {
        internal  readonly  string          id;      // never null
        private   readonly  EntitySet<T>    set;
        private             T               entity;
        
        internal            bool            assigned;
        internal            bool            created;
        internal            bool            updated;

        internal            T               PatchSource     { get; private set; }
        internal            T               NextPatchSource { get; private set; }

        public   override   string          ToString() => id;
        
        internal PeerEntity(T entity) {
            if (entity == null)
                throw new NullReferenceException($"entity must not be null. Type: {typeof(T)}");
            this.entity = entity;
            this.id     = entity.id;
        }
        
        internal PeerEntity(string id, EntitySet<T> set) {
            if (id == null)
                throw new NullReferenceException($"id must not be null. Type: {typeof(T)}");
            this.id = id;
            this.set = set;
        }
        
        internal T Entity => entity;
        
        internal T GetEntity() {
            if (entity != null)
                return entity;
            entity = (T)set.intern.typeMapper.CreateInstance();
            entity.id = id;
            return entity;
        }

        public void SetEntity(T entity) {
            if (this.entity == null) {
                this.entity = entity;
                return;
            }
            if (this.entity != entity)
                throw new ArgumentException($"Another entity with same id is already tracked. id: {entity.id}");
        }

        internal void SetPatchSource(T entity) {
            if (entity == null)
                throw new InvalidOperationException("SetPatchSource() - expect entity not null");
            PatchSource = entity;
        }
        
        internal void SetPatchSourceNull() {
            PatchSource = null;
        }
        
        internal void SetNextPatchSource(T entity) {
            if (entity == null)
                throw new InvalidOperationException("SetNextPatchSource() - expect entity not null");
            NextPatchSource = entity;
        }
        
        internal void SetNextPatchSourceNull() {
            NextPatchSource = null;
        }
    }


}

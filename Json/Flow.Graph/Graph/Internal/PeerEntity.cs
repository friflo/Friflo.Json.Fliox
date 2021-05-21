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
        
        internal PeerEntity(string id) {
            if (id == null)
                throw new NullReferenceException($"id must not be null. Type: {typeof(T)}");
            this.id = id;
        }
        
        internal T Entity           => entity ?? throw new InvalidOperationException("Caller ensure & expect entity not null");
        internal T NullableEntity   => entity;
        
        internal void SetEntity(T entity) {
            if (entity == null)
                throw new InvalidOperationException("Expect entity not null");
            if (this.entity == null) {
                if (entity.id != id)
                    throw new InvalidOperationException("Expect entity.id == id");
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

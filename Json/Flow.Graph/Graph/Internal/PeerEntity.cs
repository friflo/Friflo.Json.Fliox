// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Sync;

// ReSharper disable JoinNullCheckWithUsage
namespace Friflo.Json.Flow.Graph.Internal
{

    internal class PeerEntity { }
    
    // --- PeerEntity<>
    internal class PeerEntity<T> : PeerEntity where T : Entity
    {
        internal  readonly  string          id;      // never null
        private             T               entity;
        internal            EntityError     error;
        
        internal            bool            assigned;
        internal            bool            created;
        internal            bool            updated;

        internal            T               PatchSource     { get; private set; }
        internal            T               NextPatchSource { get; private set; }

        public   override   string          ToString() => id;
        
        internal PeerEntity(T entity, string id) {
            if (entity == null)
                throw new NullReferenceException($"entity must not be null. Type: {typeof(T)}");
            this.entity = entity;
            this.id     = id;
        }
        
        internal PeerEntity(string id) {
            if (id == null)
                throw new NullReferenceException($"id must not be null. Type: {typeof(T)}");
            this.id = id;
        }
        
        /// Using the the unchecked <see cref="NullableEntity"/> must be an exception. Use <see cref="Entity"/> by default.
        internal T NullableEntity   => entity;
        internal T Entity           => entity ?? throw new InvalidOperationException($"Caller ensure & expect entity not null. id: '{id}'");
        
        internal void SetEntity(T entity) {
            if (entity == null)
                throw new InvalidOperationException("Expect entity not null");
            // if (entityId != id)
            //    throw new InvalidOperationException("Expect entity.id == id");
            if (this.entity == null) {
                this.entity = entity;
                return;
            }
            if (this.entity != entity)
                throw new ArgumentException($"Entity is already tracked by another instance. id: '{id}'");
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

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
        internal readonly   T               entity; // never null
        internal            bool            assigned;
        internal            bool            created;
        internal            bool            updated;

        internal            T               PatchSource     { get; private set; }
        internal            T               NextPatchSource { get; private set; }

        public   override   string          ToString() => entity.id ?? "null";

        internal PeerEntity(T entity) {
            if (entity == null)
                throw new NullReferenceException($"entity must not be null. Type: {typeof(T)}");
            this.entity = entity;
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

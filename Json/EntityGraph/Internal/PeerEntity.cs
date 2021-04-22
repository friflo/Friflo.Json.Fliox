// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

// ReSharper disable JoinNullCheckWithUsage
namespace Friflo.Json.EntityGraph.Internal
{
    // --- PeerEntity<>
    internal class PeerEntity<T>  where T : Entity
    {
        internal readonly   T               entity; // never null
        internal            bool            assigned;
        internal            ReadTask<T>     read;
        internal            CreateTask<T>   create;

        internal            T               PatchSource     { get; private set; }
        internal            T               NextPatchSource { get; private set; }

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

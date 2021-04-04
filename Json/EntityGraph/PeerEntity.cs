// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.EntityGraph
{
    // --- PeerEntity<>
    internal class PeerEntity<T>  where T : Entity
    {
        internal readonly   T                               entity;
        internal            T                               patchReference; 
        internal            T                               nextPatchReference; 
        internal            bool                            assigned;
        internal            Read<T>                         read;
        internal            Create<T>                       create;

        internal PeerEntity(T entity) {
            this.entity = entity;
        }
    }

    public class PeerNotAssignedException : Exception
    {
        public readonly Entity entity;
        
        public PeerNotAssignedException(Entity entity) : base ($"Entity: {entity.GetType().Name} id: {entity.id}") {
            this.entity = entity;
        }
    }
}

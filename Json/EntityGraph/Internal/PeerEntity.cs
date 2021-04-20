// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


namespace Friflo.Json.EntityGraph.Internal
{
    // --- PeerEntity<>
    internal class PeerEntity<T>  where T : Entity
    {
        internal readonly   T               entity;
        internal            T               patchSource; 
        internal            T               nextPatchReference; 
        internal            bool            assigned;
        internal            ReadTask<T>     read;
        internal            CreateTask<T>   create;

        internal PeerEntity(T entity) {
            this.entity = entity;
        }
    }


}

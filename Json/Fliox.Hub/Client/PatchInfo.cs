// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Transform;
using static System.Diagnostics.DebuggerBrowsableState;

namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// Contain the patches applied to the <see cref="Members"/> of an entity identified by its <see cref="Id"/>
    /// </summary>
    public readonly struct EntityPatchInfo {
        public              JsonKey                     Id          => entityPatch.id;
        public              IReadOnlyList<PatchReplace> Members     => GetMembers();
        
        [DebuggerBrowsable(Never)]
        internal  readonly  EntityPatch                 entityPatch;
        public    override  string                      ToString()  => entityPatch.id.AsString();

        internal EntityPatchInfo (EntityPatch entityPatch) {
            this.entityPatch = entityPatch;   
        }
        
        /// creation of new array is okay, as it is expected to be used mainly for debugging 
        private PatchReplace[]  GetMembers() {
            var patches = entityPatch.patches;
            var result = new PatchReplace[patches.Count];
            for (int n = 0; n < patches.Count; n++) {
                result[n] = (PatchReplace)patches[n];
            }
            return result;
        }
    }
}
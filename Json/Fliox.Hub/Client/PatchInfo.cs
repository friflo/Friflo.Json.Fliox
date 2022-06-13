// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Transform;
using static System.Diagnostics.DebuggerBrowsableState;

namespace Friflo.Json.Fliox.Hub.Client
{
    public readonly struct EntityPatchInfo
    {
        JsonKey                                         Id          => entityPatch.id;
        IReadOnlyList<PatchReplace>                     Members     => GetMembers(entityPatch);
        
        [DebuggerBrowsable(Never)] private  readonly    EntityPatch entityPatch;
        
        internal EntityPatchInfo (EntityPatch entityPatch) {
            this.entityPatch    = entityPatch;
        }
        
        /// creation of new array is okay, as it is expected to be used mainly for debugging 
        internal static PatchReplace[]  GetMembers(EntityPatch entityPatch) {
            var patches = entityPatch.patches;
            var result = new PatchReplace[patches.Count];
            for (int n = 0; n < patches.Count; n++) {
                result[n] = (PatchReplace)patches[n];
            }
            return result;
        }
    }

    /// <summary>
    /// Contain the patches applied to the <see cref="Members"/> of an entity identified by its <see cref="Id"/>
    /// </summary>
    public readonly struct EntityPatchInfo<T> where T : class {
        public              JsonKey                                 Id      => entityPatch.id;
        public              IReadOnlyList<PatchReplace>             Members => EntityPatchInfo.GetMembers(entityPatch);
        public              T                                       Entity  => entity;
        
        [DebuggerBrowsable(Never)] internal  readonly   EntityPatch entityPatch;
        [DebuggerBrowsable(Never)] private   readonly   T           entity;
        public    override  string                                  ToString()  => entityPatch.id.AsString();

        internal EntityPatchInfo (EntityPatch entityPatch, T entity) {
            this.entityPatch    = entityPatch;
            this.entity         = entity;
        }
    }
}
// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Mapper.Diff;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Mapper
{
    /// <summary> Defines how to process array (set) differences when creating a <see cref="DiffNode"/> tree </summary>
    public enum DiffKind
    {
        /// <summary>Add all array (set) element differences to the <see cref="DiffNode"/> tree </summary>
        DiffElements    = 1,
        /// <summary>Add only the first element difference of any array (set) to the <see cref="DiffNode"/> tree </summary>
        DiffArrays      = 2
    }
    
    [CLSCompliant(true)]
    public sealed class ObjectDiffer : IDisposable
    {
        private readonly Differ differ;
        
        public ObjectDiffer(TypeStore typeStore) {
            var typeCache = new TypeCache(typeStore);
            differ = new Differ(typeCache);
        }
        
        public void Dispose() {
            differ.Dispose();
        }

        public DiffNode GetDiff<T>(T left, T right, DiffKind kind) {
            return differ.GetDiff(left, right, kind);
        }
    }
}
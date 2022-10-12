// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Mapper.Diff;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Mapper
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
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

        public DiffNode GetDiff<T>(T left, T right) {
            return differ.GetDiff(left, right);
        }
    }
}
// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Mapper.Diff;

namespace Friflo.Json.Flow.Mapper
{
    public class ObjectDiffer : IDisposable
    {
        private readonly Differ differ;
        
        public ObjectDiffer(TypeStore typeStore) {
            differ = new Differ(typeStore);
        }
        
        public void Dispose() {
            differ.Dispose();
        }

        public DiffNode GetDiff<T>(T left, T right) {
            return differ.GetDiff(left, right);
        }
    }
}
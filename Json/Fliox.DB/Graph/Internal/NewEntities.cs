// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Friflo.Json.Fliox.DB.Graph.Internal
{
    public class EntityEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        public static readonly EntityEqualityComparer<T> Instance = new EntityEqualityComparer<T>();
        
        public bool Equals(T x, T y) {
            return x == y;
        }

        public int GetHashCode(T obj) {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
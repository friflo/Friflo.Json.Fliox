// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if UNITY_5_3_OR_NEWER

using System.Collections.Generic;

namespace Friflo.Json.Burst
{
    public static class UnityExtension
    {
        public static bool TryAdd<TKey,TValue>(this Dictionary<TKey,TValue> dictionary, TKey key, TValue value) {
            if (dictionary.ContainsKey(key))
                return false;
            
            dictionary.Add(key, value);
            return true;
        }
        
        public static HashSet<T> ToHashSet<T>(this ICollection<T> collection) {
            var hashSet = new HashSet<T>();
            foreach (var element in collection) {
                hashSet.Add(element);
            }
            return hashSet;
        }
    }
}
#endif

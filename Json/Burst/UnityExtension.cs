// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if UNITY_5_3_OR_NEWER

using System.Collections.Generic;

namespace System.Collections.Generic
{
    public static class UnityExtensionGeneric
    {
        public static bool TryAdd<TKey,TValue>(this IDictionary<TKey,TValue> dictionary, TKey key, TValue value) {
            if (dictionary.ContainsKey(key))
                return false;
            
            dictionary.Add(key, value);
            return true;
        }
    }
}

namespace System.Linq
{
    public static class UnityExtensionLinq
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> collection) {
            // return new HashSet<T>(collection, null); todo use this
            var hashSet = new HashSet<T>();
            foreach (var element in collection) {
                hashSet.Add(element);
            }
            return hashSet;
        }
    }
}
#endif

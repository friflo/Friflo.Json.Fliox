// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;

// ReSharper disable CheckNamespace
namespace System.Collections.Generic
{
    public static class Helper
    {
        public static HashSet<T> CreateHashSet<T>(int capacity) {
#if UNITY_5_3_OR_NEWER
            return new HashSet<T>();
#else
            return new HashSet<T>(capacity);
#endif
        }
        
        public static T First<T>(this HashSet<T> hashSet) {
            // ReSharper disable once GenericEnumeratorNotDisposed - HashSet<T>.Dispose() does nothing
            var enumerator = hashSet.GetEnumerator();
            enumerator.MoveNext();
            return enumerator.Current;
        }
    }
}

#if UNITY_5_3_OR_NEWER

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

        public static int EnsureCapacity<TKey,TValue>(this Dictionary<TKey,TValue> dictionary, int capacity) {
            return 0;
        }
    }
}

namespace System.Collections.Concurrent
{
    public static class UnityExtensionConcurrent
    {
        public static void Clear<T>(this ConcurrentQueue<T> queue) {
            while (queue.TryDequeue(out T _)) { }
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

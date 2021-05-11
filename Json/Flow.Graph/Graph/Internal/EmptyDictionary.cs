// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Friflo.Json.Flow.Graph.Internal
{
    public class EmptyDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
            throw new System.NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<TKey, TValue> item) {
            throw new InvalidOperationException($"EmptyDictionary<> is immutable. key: {item.Key}");
        }

        public void Clear() {
        }

        public bool Contains(KeyValuePair<TKey, TValue> item) {
            return false;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
            throw new InvalidOperationException("EmptyDictionary<> is immutable");
        }

        public bool Remove(KeyValuePair<TKey, TValue> item) {
            throw new InvalidOperationException($"EmptyDictionary<> is immutable. key: {item.Key}");
        }

        public int      Count       => 0;
        public bool     IsReadOnly  => true;
        
        public void Add(TKey key, TValue value) {
            throw new InvalidOperationException($"EmptyDictionary<> is immutable. key: {key}");
        }

        public bool ContainsKey(TKey key) {
            return false;
        }

        public bool Remove(TKey key) {
            throw new InvalidOperationException($"EmptyDictionary<> is immutable. key: {key}");
        }

        public bool TryGetValue(TKey key, out TValue value) {
            value = default;
            return false;
        }

        public TValue this[TKey key] {
            get => throw new KeyNotFoundException($"EmptyDictionary<> is always empty. key: {key}");
            set => throw new InvalidOperationException($"EmptyDictionary<> is immutable. key: {key}");
        }

        public ICollection<TKey>    Keys    => new List<TKey>();
        public ICollection<TValue>  Values  => new List<TValue>();
    }
}
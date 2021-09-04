// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Friflo.Json.Fliox.DB.Graph.Internal
{
    /// <summary>
    /// An immutable <see cref="IDictionary{TKey,TValue>"/> implementation containing no entries.
    /// Used for methods returning an empty dictionary instead of null reference to avoid null checks or throwing
    /// a <see cref="NullReferenceException"/> when accessing the return value. 
    /// </summary>
    public class EmptyDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        public  override    string  ToString() => "Count: 0";

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
            return new EmptyDictionaryIterator<KeyValuePair<TKey, TValue>>();
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
            // nothing there to copy
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

    public class EmptyDictionaryIterator<TValue> : IEnumerator<TValue>
    {
        public bool MoveNext() {
            return false;
        }

        public void Reset() { }

        public TValue Current => throw new InvalidOperationException("EmptyDictionary<> is always empty");

        object IEnumerator.Current => Current;

        public void Dispose() { }
    }
}
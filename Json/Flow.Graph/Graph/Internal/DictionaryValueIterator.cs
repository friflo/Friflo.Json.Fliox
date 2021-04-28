// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Friflo.Json.Flow.Graph.Internal
{
    public struct DictionaryValueIterator<TKey, TValue> : IEnumerator<TValue>
    {
        private Dictionary<TKey,TValue>.Enumerator iterator;
        
        public DictionaryValueIterator(Dictionary<TKey, TValue> map) {
            iterator = map.GetEnumerator();
        }
        
        public bool MoveNext() {
            return iterator.MoveNext();
        }

        public void Reset() {
            throw new NotImplementedException();
        }

        public TValue Current => iterator.Current.Value;

        object IEnumerator.Current => Current;

        public void Dispose() {
            iterator.Dispose();
        }
    }
}
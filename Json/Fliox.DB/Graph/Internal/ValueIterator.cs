// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Friflo.Json.Fliox.DB.Graph.Internal
{
    public struct ValueIterator<TKey, TValue> : IEnumerator<TValue>
    {
        private                 Dictionary<TKey,TValue>.Enumerator  iterator;
        private static readonly Dictionary<TKey, TValue>            EmptyMap = new Dictionary<TKey, TValue>();
        
        
        public ValueIterator(Dictionary<TKey, TValue> map) {
            if (map != null)
                iterator = map.GetEnumerator();
            else
                iterator = EmptyMap.GetEnumerator();
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
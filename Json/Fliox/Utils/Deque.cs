// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Utils
{
    public class Deque<T>
    {
        private int     first;
        private int     last;
        private int     capacity;
        private T[]     items;
        
        public  int     Count => last - first;
        
        public Deque() {
            capacity    = 4;
            items       = new T[capacity];
        }
        
        public Enumerator GetEnumerator() {
            return new Enumerator(this);
        }
        
        public void Clear() {
            for (int i = first; i < last; i++) {
                items[i] = default;
            }
            first   = 0;
            last    = 0;
        }

        public T RemoveHead() {
            if (first < last) {
                var result = items[first];
                items[first++] = default;
                return result;
            }
            return default;
        }
        
        public void AddTail(T item) {
            if (last < capacity) {
                items[last++] = item;
                return;
            }
            capacity       *= 2;
            var newItems    = new T[capacity];
            var count       = Count;
            for (int n = 0; n < count; n++) {
                newItems[n] = items[n + first];
            }
            items           = newItems;
            first           = 0;
            last            = count;
            items[last++]   = item;
        }
        
        // ---------------------------------------- Enumerator<T> ----------------------------------------
        public struct Enumerator
        {
            private readonly    T[]     items;
            private readonly    int     last;
            private             int     current;
            
            internal Enumerator (Deque<T> deque) {
                items   = deque.items;
                current = deque.first   - 1;
                last    = deque.last    - 1;
            }
            
            public bool MoveNext() {
                if (current < last) {
                    current++;
                    return true;
                }
                return false;
            }
        
            public T Current => current <= last ? items[current] : default;
        }
    }
}
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
        
        public  int     Count { get; private set; }

        public Deque(int capacity = 4) {
            this.capacity   = capacity;
            items           = new T[capacity];
        }
        
        public Enumerator GetEnumerator() {
            return new Enumerator(this);
        }
        
        public void Clear() {
            int i = first;
            while (i != last) {
                items[i] = default;
                i = (i + 1) % capacity;
            }
            Count   = 0;
            first   = 0;
            last    = 0;
        }

        public T RemoveHead() {
            if (Count > 0) {
                var result      = items[first];
                items[first]    = default;
                first           = (first + 1) % capacity;
                Count--;
                return result;
            }
            return default;
        }
        
        public void AddHead(T item) {
            var count = Count++;
            if (count < capacity) {
                first = (first + capacity - 1) % capacity;
                items[first] = item;
                return;
            }
            var newItems    = new T[2 * capacity];
            for (int n = 0; n < count; n++) {
                var index = (n + first) % capacity;
                newItems[n + 1] = items[index];
            }
            capacity        = newItems.Length;
            items           = newItems;
            first           = 0;
            items[0]        = item;
            last            = count;
        }
        
        public void AddTail(T item) {
            var count = Count++;
            if (count < capacity) {
                items[last] = item;
                last = (last + 1) % capacity;
                return;
            }
            var newItems    = new T[2 * capacity];
            for (int n = 0; n < count; n++) {
                var index = (n + first) % capacity;
                newItems[n] = items[index];
            }
            capacity        = newItems.Length;
            items           = newItems;
            first           = 0;
            last            = count;
            items[last]     = item;
        }
        
        // ---------------------------------------- Enumerator<T> ----------------------------------------
        public struct Enumerator
        {
            private readonly    T[]     items;
            private readonly    int     capacity;
            private             int     remaining;
            private             int     next;
            private             int     current;
            
            internal Enumerator (Deque<T> deque) {
                items       = deque.items;
                capacity    = deque.capacity;
                remaining   = deque.Count;
                next        = deque.first;
                current     = -1;
            }
            
            public bool MoveNext() {
                if (remaining > 0) {
                    current = next;
                    next    = (next + 1) % capacity;
                    remaining--;
                    return true;
                }
                return false;
            }
        
            public T Current => current != -1 ? items[current] : default;
        }
    }
}
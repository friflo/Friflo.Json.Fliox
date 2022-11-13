// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Fliox.Utils
{
    public class Deque<T>
    {
        private int     first;
        private int     capacity;
        private T[]     array;
        
        public  int     Count { get; private set; }
        private T[]     Items => ToArray();

        public Deque(int capacity = 4) {
            this.capacity   = capacity;
            array           = new T[capacity];
        }
        
        public T[] ToArray() {
            var count   = Count;
            var result  = new T[count];
            for (int n = 0; n < count; n++) {
                var index   = (first + n) % capacity;
                result[n]   = array[index];
            }
            return result;
        }
        
        public Enumerator GetEnumerator() {
            return new Enumerator(this);
        }
        
        public void Clear() {
            int i       = first;
            var count   = Count;
            for (int n = 0; n < count; n++) {
                array[i] = default;
                i = (i + 1) % capacity;
            }
            Count   = 0;
            first   = 0;
        }

        public T RemoveHead() {
            if (Count > 0) {
                var result      = array[first];
                array[first]    = default;
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
                array[first] = item;
                return;
            }
            var newItems    = new T[2 * capacity];
            for (int n = 0; n < count; n++) {
                var index = (n + first) % capacity;
                newItems[n + 1] = array[index];
            }
            capacity        = newItems.Length;
            array           = newItems;
            first           = 0;
            array[0]        = item;
        }
        
        public void AddTail(T item) {
            var count   = Count++;
            if (count < capacity) {
                var last    = (first + count) % capacity;
                array[last] = item;
                return;
            }
            var newItems    = new T[2 * capacity];
            for (int n = 0; n < count; n++) {
                var index = (n + first) % capacity;
                newItems[n] = array[index];
            }
            capacity        = newItems.Length;
            array           = newItems;
            first           = 0;
            array[count]    = item;
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
                items       = deque.array;
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
                current = -1;
                return false;
            }
        
            public T Current => current != -1 ? items[current] : throw new InvalidOperationException("invalid enumerator");
        }
    }
}
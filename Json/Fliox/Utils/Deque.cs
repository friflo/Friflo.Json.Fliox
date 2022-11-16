// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
namespace Friflo.Json.Fliox.Utils
{
    public class Deque<T>
    {
        private int     count;
        private int     first;
        private int     capacity;
        private T[]     array;
        
        public  int     Count => count;
        private T[]     Items => ToArray();

        public Deque(int capacity = 4) {
            this.capacity   = capacity;
            array           = new T[capacity];
        }
        
        public T[] ToArray() {
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
            int i   = first;
            for (int n = 0; n < count; n++) {
                array[i]    = default;
                i           = (i + 1) % capacity;
            }
            count   = 0;
            first   = 0;
        }

        public T RemoveHead() {
            if (count > 0) {
                var result      = array[first];
                array[first]    = default;
                first           = (first + 1) % capacity;
                count--;
                return result;
            }
            throw new InvalidOperationException("Expect Deque not empty");
        }
        
        public void AddHead(T item) {
            if (count == capacity) {
                Resize(2 * capacity);
            }
            first = (first + capacity - 1) % capacity;
            array[first] = item;
            count++;
        }
        
        public void AddHeadQueue(Queue<T> queue) {
            var newCount    = count + queue.Count;
            if (newCount > capacity) {
                Resize(newCount);
            }
            int index = first = (first + capacity - queue.Count) % capacity;
            foreach (var item in queue) {
                array[index]    = item;
                index           = (index + 1) % capacity;
            }
            count = newCount;
        }
        
        public void AddTail(T item) {
            if (Count == capacity) {
                Resize(2 * capacity);
            }
            var last    = (first + count++) % capacity;
            array[last] = item;
        }
        
        private void Resize(int newCapacity) {
            var newItems = new T[newCapacity];
            for (int n = 0; n < count; n++) {
                var index   = (n + first) % capacity;
                newItems[n] = array[index];
            }
            capacity    = newCapacity;
            array       = newItems;
            first       = 0;
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
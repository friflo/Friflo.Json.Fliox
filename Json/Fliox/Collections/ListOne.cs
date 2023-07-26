// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

// ReSharper disable ConvertToAutoProperty
namespace Friflo.Json.Fliox.Collections
{
    public class ListOne<T> : IList<T>, IList, IReadOnlyList<T>
    {
        private int     count;
        private T[]     items;
        private T       single;

        public  int     Capacity        => items?.Length ?? 1;

        // --- IList, ICollection, IReadOnlyCollection
        public  int     Count           => count;
        public  bool    IsSynchronized  => false;
        public  object  SyncRoot        => this;
        public  bool    IsFixedSize     => false;
        public  bool    IsReadOnly      => false;
        
        public          Enumerator      GetEnumerator() => new Enumerator(this);
        IEnumerator<T>  IEnumerable<T>. GetEnumerator() => new Enumerator(this);
        IEnumerator     IEnumerable.    GetEnumerator() => new Enumerator(this);
        
        public ListOne() { }
        public ListOne(int capacity)
        {
            switch (capacity) {
                case 0:
                case 1: return;
            }
            items = new T[capacity];
        }

        public void Add(T item)
        {
            switch (count) {
                case 0:
                    single      = item;
                    count       = 1;
                    return;
                case 1:
                    EnsureCapacity(4);
                    items[0]    = single;
                    items[1]    = item;
                    count       = 2;
                    return;
                default:
                    if (count < items.Length) {
                        items[count++] = item;
                        return;
                    }
                    EnsureCapacity(count + 1);
                    items[count++] = item;
                    return;
            }
        }

        private void EnsureCapacity(int capacity) {
            if (items == null) {
                var newCapacity = Math.Max(capacity, 4);
                items           = new T[newCapacity];
                return;
            }
            if (capacity <= items.Length) {
                return;
            }
            var doubleLen   = 2 * items.Length;
            var newLen      = Math.Max(doubleLen, capacity);
            var newItems    = new T[newLen];
            Array.Copy(items, 0, newItems, 0, count);
            items = newItems;
        }

        public int Add(object value) {
            throw new NotImplementedException();
        }

        public void Clear() {
            switch (count) {
                case 0:
                    return;
                case 1:
                    single = default;
                    break;
                default:
                    Array.Clear(items, 0, count); // enable GC collect unused items
                    break;
            }
            count = 0;
        }

        // --- IList<>
        public void RemoveAt(int index) {
            throw new NotImplementedException();
        }
        
        // --- IList
        public bool Contains(object value) {
            throw new NotImplementedException();
        }

        public int IndexOf(object value) {
            throw new NotImplementedException();
        }

        public void Insert(int index, object value) {
            throw new NotImplementedException();
        }

        public void Remove(object value) {
            throw new NotImplementedException();
        }

        object IList.this[int index] {
            get => items[index];
            set => items[index] = (T)value;
        }
        
        public T this[int index] {
            get {
                switch (count) {
                    case 0:
                        throw new ArgumentOutOfRangeException();
                    case 1:
                        if (index == 0) {
                            return single;
                        }
                        throw new ArgumentOutOfRangeException();
                }
                if ((uint)index < (uint)count) {
                    return items[index];
                }
                throw new ArgumentOutOfRangeException();
            }
            set {
                switch (count) {
                    case 0:
                        throw new ArgumentOutOfRangeException();
                    case 1:
                        if (index == 0) {
                            single = value;
                            return;
                        }
                        throw new ArgumentOutOfRangeException();
                }
                if ((uint)index < (uint)count) {
                    items[index] = value;
                }
                throw new ArgumentOutOfRangeException();
            }
        }

        public bool Contains(T item) {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex) {
            throw new NotImplementedException();
        }

        public bool Remove(T item) {
            throw new NotImplementedException();
        }

        public void CopyTo(Array array, int index) {
            throw new NotImplementedException();
        }

        public int IndexOf(T item) {
            throw new NotImplementedException();
        }

        public void Insert(int index, T item) {
            throw new NotImplementedException();
        }
        
        
        // ----------------------------------------- Enumerator -----------------------------------------
        public struct Enumerator : IEnumerator<T>
        {
            private readonly    ListOne<T>  list;
            private             int         index;
            private             T           current;

            internal Enumerator(ListOne<T> list)
            {
                this.list   = list;
                index       = 0;
                current     = default;
            }

            public void Dispose() { }

            public bool MoveNext()
            {
                ListOne<T> localList = list;    
                if ((uint)index < (uint)localList.count)
                {
                    current = localList.items[index];
                    index++;
                    return true;
                }
                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                index = list.count + 1;
                current = default;
                return false;
            }

            public T Current => current!;

            object IEnumerator.Current
            {
                get
                {
                    if (index == 0 || index == list.count + 1) {
                        throw new InvalidOperationException("unexpected state");
                    }
                    return Current;
                }
            }

            void IEnumerator.Reset()
            {
                index = 0;
                current = default;
            }
        }
    }
}
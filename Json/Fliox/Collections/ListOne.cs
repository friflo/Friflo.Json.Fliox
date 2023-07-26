// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
// ReSharper disable ConvertToAutoProperty
namespace Friflo.Json.Fliox.Collections
{
    /// <summary>
    /// Container implementation aligned to <see cref="List{T}"/> with focus on minimizing heap allocations<br/>
    /// - Implemented as struct to avoid allocation for the container itself<br/>
    /// - Optimized for typical use-cases storing only a single item. No heap allocation if <see cref="Count"/> == 1. 
    /// </summary>
    public struct ListOne<T> : IList<T>, IReadOnlyList<T> // intentionally not implemented IList
    {
        [Browse(Never)] private     int     count;
                        private     T       single;
                        private     T[]     items;

        public override             string  ToString() => "Count: " + count;

        // --- ICollection<>, IReadOnlyCollection<>
                        public      int     Count           => count;
        [Browse(Never)] public      bool    IsReadOnly      => false;
        
        // public ListOne() {}
        
        public ListOne(int capacity)
        {
            count   = 0;
            single  = default;
            if (capacity == 0 || capacity == 1) {
                items = null;
                return;
            }
            items   = new T[capacity];
        }

        // --- ICollection<>
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
                    single      = default;
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
        
        public bool Contains(T item) {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex) {
            for ( int n= 0; n < count; n++) {
                array[n + arrayIndex] = this[n];
            }
        }

        public bool Remove(T item) {
            throw new NotImplementedException();
        }

        // --- IList<>
        public void RemoveAt(int index) {
            throw new NotImplementedException();
        }
        
        public T this[int index] {
            get {
                switch (count) {
                    case 0:
                        throw new IndexOutOfRangeException();
                    case 1:
                        if (index == 0) {
                            return single;
                        }
                        throw new IndexOutOfRangeException();
                }
                if ((uint)index < (uint)count) {
                    return items[index];
                }
                throw new IndexOutOfRangeException();
            }
            set {
                switch (count) {
                    case 0:
                        throw new ArgumentOutOfRangeException(nameof(index));
                    case 1:
                        if (index == 0) {
                            single = value;
                            return;
                        }
                        throw new ArgumentOutOfRangeException(nameof(index));
                }
                if ((uint)index < (uint)count) {
                    items[index] = value;
                }
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
        
        public int IndexOf(T item) {
            throw new NotImplementedException();
        }

        public void Insert(int index, T item) {
            throw new NotImplementedException();
        }

        // --- List<> aligned methods 
        public int Capacity {
            get => items?.Length ?? 1;
            set {
                if (value < count) {
                    throw new ArgumentOutOfRangeException(nameof(Capacity));
                }
                EnsureCapacity(value);
            }
        }

        public void RemoveRange(int index, int count)
        {
            if (index < 0)                  throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0)                  throw new ArgumentOutOfRangeException(nameof(count));
            if (this.count - index < count) throw new ArgumentException(nameof(index));

            if (count == 0) {
                return;
            }
            this.count -= count;
            if (index < this.count) {
                Array.Copy(items, index + count, items, index, this.count - index);
            }
            if (items != null) {
                Array.Clear(items, this.count, count);
            }
            if (this.count == 1) {
                single   = items[0];
                items[0] = default;
            }
        }
        
        // --- Span<>, ReadOnlySpan<>
        public ReadOnlySpan<T> GetReadOnlySpan()
        {
            switch (count) {
                case 0: return new ReadOnlySpan<T>(null);
#if NETSTANDARD2_0
                case 1: return Burst.Utils.UnsafeUtils.CreateReadOnlySpan(ref single);
#else
                case 1: return MemoryMarshal.CreateReadOnlySpan(ref single, 1);
#endif
            }
            return new ReadOnlySpan<T>(items, 0, count);
        }
        
        public Span<T> GetSpan()
        {
            switch (count) {
                case 0: return new Span<T>(null);
#if NETSTANDARD2_0
                case 1: return Burst.Utils.UnsafeUtils.CreateSpan(ref single);
#else
                case 1: return MemoryMarshal.CreateSpan(ref single, 1);
#endif
            }
            return new Span<T>(items, 0, count);
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
        
        // ----------------------------------------- Enumerator -----------------------------------------
        public  Enumerator                      GetEnumerator() => new Enumerator(this);
                IEnumerator<T>  IEnumerable<T>. GetEnumerator() => new Enumerator(this);
                IEnumerator     IEnumerable.    GetEnumerator() => new Enumerator(this);
        
        public struct Enumerator : IEnumerator<T>
        {
            private readonly    ListOne<T>  list;
            private             int         index;
            private             T           current;

            internal Enumerator(in ListOne<T> list) {
                this.list   = list;
                index       = 0;
                current     = default;
            }

            public void Dispose() { }

            public bool MoveNext()
            {
                if ((uint)index < (uint)list.count)
                {
                    current = list[index++];
                    return true;
                }
                current = default;
                return false;
            }

            public T Current => current!;

            object IEnumerator.Current {
                get
                {
                    if (index == 0 || index == list.count + 1) {
                        throw new InvalidOperationException("unexpected state");
                    }
                    return Current;
                }
            }

            void IEnumerator.Reset() {
                index   = 0;
                current = default;
            }
        }
    }
}
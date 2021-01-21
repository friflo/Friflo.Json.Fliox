// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

namespace Friflo.Json.Burst.Utils
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public struct ValueList<T> : IDisposable where T : struct
    {
#if JSON_BURST
        public Unity.Collections.NativeList<T> array;

        public ValueList(int size,  AllocType allocType) {
            var allocator = AllocUtils.AsAllocator(allocType);
            array = new Unity.Collections.NativeList<T>(size, allocator);
        }

        // public int Length => array.Length;
        public int Count => array.Length;

        public void Clear() {
            array.Clear();
        }

        public ref T ElementAt(int index) {
            return ref array.ElementAt(index);
        }

        public void Resize(int size) {
            array.Resize(size, Unity.Collections.NativeArrayOptions.ClearMemory);
        }

        public void Add(T value) {
            array.Add(value);
        }

        public void RemoveAt(int index) {
            array.RemoveAt(index);
        }
        
        public void Dispose() {
            array.Dispose();
        }

        public bool IsCreated() {
            return array.IsCreated;
        }
        
#else // MANAGED
        public T[] array;
        private int len;

        // ReSharper disable once UnusedParameter.Local
        public ValueList(int size, AllocType allocType) {
            array = new T[size];
            len = 0;
            DebugUtils.TrackAllocation(array);
        }
 
        // public int Length => len;
        public int Count => len;
        
        public void Clear() {
            len = 0;
        }

        public ref T ElementAt(int index) {
            return ref array[index];
        }
        
        public void Resize(int size) {
            EnsureCapacityAbs(size);
            len = size;
        }

        public void Add(T value) {
            EnsureCapacity(1);
            array[len++] = value;
        }
        
        // untested
        public void RemoveAt(int index) {
            Buffer.BlockCopy (array, index + 1, array, index, --len - index);
        }

        private void EnsureCapacity(int additionalCount) {
            EnsureCapacityAbs(len + additionalCount);
        }
        
        private void EnsureCapacityAbs(int size) {
            if (size <= array.Length)
                return;
            T[] newArr = new T[size];
            // Buffer.BlockCopy (array, 0, newArr, 0, len);
            for (int i = 0; i < len; i++)
                newArr[i] = array[i];
            DebugUtils.UntrackAllocation(array);
            DebugUtils.TrackAllocation(newArr);
            array = newArr;
        }

        public void Dispose() {
            if (array == null)
                throw new InvalidOperationException("Friflo.Json.Burst.Utils.ValueList has been disposed. Mimic NativeArray behavior");
            DebugUtils.UntrackAllocation(array);
            array = null;
        }
        
        public bool IsCreated() {
            return array != null;
        }
#endif
    }
}
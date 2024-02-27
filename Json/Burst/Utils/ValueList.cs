// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

namespace Friflo.Json.Burst.Utils
{
    // managed version does not have the constraint: where T : struct
    // JSON_BURST_TAG - was used for JSON_BURST to implement a ValueList<> with a Unity.Collections.NativeList<T>
    public struct ValueList<T> : IDisposable 
    {
        public T[] array;
        private int len;

        // ReSharper disable once UnusedParameter.Local
        public ValueList(int size, AllocType allocType) {
            array = new T[size];
            len = 0;
            DebugUtils.TrackAllocationObsolete(array);
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
            DebugUtils.UntrackAllocationObsolete(array);
            DebugUtils.TrackAllocationObsolete(newArr);
            array = newArr;
        }

        public void Dispose() {
            if (array == null)
                throw new InvalidOperationException("Friflo.Json.Burst.Utils.ValueList has been disposed. Mimic NativeArray behavior");
            DebugUtils.UntrackAllocationObsolete(array);
            array = null;
        }
        
        public bool IsCreated() {
            return array != null;
        }
    }
}
// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

namespace Friflo.Json.Burst.Utils
{
    public struct ByteList : IDisposable
    {
#if JSON_BURST
        public Unity.Collections.NativeList<byte> array;

        public ByteList(int size,  AllocType allocType) {
            var allocator = AllocUtils.AsAllocator(allocType);
            array = new Unity.Collections.NativeList<byte>(size, allocator);
        }
        
        /* public byte this[int index]
        {
            get {
                return array[index];
            }
            set {
                array[index] = value;
            }
        } */

        // public int Length => len;
        public int Count => array.Length;

        public void Resize(int size) {
            array.Resize(size, Unity.Collections.NativeArrayOptions.ClearMemory);
        }
        
        public void Dispose() {
            array.Dispose();
        }

        public bool IsCreated() {
            return array.IsCreated;
        }
        
#else // MANAGED
        public byte[] array;

        public ByteList(int size, AllocType allocType) {
            array = new byte[size];
            DebugUtils.TrackAllocation(array);
        }
        
 /*       public byte this[int index]
        {
            get {
                return array[index];
            }
            set {
                array[index] = value;
            }
        } */
 
        // public int Length => len;
        public int Count => array.Length;

        public void Resize(int size) {
            byte[] newArr = new byte[size];
            int len = size < array.Length ? size : array.Length;
            Buffer.BlockCopy (array, 0, newArr, 0, len);
            //  for (int i = 0; i < len; i++)
            //      newArr[i] = array[i];
            DebugUtils.UntrackAllocation(array);
            DebugUtils.TrackAllocation(newArr);
            array = newArr;
        }

        public void Dispose() {
            if (array == null)
                throw new InvalidOperationException("Friflo.Json.Burst.Utils.ByteList has been disposed. Mimic NativeArray behavior");
            DebugUtils.UntrackAllocation(array);
            array = null;
        }
        
        public bool IsCreated() {
            return array != null;
        }
#endif
    }
}
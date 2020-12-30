// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

namespace Friflo.Json.Burst.Utils
{
    public struct ByteArray : IDisposable
    {
#if JSON_BURST
        public Unity.Collections.NativeArray<byte> array;

        public ByteArray(int size) {
            array = new Unity.Collections.NativeArray<byte>(size, Unity.Collections.Allocator.Persistent);
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

        public int Length {
            get { return array.Length; }
        }
        
        public void Dispose() {
            array.Dispose();
        }

        public bool IsCreated() {
            return array.IsCreated;
        }
        
#else // MANAGED
        public byte[] array;

        public ByteArray(int size) {
            array = new byte[size];
            DebugUtils.AcquireAllocation(array);
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

        public int Length {
            get { return array.Length; }
        }

        public void Dispose() {
            if (array == null)
                throw new InvalidOperationException("Friflo.Json.Burst.Utils.ByteArray has been disposed. Mimic NativeArray behavior");
            DebugUtils.ReleaseAllocation(array);
            array = null;
        }
        
        public bool IsCreated() {
            return array != null;
        }
#endif
    }
}
// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Mapper.Utils
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public static class Arrays
    {
        public static Array CreateInstance (Type componentType, int length)
        {
            return Array. CreateInstance (componentType, length);
        }

        public static void ToBytes (ref Bytes dst, byte[] src) {
            dst.EnsureCapacityAbs(src.Length);
            dst.start = 0;
            dst.end = src.Length;
            dst.hc = 0;
#if JSON_BURST
            /* unsafe {
                void* dstPtr = Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(array.array);
                fixed (byte* srcPtr = src) {
                    Buffer.MemoryCopy(srcPtr, dstPtr, array.Length, src.Length);
                }
            } */
            dst.buffer.array.CopyFrom(src);
#else
            Buffer.BlockCopy (src, 0, dst.buffer.array, 0, src.Length);
#endif
        }
        
        public static void ToManagedArray(byte[] dst, Bytes src) {
            if (dst.Length < src.Len)
                throw new IndexOutOfRangeException();
#if JSON_BURST
            for (int i = 0; i < src.Len; i++)
                dst[i] = src.buffer.array[src.start + i];
#else
            Buffer.BlockCopy (src.buffer.array, src.start, dst, 0, src.Len);
#endif
        }
    }
}

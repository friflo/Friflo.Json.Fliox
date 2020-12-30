// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst.Utils;

namespace Friflo.Json.Managed.Utils
{
	public static class Arrays
	{
		static public T[] CopyOf <T> (T[] src, int length)
		{
			T[] dst = (T[])Array.CreateInstance(typeof (T), length);
            int min = Math.Min (length, src. Length);
			Array.Copy(src, dst, min);
			return dst;
		}

		static public Array CopyOfType (Type type, Array src, int length)
		{
			Array dst = Array.CreateInstance(type, length);
            int min = Math.Min (length, src. Length);
			Array.Copy(src, dst, min);
			return dst;
		}

	    static public Array CreateInstance (Type componentType, int length)
	    {
		    return Array. CreateInstance (componentType, length);
	    }

	    static public ByteArray CopyFrom(byte[] src) {
		    ByteArray array = new ByteArray(src.Length);
#if JSON_BURST
		    /* unsafe {
			    void* dstPtr = Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(array.array);
			    fixed (byte* srcPtr = src) {
				    Buffer.MemoryCopy(srcPtr, dstPtr, array.Length, src.Length);
			    }
		    } */
		    array.array.CopyFrom(src);
#else
		    Buffer.BlockCopy (src, 0, array.array, 0, src.Length);
#endif
		    return array;
	    }
	}
}

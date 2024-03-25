// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Mapper.Utils
{
    [CLSCompliant(true)]
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
            Buffer.BlockCopy (src, 0, dst.buffer, 0, src.Length);
        }
        
        public static void ToManagedArray(byte[] dst, Bytes src) {
            if (dst.Length < src.Len) throw new IndexOutOfRangeException();
            Buffer.BlockCopy (src.buffer, src.start, dst, 0, src.Len);
        }
    }
}

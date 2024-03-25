// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if UNITY_BURST

using Unity.Burst.Intrinsics;

namespace Friflo.Json.Burst.Vector
{
    /// <summary>
    /// Vector (SIMD) operations for Unity CLR
    /// UNITY_BURST requires <br/>
    /// - adding entry "Unity.Burst" in Friflo.Json.asmdef > "references" <br/>
    /// - installing package 'Burst' in Package Explorer <br/>
    /// </summary>
    public sealed class VectorOpsUnity : VectorOps
    {
        public override unsafe void Xor(
            byte[] dest,    int destPos,
            byte[] src,     int srcPos,
            byte[] mask,    int maskPos,
            int length)
        {
            int     n = 0;
            const   int vectorSize = 16; // 128 bit
            fixed (byte* destPointer  = dest)
            fixed (byte* srcPointer   = src)
            fixed (byte* maskPointer  = mask)
            {
                var end = length - vectorSize;
                for (; n <= end; n += vectorSize) {
                    var bufferVector        = X86.Sse2.load_si128(srcPointer   + srcPos + n);
                    var maskingKeyVector    = X86.Sse2.load_si128(maskPointer  + (maskPos + n) % 4);
                    var xor                 = X86.Sse2.xor_si128(bufferVector, maskingKeyVector);
                    X86.Sse2.store_si128(destPointer + destPos + n, xor);
                }
            }
            // remaining bytes
            base.Xor(dest, destPos + n, src, srcPos + n, mask, maskPos + n, length - n);
        }

        public override void Populate(byte[] arr) {
            PopulateVector(arr);
        }
    }
}

#endif
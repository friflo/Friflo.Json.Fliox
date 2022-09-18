// Copyright (c) Ullrich Praetz. All rights reserved.
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
    public class VectorOpsUnity : VectorOps
    {
        public override unsafe void MaskPayload(
            byte[] dest,    int destPos,
            byte[] src,     int srcPos,
            byte[] mask,    int maskPos,
            int length)
        {
            const int vectorSize = 16; // 128 bit
            fixed (byte* destPointer  = dest)
            fixed (byte* srcPointer   = src)
            fixed (byte* maskPointer  = mask)
            {
                for (int n = 0; n < length; n += vectorSize) {
                    var bufferVector        = X86.Sse2.load_si128(srcPointer   + srcPos + n);
                    var maskingKeyVector    = X86.Sse2.load_si128(maskPointer  + (maskPos + n) % 4);
                    var xor                 = X86.Sse2.xor_si128(bufferVector, maskingKeyVector);
                    X86.Sse2.store_si128(destPointer + destPos + n, xor);
                }
            }
        }

        public override void Populate(byte[] arr) {
            PopulateVector(arr);
        }
    }
}

#endif
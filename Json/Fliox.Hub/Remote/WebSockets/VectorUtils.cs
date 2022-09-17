// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if UNITY_BURST
    using Unity.Burst.Intrinsics;
#elif NETCOREAPP3_0_OR_GREATER
    using System.Runtime.Intrinsics.X86;
#endif

namespace Friflo.Json.Fliox.Hub.Remote.WebSockets
{
    /// <summary>
    /// UNITY_BURST requires <br/>
    /// - adding entry "Unity.Burst" in Friflo.Json.asmdef > "references" <br/>
    /// - installing package 'Burst' in Package Explorer <br/>
    /// </summary>
    public static class VectorUtils
    {
#if UNITY_BURST || NETCOREAPP3_0_OR_GREATER
        private static readonly bool UseSse = false;
#endif

        internal static void MaskPayload(
            byte[] dest,    int destPos,
            byte[] src,     int srcPos,
            byte[] mask,    int maskPos,
            int length)
        {

            if (UseSse) {
                // --- SIMD
                unsafe {
                    const int vectorSize = 16; // 128 bit
                    fixed (byte* destPointer  = dest)
                    fixed (byte* srcPointer   = src)
                    fixed (byte* maskPointer  = mask)
                    {
                        for (int n = 0; n < length; n += vectorSize) {
#if UNITY_BURST
                            var bufferVector        = X86.Sse2.load_si128(srcPointer   + srcPos + n);
                            var maskingKeyVector    = X86.Sse2.load_si128(maskPointer  + (maskPos + n) % 4);
                            var xor                 = X86.Sse2.xor_si128(bufferVector, maskingKeyVector);
                            X86.Sse2.store_si128(destPointer + destPos + n, xor);
#elif NETCOREAPP3_0_OR_GREATER
                            var bufferVector        = Sse2.LoadVector128(srcPointer   + srcPos + n);
                            var maskingKeyVector    = Sse2.LoadVector128(maskPointer  + (maskPos + n) % 4);
                            var xor                 = Sse2.Xor(bufferVector, maskingKeyVector);
                            Sse2.Store(destPointer + destPos + n, xor);
#endif
                        }
                    }
                }
                return;
            }
            // --- SISD
            for (int n = 0; n < length; n++) {
                var b = src[srcPos + n];
                dest[destPos + n] = (byte)(b ^ mask[(maskPos + n) % 4]);
            }
        }
        
        internal static void Populate(byte[] arr) {
#if UNITY_BURST || NETCOREAPP3_0_OR_GREATER
            if (!UseSse)
                return;
            arr[4] = arr [8] = arr[12] = arr[16] = arr[0];
            arr[5] = arr [9] = arr[13] = arr[17] = arr[1];
            arr[6] = arr[10] = arr[14] = arr[18] = arr[2];
            arr[7] = arr[11] = arr[15] = arr[19] = arr[3];
#endif
        }
    }
}
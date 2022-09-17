using System.Runtime.Intrinsics.X86;

namespace Friflo.Json.Fliox.Hub.Remote.WebSockets
{
    public static class VectorUtils
    {
        private static readonly bool UseSse = false;
        
        internal static void MaskPayload(
            byte[] dest,    int destPos,
            byte[] src,     int srcPos,
            byte[] mask,    int maskPos,
            int length)
        {
            if (UseSse) {
                unsafe {
                    const int vectorSize = 16; // 128 bit
                    fixed (byte* destPointer  = dest)
                    fixed (byte* srcPointer   = src)
                    fixed (byte* maskPointer  = mask)
                    {
                        for (int n = 0; n < length; n += vectorSize) {
                            var bufferVector        = Sse2.LoadVector128(srcPointer   + srcPos + n);
                            var maskingKeyVector    = Sse2.LoadVector128(maskPointer  + (maskPos + n) % 4);
                            var xor                 = Sse2.Xor(bufferVector, maskingKeyVector);
                            Sse2.Store(destPointer + destPos + n, xor);
                        }
                    }
                }
            } else {
                for (int n = 0; n < length; n++) {
                    var b = src[srcPos + n];
                    dest[destPos + n] = (byte)(b ^ mask[(maskPos + n) % 4]);
                }
            }
        }
        
        internal static void Populate(byte[] arr) {
            if (!UseSse)
                return;
            arr[4] = arr [8] = arr[12] = arr[16] = arr[0];
            arr[5] = arr [9] = arr[13] = arr[17] = arr[1];
            arr[6] = arr[10] = arr[14] = arr[18] = arr[2];
            arr[7] = arr[11] = arr[15] = arr[19] = arr[3];
        }
    }
}
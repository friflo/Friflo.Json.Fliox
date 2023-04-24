// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

#if JSON_BURST
    using Unity.Collections.LowLevel.Unsafe;
#endif

namespace Friflo.Json.Burst
{
    public partial struct Bytes
    {
        /// <summary>
        /// <b>Obsolete</b> - as <see cref="AppendBytesOld"/> is obsolete
        /// </summary>
        public const int CopyRemainder = 8;
        
        public ReadOnlySpan<byte> AsSpan() {
            return new ReadOnlySpan<byte>(buffer, start, end - start);
        }

        public void AppendBytesSpan(in ReadOnlySpan<byte> bytes) {
            int srcLen  = bytes.Length;
            int dstEnd  = end;
            int newEnd  = dstEnd + srcLen;
            if (newEnd > buffer.Length) {
                DoubleSize(newEnd);
            }
            var target = new Span<byte>(buffer, dstEnd, srcLen);
            bytes.CopyTo(target);
            end = newEnd;
        }
        
        public void AppendBytes(in Bytes src) {
            AppendBytesSpan(src.AsSpan());
        }
        
        /// <summary>
        /// <b>Obsolete</b> - use <see cref="AppendBytesSpan"/><br/>
        /// Method not removed to remember crazy pointer arithmetic
        /// </summary>
        public unsafe void AppendBytesOld(ref Bytes src)
        {
/*
            int     curEnd = end;
            int     caLen = src.Len;
            int     newEnd = curEnd + caLen;
            ref var buf = ref buffer.array;
            EnsureCapacity(caLen);
            int     n2 = src.StartPos;
            ref var str2 = ref src.buffer.array;
            for (int n = curEnd; n < newEnd; n++)
                buf[n] = str2[n2++];
            end = newEnd;
            hc = BytesConst.notHashed;
*/
            int curEnd = end;
            // ensure both buffer's are large enough when accessing the byte array's via unsafe (long*)
            var srcLen = src.end + CopyRemainder;
            if (srcLen > src.buffer.Length) {
                src.DoubleSize(srcLen);
            }
            int len     = src.end - src.start;
            int dstLen  = end + len + CopyRemainder;
            if (dstLen > buffer.Length) {
                DoubleSize(dstLen);
            }
           
            end += len;
#if JSON_BURST
            byte*  srcPtr =  &((byte*)src.buffer.array.GetUnsafeList()->Ptr) [src.start];
            byte*  destPtr = &((byte*)buffer.array.GetUnsafeList()->Ptr)     [curEnd];
#else
            fixed (byte* srcPtr =     &src.buffer                            [src.start])
            fixed (byte* destPtr =    &buffer                                [curEnd])
#endif
            {
                if (len <= 8) {
                    *(long*)(destPtr +  0) = *(long*)(srcPtr +  0);
                    return;
                }
                if (len <= 16) {
                    *(long*)(destPtr +  0) = *(long*)(srcPtr +  0);
                    *(long*)(destPtr +  8) = *(long*)(srcPtr +  8);
                    return;
                }
                if (len <= 24) {
                    *(long*)(destPtr +  0) = *(long*)(srcPtr +  0);
                    *(long*)(destPtr +  8) = *(long*)(srcPtr +  8);
                    *(long*)(destPtr + 16) = *(long*)(srcPtr + 16);
                    return;
                }
                if (len <= 32) {
                    *(long*)(destPtr +  0) = *(long*)(srcPtr +  0);
                    *(long*)(destPtr +  8) = *(long*)(srcPtr +  8);
                    *(long*)(destPtr + 16) = *(long*)(srcPtr + 16);
                    *(long*)(destPtr + 24) = *(long*)(srcPtr + 24);
                    return;
                }
                if (len <= 40) {
                    *(long*)(destPtr +  0) = *(long*)(srcPtr +  0);
                    *(long*)(destPtr +  8) = *(long*)(srcPtr +  8);
                    *(long*)(destPtr + 16) = *(long*)(srcPtr + 16);
                    *(long*)(destPtr + 24) = *(long*)(srcPtr + 24);
                    *(long*)(destPtr + 32) = *(long*)(srcPtr + 32);
                    return;
                }
                if (len <= 48) {
                    *(long*)(destPtr +  0) = *(long*)(srcPtr +  0);
                    *(long*)(destPtr +  8) = *(long*)(srcPtr +  8);
                    *(long*)(destPtr + 16) = *(long*)(srcPtr + 16);
                    *(long*)(destPtr + 24) = *(long*)(srcPtr + 24);
                    *(long*)(destPtr + 32) = *(long*)(srcPtr + 32);
                    *(long*)(destPtr + 40) = *(long*)(srcPtr + 40);
                    return;
                }
                if (len <= 56) {
                    *(long*)(destPtr +  0) = *(long*)(srcPtr +  0);
                    *(long*)(destPtr +  8) = *(long*)(srcPtr +  8);
                    *(long*)(destPtr + 16) = *(long*)(srcPtr + 16);
                    *(long*)(destPtr + 24) = *(long*)(srcPtr + 24);
                    *(long*)(destPtr + 32) = *(long*)(srcPtr + 32);
                    *(long*)(destPtr + 40) = *(long*)(srcPtr + 40);
                    *(long*)(destPtr + 48) = *(long*)(srcPtr + 48);
                    return;
                }
                if (len <= 64) {
                    *(long*)(destPtr +  0) = *(long*)(srcPtr +  0);
                    *(long*)(destPtr +  8) = *(long*)(srcPtr +  8);
                    *(long*)(destPtr + 16) = *(long*)(srcPtr + 16);
                    *(long*)(destPtr + 24) = *(long*)(srcPtr + 24);
                    *(long*)(destPtr + 32) = *(long*)(srcPtr + 32);
                    *(long*)(destPtr + 40) = *(long*)(srcPtr + 40);
                    *(long*)(destPtr + 48) = *(long*)(srcPtr + 48);
                    *(long*)(destPtr + 56) = *(long*)(srcPtr + 56);
                    return;
                }
#if JSON_BURST
                UnsafeUtility.MemCpy(destPtr, srcPtr, len);
#else
                Buffer.MemoryCopy(srcPtr, destPtr, buffer.Length - curEnd, len);
                // Buffer.BlockCopy(src.buffer.array, src.start, buffer.array, curEnd, len);
#endif
            }
        }
    }
}
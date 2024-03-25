// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

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

        public void AppendBytesSpan(in ReadOnlySpan<byte> source) {
            int count   = source.Length;
            int curEnd  = end;
            int newEnd  = curEnd + count;
            if (newEnd > buffer.Length) {
                DoubleSize(newEnd);
            }
            end = newEnd;
            var target = new Span<byte>(buffer, curEnd, count);
            source.CopyTo(target);
        }
        
        public unsafe void AppendBytes(in Bytes source) {
            int count   = source.end - source.start;
            int curEnd  = end;
            int newEnd  = curEnd + count;
            if (newEnd > buffer.Length) {
                DoubleSize(newEnd);
            }
            end = newEnd;
#if SPAN_IMPLEMENTATION
            // 4.6 x slower in Unity 2021.3.9f1 with "0123"
            // 1.2 x slower in net6.0           with "0123"
            var srcSpan = new ReadOnlySpan<byte>(source.buffer, source.start, count);   
            var dstSpan = new Span<byte>(buffer, curEnd, count);
            srcSpan.CopyTo(dstSpan);
#else
            fixed (byte* src    = &source.buffer [source.start])
            fixed (byte* dst    = &buffer        [curEnd]) {
                if (count <= 8) {
                    switch (count) {
                        case 0: return;
                        case 1: *(byte*) (dst)              = *(byte*) (src);        return;
                        case 2: *(short*)(dst)              = *(short*)(src);        return;
                        case 3: *(short*)(dst)              = *(short*)(src);
                                *(byte*) (dst + 2)          = *(byte*) (src + 2);    return;
                        default:
                                *(int*)  (dst)              = *(int*)  (src);
                                // copy last 4 bytes                
                                *(int*)  (dst + count - 4)  = *(int*)  (src + count - 4);  return;
                    }
                }
                // copy last 8 bytes
                *(long*)(dst +  count - 8)  = *(long*)(src +  count - 8);
                
                if (count < 16) {
                    *(long*)(dst +  0)      = *(long*)(src +  0);
                    return;
                }
                if (count < 24) {
                    *(long*)(dst +  0)      = *(long*)(src +  0);
                    *(long*)(dst +  8)      = *(long*)(src +  8);
                    return;
                }
                if (count < 32) {
                    *(long*)(dst +  0)      = *(long*)(src +  0);
                    *(long*)(dst +  8)      = *(long*)(src +  8);
                    *(long*)(dst +  16)     = *(long*)(src +  16);
                    return;
                }
                if (count < 40) {
                    *(long*)(dst +  0)      = *(long*)(src +  0);
                    *(long*)(dst +  8)      = *(long*)(src +  8);
                    *(long*)(dst +  16)     = *(long*)(src +  16);
                    *(long*)(dst +  24)     = *(long*)(src +  24);
                    return;
                }
                if (count < 48) {
                    *(long*)(dst +  0)      = *(long*)(src +  0);
                    *(long*)(dst +  8)      = *(long*)(src +  8);
                    *(long*)(dst +  16)     = *(long*)(src +  16);
                    *(long*)(dst +  24)     = *(long*)(src +  24);
                    *(long*)(dst +  32)     = *(long*)(src +  32);
                    return;
                }
                if (count < 56) {
                    *(long*)(dst +  0)      = *(long*)(src +  0);
                    *(long*)(dst +  8)      = *(long*)(src +  8);
                    *(long*)(dst +  16)     = *(long*)(src +  16);
                    *(long*)(dst +  24)     = *(long*)(src +  24);
                    *(long*)(dst +  32)     = *(long*)(src +  32);
                    *(long*)(dst +  40)     = *(long*)(src +  40);
                    return;
                }
                if (count < 64) {
                    *(long*)(dst +  0)      = *(long*)(src +  0);
                    *(long*)(dst +  8)      = *(long*)(src +  8);
                    *(long*)(dst +  16)     = *(long*)(src +  16);
                    *(long*)(dst +  24)     = *(long*)(src +  24);
                    *(long*)(dst +  32)     = *(long*)(src +  32);
                    *(long*)(dst +  40)     = *(long*)(src +  40);
                    *(long*)(dst +  48)     = *(long*)(src +  48);
                    return;
                }
                Buffer.MemoryCopy(src, dst, buffer.Length - curEnd, count);
            }
#endif
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
            
            fixed (byte* srcPtr =     &src.buffer   [src.start])
            fixed (byte* destPtr =    &buffer       [curEnd])
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
                Buffer.MemoryCopy(srcPtr, destPtr, buffer.Length - curEnd, len);
                // Buffer.BlockCopy(src.buffer.array, src.start, buffer.array, curEnd, len);
            }
        }
    }
}
﻿// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Runtime.CompilerServices;

namespace Friflo.Json.Burst.Utils
{
    [CLSCompliant(true)]
    public struct Utf8Utils
    {

        public static bool IsStringEqualUtf8(String str, in Bytes bytes) {
            return IsStringEqualUtf8(str, bytes.buffer, bytes.start, bytes.end);
        }

        public static bool IsStringEqualUtf8 (String str, byte[] bytes, int start, int end) {
            int strPos = 0;
            int bytePos = start;
            while (bytePos < end && strPos < str.Length) {
                int utf8Bytes= UnicodeFromUtf8Bytes(bytes, ref bytePos);
                
                int utf8Str = Char.ConvertToUtf32 (str, strPos);
                strPos +=     Char.IsSurrogatePair(str, strPos) ? 2 : 1;
                
                if (utf8Bytes != utf8Str)
                    return false;
            }
            return bytePos == end && strPos == str.Length;
        }
        
        private static readonly int     m_1ooooooo = 0x80;
        private static readonly int     m_11oooooo = 0xc0;
        private static readonly int     m_111ooooo = 0xe0;
        private static readonly int     m_1111oooo = 0xf0;
    
        private static readonly int     m_ooooo111 = 0x07;
        private static readonly int     m_oooo1111 = 0x0f;
        private static readonly int     m_ooo11111 = 0x1f;
        private static readonly int     m_oo111111 = 0x3f;
        
        static int UnicodeFromUtf8Bytes(byte[] bytes, ref int n) {
            int b = bytes[n++];
        
            if  (b < m_1ooooooo )
            {
                // 0000 0000 0000 007F
                return b;
            }

            if (b < m_11oooooo) {
                throw new InvalidOperationException("highNibble < 1100 (binary)");
            }

            if (b < m_111ooooo) {
                // 0000 0080 0000 07FF
                return  ( (b            & m_ooo11111) << 6)
                        | (bytes[n++]   & m_oo111111);
            }

            if (b < m_1111oooo) {
                // 0000 0800 0000 FFFF
                return ( (b             & m_oooo1111) << 12) 
                       | ((bytes[n++]   & m_oo111111) << 6)
                       |  (bytes[n++]   & m_oo111111);
            }

            //     0001 0000 0010 FFFF
            return (  (b                & m_ooooo111) << 18)
                    | ((bytes[n++]      & m_oo111111) << 12)
                    | ((bytes[n++]      & m_oo111111) << 6)
                    |  (bytes[n++]      & m_oo111111);
        }

        /// NOTE!: Caller need to ensure dst buffer has sufficient capacity -> 4 bytes
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AppendUnicodeToBytes(ref Bytes dst, int uni) {
            // UTF-8 Encoding
#if DEBUG
            int len = dst.end - dst.start;
            if (len + 4 > dst.buffer.Length)
                throw new InvalidOperationException("Insufficient buffer size");
#endif
            var dstArr = dst.buffer;
            int i = dst.end;
            if (uni < 0x80)
            {
                dstArr[i] =    (byte)uni;
                dst.end = i + 1;
                return;
            }
            if (uni < 0x800)
            {
                dstArr[i]   =  (byte)(m_11oooooo | (uni >> 6));
                dstArr[i+1] =  (byte)(m_1ooooooo | (uni         & m_oo111111));
                dst.end = i + 2;
                return;
            }
            if (uni < 0x10000)
            {
                dstArr[i]   =  (byte)(m_111ooooo |  (uni >> 12));
                dstArr[i+1] =  (byte)(m_1ooooooo | ((uni >> 6)  & m_oo111111));
                dstArr[i+2] =  (byte)(m_1ooooooo |  (uni        & m_oo111111));
                dst.end = i + 3;
                return;
            }
            dstArr[i]   =      (byte)(m_1111oooo |  (uni >> 18));
            dstArr[i+1] =      (byte)(m_1ooooooo | ((uni >> 12) & m_oo111111));
            dstArr[i+2] =      (byte)(m_1ooooooo | ((uni >> 6)  & m_oo111111));
            dstArr[i+3] =      (byte)(m_1ooooooo |  (uni        & m_oo111111));
            dst.end = i + 4;
        }
    }
}
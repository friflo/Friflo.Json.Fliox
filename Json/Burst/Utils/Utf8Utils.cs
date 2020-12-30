// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Burst.Utils
{
    public struct Utf8Utils
    {
        public static bool IsStringEqualUtf8(String str, Bytes bytes) {
            return IsStringEqualUtf8(str, ref bytes);
        }

        public static bool IsStringEqualUtf8(String str, ref Bytes bytes) {
            return IsStringEqualUtf8(str, ref bytes.buffer, bytes.start, bytes.end);
        }

        public static bool IsStringEqualUtf8 (String str, ref ByteArray bytes, int start, int end) {
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
        
        private static readonly int 	m_1ooooooo = 0x80;
        private static readonly int 	m_11oooooo = 0xc0;
        private static readonly int 	m_111ooooo = 0xe0;
        private static readonly int 	m_1111oooo = 0xf0;
	
        private static readonly int 	m_ooooo111 = 0x07;
        private static readonly int 	m_oooo1111 = 0x0f;
        private static readonly int 	m_ooo11111 = 0x1f;
        private static readonly int 	m_oo111111 = 0x3f;
        
        static int UnicodeFromUtf8Bytes(ByteArray byteArray, ref int n) {
            var bytes = byteArray.array;
            int b = bytes[n++];
		
            if 	(b < m_1ooooooo )
            {
                // 0000 0000 0000 007F
                return b;
            }

            if (b < m_11oooooo) {
                throw new FrifloException("highNibble < 1100 (binary)");
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
    }
}
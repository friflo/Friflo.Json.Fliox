// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Friflo.Json.Burst.Utils
{
    /// <summary>
    /// Utility methods to support SSO - short string optimization<br/>
    /// In case the UTF-8 representation of a string fits in 16 bytes
    /// <b> 15 bytes + 1 length byte  </b>
    /// a string is encode in <b>two long</b> values.<br/>
    /// <br/>
    /// <b>Two long</b> fields are used by the <c>struct JsonKey</c> internally.
    /// </summary>
    public static class ShortStringUtils
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public  const int   MaxLength      = 15;
        /// <summary> shift highest byte of lng2 7 bytes right to get byte count</summary>
        private const int   ShiftLength    = 56;
        /// <summary> <see cref="MaxLength"/> + 1 </summary>
        private const int   ByteCount      = 16;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetLength (long lng2) {
            return (int)(lng2 >> ShiftLength);
        }
        
        /// <summary>
        /// If <paramref name="value"/> contains control characters => use string instance.<br/>
        /// Same behavior as in <see cref="BytesToLongLong"/>
        /// </summary>
        public static unsafe void StringToLongLong(string value, out string str, out long lng, out long lng2)
        {
            if (ContainsControlChars(value)) {
                str     = value;
                lng     = 0;
                lng2    = 0;
                return;
            }
            var byteCount = Encoding.UTF8.GetByteCount(value);
            if (byteCount <= MaxLength) {
                Span<byte> bytes        = stackalloc byte[ByteCount];
                bytes[MaxLength]        = (byte)byteCount;
                Encoding.UTF8.GetBytes(value, bytes);
                fixed (byte*  bytesPtr  = &bytes[0]) 
                fixed (long*  lngPtr    = &lng)
                fixed (long*  lngPtr2   = &lng2)
                {
                    var bytesLongPtr    = (long*)bytesPtr;
                    *lngPtr             = bytesLongPtr[0];
                    *lngPtr2            = bytesLongPtr[1];
                }
                str = null;
                return;
            }
            str     = value;
            lng     = 0;
            lng2    = 0;
        }
        
        private static bool ContainsControlChars(string value) {
            foreach (var c in value) {
                switch (c) {
                    case '"':
                    case '\\':
                        return true;
                }
            }
            return false;
        }
        
        public static unsafe void LongLongToString(long lng, long lng2, out string str) {
            Span<byte> bytes    = stackalloc byte[ByteCount];
            int byteCount       = GetLength(lng2);
            fixed (byte*  bytesPtr  = &bytes[0]) {
                var bytesLongPtr    = (long*)bytesPtr;
                bytesLongPtr[0]     = lng;
                bytesLongPtr[1]     = lng2;
                str                 = Encoding.UTF8.GetString(bytesPtr, byteCount);
            }
        }
        
        /// <summary>
        /// If <paramref name="value"/> contains control characters => use string instance.<br/>
        /// Same behavior as in <see cref="StringToLongLong"/>
        /// </summary>
        public static unsafe bool BytesToLongLong (in Bytes value, out long lng, out long lng2) {
            var byteCount = value.end - value.start;
            if (byteCount > MaxLength) {
                lng     = 0;
                lng2    = 0;
                return false;
            }
            // --- use string instance if JSON control characters included
            var end = value.end;
            var buf = value.buffer;
            for (int i = value.start; i < end; i++) {
                switch (buf[i]) {
                    case (int)'"':
                    case (int)'\\':
                        lng     = 0;
                        lng2    = 0;
                        return false;
                }
            }
            Span<byte> src  = new Span<byte>(value.buffer, value.start, byteCount);
            Span<byte> dst  = stackalloc byte[ByteCount];
            src.CopyTo(dst);                    // copy byteCount bytes to dst 
            dst[MaxLength] = (byte)byteCount;   // set last byte to length
            
            fixed (byte*  bytesPtr  = dst) 
            fixed (long*  lngPtr    = &lng)
            fixed (long*  lngPtr2   = &lng2)
            {
                var bytesLongPtr    = (long*)bytesPtr;
                *lngPtr             = bytesLongPtr[0];
                *lngPtr2            = bytesLongPtr[1];
            }
            return true;
        }
        
        public static unsafe int GetChars(long lng, long lng2, in Span<char> dst) {
            int byteCount           = GetLength(lng2);
            Span<byte> bytes        = stackalloc byte[ByteCount];
            ReadOnlySpan<byte> src  = bytes.Slice(0, byteCount);
            fixed (byte*  bytesPtr = &bytes[0]) {
                var bytesLongPtr    = (long*)bytesPtr;
                bytesLongPtr[0]     = lng;
                bytesLongPtr[1]     = lng2;
                return Encoding.UTF8.GetChars(src, dst);
            }
        }
    }
}
// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;

namespace Friflo.Json.Burst.Utils
{
    public static class ShortString
    {
        public const int MaxLength      = 15;
        public const int ShiftLength    = 56;
        
        public static unsafe void StringToLongLong(string value, out string str, out long lng, out long lng2) {
            var byteCount       = Encoding.UTF8.GetByteCount(value);
            if (byteCount <= MaxLength) {
                Span<byte> bytes        = stackalloc byte[16];
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
        
        public static unsafe void LongLongToString(long lng, long lng2, out string str) {
            Span<byte> bytes    = stackalloc byte[16];
            int byteCount       = (int)(lng2 >> ShiftLength); // shift 7 bytes right 
            fixed (byte*  bytesPtr  = &bytes[0]) {
                var bytesLongPtr    = (long*)bytesPtr;
                bytesLongPtr[0]     = lng;
                bytesLongPtr[1]     = lng2;
                str                 = Encoding.UTF8.GetString(bytesPtr, byteCount);
            }
        }
        
        public static unsafe void BytesToLongLong (in Bytes value, out long lng, out long lng2) {
            var byteCount       = value.end - value.start;
            if (byteCount > MaxLength) throw new ArgumentException($"expect value length <= {MaxLength}");
            Span<byte> src      = new Span<byte>(value.buffer, value.start, byteCount);
            Span<byte> bytes    = stackalloc byte[16];
            src.CopyTo(bytes);

            bytes[MaxLength] = (byte)byteCount;
            fixed (byte*  bytesPtr  = bytes) 
            fixed (long*  lngPtr    = &lng)
            fixed (long*  lngPtr2   = &lng2)
            {
                var bytesLongPtr    = (long*)bytesPtr;
                *lngPtr             = bytesLongPtr[0];
                *lngPtr2            = bytesLongPtr[1];
            }
        }
        
        public static unsafe int GetChars(long lng, long lng2, in Span<char> chars) {
            int byteCount       = (int)(lng2 >> ShiftLength); // shift 7 bytes right
            Span<byte> bytes    = stackalloc byte[byteCount];
            fixed (byte*  bytesPtr  = &bytes[0]) {
                var bytesLongPtr    = (long*)bytesPtr;
                bytesLongPtr[0]     = lng;
                bytesLongPtr[1]     = lng2;
                return Encoding.UTF8.GetChars(bytes, chars);
            }
        }
    }
}
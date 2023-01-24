// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;

namespace Friflo.Json.Burst.Utils
{
    public static class ShortStringUtils
    {
        public static unsafe void StringToLongLong(string value, out string str, out long lng, out long lng2) {
            var byteCount       = Encoding.UTF8.GetByteCount(value);
            if (byteCount <= 15) {
                Span<byte> bytes    = stackalloc byte[16];
                bytes[15]           = (byte)byteCount;
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
            fixed (byte*  bytesPtr  = &bytes[0]) {
                int byteCount       = (int)(lng2 >> 56); // shift 7 bytes right 
                var bytesLongPtr    = (long*)bytesPtr;
                bytesLongPtr[0]     = lng;
                bytesLongPtr[1]     = lng2;
                str                 = Encoding.UTF8.GetString(bytesPtr, byteCount);
            }
        }
    }
}
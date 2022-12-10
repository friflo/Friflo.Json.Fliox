// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Burst.Utils
{
    public static class GuidUtils
    {
        public static unsafe Guid LongLongToGuid(in long lng, in long lng2) {
            ReadOnlySpan<byte> bytes = stackalloc byte[16];
            fixed (byte*  bytesPtr  = &bytes[0]) 
            fixed (long*  lngPtr    = &lng)
            fixed (long*  lngPtr2   = &lng2)
            {
                var bytesLongPtr    = (long*)bytesPtr;
                bytesLongPtr[0]     = *lngPtr;
                bytesLongPtr[1]     = *lngPtr2;
            }
            return new Guid(bytes);
        }
        
        public static unsafe void GuidToLongLong(in Guid guid, out long lng, out long lng2) {
            Span<byte> bytes = stackalloc byte[16];
            guid.TryWriteBytes(bytes);
            fixed (byte*  bytesPtr  = &bytes[0]) 
            fixed (long*  lngPtr    = &lng)
            fixed (long*  lngPtr2   = &lng2)
            {
                var bytesLongPtr    = (long*)bytesPtr;
                *lngPtr             = bytesLongPtr[0];
                *lngPtr2            = bytesLongPtr[1];
            }
        }
    }
}
// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Burst.Utils
{
    public static class GuidUtils
    {
        public static unsafe Guid LongLongToGuid(in long lng, in long lng2) {
#if NETSTANDARD2_0
            var bytes = new byte[16]; // NETSTANDARD2_0_ALLOC
#else
            ReadOnlySpan<byte> bytes = stackalloc byte[16];
#endif
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
#if NETSTANDARD2_0
            var bytes = guid.ToByteArray(); // NETSTANDARD2_0_ALLOC
#else
            Span<byte> bytes = stackalloc byte[16];
            guid.TryWriteBytes(bytes);
#endif
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
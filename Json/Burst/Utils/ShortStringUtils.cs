// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Text;
using static System.BitConverter;
using static System.Buffers.Binary.BinaryPrimitives;

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
    /// <remarks>
    /// <b>BigEndian support (ARM)</b> currently untested
    /// </remarks>
    public static class ShortStringUtils
    {
        // ReSharper disable once MemberCanBePrivate.Global
        /// Maximum number of bytes that can be stored in a short string
        public  const   int     MaxLength   = 15;
        /// number of bytes used to store a short string
        public  const   int     ByteCount   = 16;
        /// position of the short string length byte.
        private const   int     LengthPos   = 15;
        /// shift highest byte of lng2 7 bytes right to get byte count
        public  const   int     ShiftLength = 56;
        
        
        /// <summary>
        /// <c>lng2</c> == <see cref="IsNull"/>     => string is null
        /// </summary>
        public  const int   IsNull      = 0;
        /// <summary>
        /// <c>lng2</c> == <see cref="IsString"/>   => string is represented by a <see cref="string"/> instance
        /// stored in <c>str</c><br/>
        /// </summary>
        public  const long  IsString    = 0x7f00_0000_0000_0000;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetLength (long lng2) {
            return (int)(lng2 >> ShiftLength) - 1;
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
                lng2    = IsString;
                return;
            }
            var byteCount = Encoding.UTF8.GetByteCount(value);
            if (byteCount <= MaxLength) {
                Span<byte> bytes        = stackalloc byte[ByteCount];
                bytes[LengthPos]        = (byte)(byteCount + 1); // set highest byte to length
                Encoding.UTF8.GetBytes(value.AsSpan(), bytes);
                fixed (byte*  bytesPtr  = &bytes[0]) 
                fixed (long*  lngPtr    = &lng)
                fixed (long*  lngPtr2   = &lng2)
                {
                    var bytesLongPtr    = (long*)bytesPtr;
                    *lngPtr             = IsLittleEndian ? bytesLongPtr[0] : ReverseEndianness(bytesLongPtr[0]);
                    *lngPtr2            = IsLittleEndian ? bytesLongPtr[1] : ReverseEndianness(bytesLongPtr[1]);
                }
                str = null;
                return;
            }
            str     = value;
            lng     = 0;
            lng2    = IsString;
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
            if (lng2 == IsNull) {
                str = null;
                return;
            }
            Span<byte> bytes    = stackalloc byte[ByteCount];
            int byteCount       = GetLength(lng2);
            fixed (byte*  bytesPtr  = &bytes[0]) {
                var bytesLongPtr    = (long*)bytesPtr;
                bytesLongPtr[0]     = IsLittleEndian ? lng  : ReverseEndianness(lng);
                bytesLongPtr[1]     = IsLittleEndian ? lng2 : ReverseEndianness(lng2);
                str                 = Encoding.UTF8.GetString(bytesPtr, byteCount);
            }
        }
        
        /// <summary>
        /// If <paramref name="value"/> contains control characters => use string instance.<br/>
        /// Same behavior as in <see cref="StringToLongLong"/>
        /// </summary>
        public static unsafe bool BytesToLongLong (in ReadOnlySpan<byte> value, out long lng, out long lng2) {
            var len = value.Length;
            if (len > MaxLength) {
                lng     = 0;
                lng2    = 0;
                return false;
            }
            // --- use string instance if JSON control characters included
            for (int i = 0; i < len; i++) {
                switch (value[i]) {
                    case (int)'"':
                    case (int)'\\':
                        lng     = 0;
                        lng2    = 0;
                        return false;
                }
            }
            Span<byte> dst  = stackalloc byte[ByteCount];
            value.CopyTo(dst);
            dst[LengthPos] = (byte)(len + 1); // set highest byte to length
            
            fixed (byte*  bytesPtr  = dst) 
            fixed (long*  lngPtr    = &lng)
            fixed (long*  lngPtr2   = &lng2)
            {
                var bytesLongPtr    = (long*)bytesPtr;
                *lngPtr             = IsLittleEndian ? bytesLongPtr[0] : ReverseEndianness(bytesLongPtr[0]);
                *lngPtr2            = IsLittleEndian ? bytesLongPtr[1] : ReverseEndianness(bytesLongPtr[1]);
            }
            return true;
        }
        
        public static unsafe int GetChars(long lng, long lng2, in Span<char> dst) {
            int byteCount           = GetLength(lng2);
            Span<byte> bytes        = stackalloc byte[ByteCount];
            ReadOnlySpan<byte> src  = bytes.Slice(0, byteCount);
            fixed (byte*  bytesPtr = &bytes[0]) {
                var bytesLongPtr    = (long*)bytesPtr;
                bytesLongPtr[0]     = IsLittleEndian ? lng  : ReverseEndianness(lng);
                bytesLongPtr[1]     = IsLittleEndian ? lng2 : ReverseEndianness(lng2);
                return Encoding.UTF8.GetChars(src, dst);
            }
        }
    }
}
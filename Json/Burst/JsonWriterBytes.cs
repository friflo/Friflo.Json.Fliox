// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.


using System;

namespace Friflo.Json.Burst
{
    public partial struct Utf8JsonWriter
    {
    
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        // --- comment to enable source alignment in WinMerge
        public static void AppendEscStringBytes(ref Bytes dst, ReadOnlySpan<byte> src)
        {
            int srcLen      = src.Length;
            dst.EnsureCapacity(2 * srcLen + 1);
            var dstArr  = dst.buffer;
            
            dstArr[dst.end++] = (byte)'"';
            for (int n = 0; n < srcLen; n++) {
                byte c =  src[n];

                switch (c) {
                    case (byte) '"':  dstArr[dst.end++] = (byte) '\\'; dstArr[dst.end++] = (byte) '\"'; break;
                    case (byte) '\\': dstArr[dst.end++] = (byte) '\\'; dstArr[dst.end++] = (byte) '\\'; break;
                    case (byte) '\b': dstArr[dst.end++] = (byte) '\\'; dstArr[dst.end++] = (byte) 'b'; break;
                    case (byte) '\f': dstArr[dst.end++] = (byte) '\\'; dstArr[dst.end++] = (byte) 'f'; break;
                    case (byte) '\r': dstArr[dst.end++] = (byte) '\\'; dstArr[dst.end++] = (byte) 'r'; break;
                    case (byte) '\n': dstArr[dst.end++] = (byte) '\\'; dstArr[dst.end++] = (byte) 'n'; break;
                    case (byte) '\t': dstArr[dst.end++] = (byte) '\\'; dstArr[dst.end++] = (byte) 't'; break;
                    default:
                        dstArr[dst.end++] = c;
                        break;
                }
            }
            dstArr[dst.end++] = (byte)'"';
        }

        private void AppendKeyBytes(ref Bytes dst, ReadOnlySpan<byte> key)
        {
            AppendEscStringBytes(ref dst, key);
            ref var dstArr = ref dst.buffer;
            dst.EnsureCapacityAbs(dst.end + 1);
            dstArr[dst.end++] = (byte)':';
            if (pretty) {
                dst.EnsureCapacityAbs(dst.end + 1);
                dstArr[dst.end++] = (byte)' ';
            }
        }

        // ----------------------------- object with properties -----------------------------





        
        // --- comment to enable source alignment in WinMerge
        /// <summary>Writes the key of key/value pair where the value will be an array</summary>
        public void MemberArrayStart(ReadOnlySpan<byte> key) {
            AssertMember();
            AddSeparator();
            AppendKeyBytes(ref json, key);
            SetStartGuard();
            ArrayStart(true);
        }
        
        /// <summary>Writes the key of key/value pair where the value will be an object</summary>
        public void MemberObjectStart(ReadOnlySpan<byte> key) {
            AssertMember();
            AddSeparator();
            AppendKeyBytes(ref json, key);
            SetStartGuard();
            ObjectStart();
        }
        
        /// <summary>Writes a key/value pair where the value is a "string"</summary>
        public void MemberStr(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value) {
            AssertMember();
            AddSeparator();
            AppendKeyBytes(ref json, key);
            AppendEscStringBytes(ref json, value);
        }
        
        /// <summary>
        /// Writes a key/value pair where the value is a <see cref="string"/><br/>
        /// </summary>
        public void MemberStr(ReadOnlySpan<byte> key, in ReadOnlySpan<char> value) {
            AssertMember();
            AddSeparator();
            AppendKeyBytes(ref json, key);
            AppendEscString(ref json, value);
        }

        /// <summary>Writes a key/value pair where the value is a <see cref="double"/></summary>
        public void MemberDbl(ReadOnlySpan<byte> key, double value) {
            AssertMember();
            AddSeparator();
            AppendKeyBytes(ref json, key);
            format.AppendDbl(ref json, value);
        }
        
        /// <summary>Writes a key/value pair where the value is a <see cref="long"/></summary>
        public void MemberLng(ReadOnlySpan<byte> key, long value) {
            AssertMember();
            AddSeparator();
            AppendKeyBytes(ref json, key);
            format.AppendLong(ref json, value);
        }
        
        /// <summary>Writes a key/value pair where the value is a <see cref="bool"/></summary>
        public void MemberBln(ReadOnlySpan<byte> key, bool value) {
            AssertMember();
            AddSeparator();
            AppendKeyBytes(ref json, key);
            format.AppendBool(ref json, value);
        }
        
        /// <summary>Writes a key/value pair where the value is null</summary>
        public void MemberNul(ReadOnlySpan<byte> key) {
            AssertMember();
            AddSeparator();
            AppendKeyBytes(ref json, key);
            json.AppendStr32(@null);
        }
        
        /// <summary>Writes a key/value pair where the value is JSON</summary>
        public void MemberArr(ReadOnlySpan<byte> key, in Bytes jsonValue) {
            AssertMember();
            AddSeparator();
            AppendKeyBytes(ref json, key);
            json.AppendBytes(jsonValue);
        }
        
        /// <summary>Writes a key/value pair where the value is Guid</summary>
        public void MemberGuid(ReadOnlySpan<byte> key, in Guid guid) {
            AssertMember();
            AddSeparator();
            AppendKeyBytes(ref json, key);
            Span<char> chars = stackalloc char[Bytes.GuidLength]; 
            json.AppendChar('\"');
            json.AppendGuid(guid, chars);
            json.AppendChar('\"');
        }
        
        /// <summary>Writes a key/value pair where the value is DateTime</summary>
        public void MemberDate(ReadOnlySpan<byte> key, in DateTime dateTime) {
            AssertMember();
            AddSeparator();
            AppendKeyBytes(ref json, key);
            Span<char> chars = stackalloc char[Bytes.DateTimeLength]; 
            json.AppendChar('\"');
            json.AppendDateTime(dateTime, chars);
            json.AppendChar('\"');
        }
        
 
        
    }
}

// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


using System;

namespace Friflo.Json.Burst
{
    public partial struct Utf8JsonWriter
    {
    
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        // --- comment to enable source alignment in WinMerge
        public static void AppendEscStringBytes(ref Bytes dst, in Bytes src) {
            int srcLen      = src.end - src.start;
            int dstCapacity = dst.end + 2 * srcLen;
            if (dstCapacity > dst.buffer.Length) {
                dst.DoubleSize(dstCapacity);
            }
            int end     = src.end;
            var dstArr  = dst.buffer;
            var srcArr  = src.buffer; 
            
            dstArr[dst.end++] = (byte)'"';
            for (int n = src.start; n < end; n++) {
                byte c =  srcArr[n];

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

        private void AppendKeyBytes(ref Bytes dst, in Bytes key) {
            AppendEscStringBytes(ref dst, in key);
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
        public void MemberArrayStart(in Bytes key) {
            AssertMember();
            AddSeparator();
            AppendKeyBytes(ref json, in key);
            SetStartGuard();
            ArrayStart(true);
        }
        
        /// <summary>Writes the key of key/value pair where the value will be an object</summary>
        public void MemberObjectStart(in Bytes key) {
            AssertMember();
            AddSeparator();
            AppendKeyBytes(ref json, in key);
            SetStartGuard();
            ObjectStart();
        }
        
        /// <summary>Writes a key/value pair where the value is a "string"</summary>
        public void MemberStr(in Bytes key, in Bytes value) {
            AssertMember();
            AddSeparator();
            AppendKeyBytes(ref json, in key);
            AppendEscStringBytes(ref json, in value);
        }
        
        /// <summary>
        /// Writes a key/value pair where the value is a <see cref="string"/><br/>
        /// </summary>
        public void MemberStr(in Bytes key, string value) {
            AssertMember();
            AddSeparator();
            AppendKeyBytes(ref json, in key);
            AppendEscString(ref json, value);
        }

        /// <summary>Writes a key/value pair where the value is a <see cref="double"/></summary>
        public void MemberDbl(in Bytes key, double value) {
            AssertMember();
            AddSeparator();
            AppendKeyBytes(ref json, in key);
            format.AppendDbl(ref json, value);
        }
        
        /// <summary>Writes a key/value pair where the value is a <see cref="long"/></summary>
        public void MemberLng(in Bytes key, long value) {
            AssertMember();
            AddSeparator();
            AppendKeyBytes(ref json, in key);
            format.AppendLong(ref json, value);
        }
        
        /// <summary>Writes a key/value pair where the value is a <see cref="bool"/></summary>
        public void MemberBln(in Bytes key, bool value) {
            AssertMember();
            AddSeparator();
            AppendKeyBytes(ref json, in key);
            format.AppendBool(ref json, value);
        }
        
        /// <summary>Writes a key/value pair where the value is null</summary>
        public void MemberNul(in Bytes key) {
            AssertMember();
            AddSeparator();
            AppendKeyBytes(ref json, in key);
            json.AppendStr32(@null);
        }
        
        /// <summary>Writes a key/value pair where the value is JSON</summary>
        public void MemberArr(in Bytes key, in Bytes jsonValue) {
            AssertMember();
            AddSeparator();
            AppendKeyBytes(ref json, in key);
            json.AppendBytes(jsonValue);
        }
        
        /// <summary>Writes a key/value pair where the value is Guid</summary>
        public void MemberGuid(in Bytes key, in Guid guid) {
            AssertMember();
            AddSeparator();
            AppendKeyBytes(ref json, in key);
            Span<char> chars = stackalloc char[Bytes.GuidLength]; 
            json.AppendChar('\"');
            json.AppendGuid(guid, chars);
            json.AppendChar('\"');
        }
        
        /// <summary>Writes a key/value pair where the value is DateTime</summary>
        public void MemberDate(in Bytes key, in DateTime dateTime) {
            AssertMember();
            AddSeparator();
            AppendKeyBytes(ref json, in key);
            Span<char> chars = stackalloc char[Bytes.DateTimeLength]; 
            json.AppendChar('\"');
            json.AppendDateTime(dateTime, chars);
            json.AppendChar('\"');
        }
        
 
        
    }
}

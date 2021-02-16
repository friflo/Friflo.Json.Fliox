



namespace Friflo.Json.Burst
{
    public partial struct JsonSerializer
    {
    
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
#if JSON_BURST
        public static void AppendEscString(ref Bytes dst, in Unity.Collections.FixedString32 src) {
            dst.EnsureCapacityAbs(dst.end + 2 * src.Length);
            int end = src.Length;
            ref var dstArr = ref dst.buffer.array;
            var srcArr = src;
            
            dstArr[dst.end++] = (byte)'"';
            for (int n = 0; n < end; n++) {
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
#endif
        // --- comment to enable source alignment in WinMerge
        public static void AppendEscStringBytes(ref Bytes dst, in Bytes src) {
            dst.EnsureCapacityAbs(dst.end + 2 * src.Len);
            int end = src.end;
            ref var dstArr = ref dst.buffer.array;
            var srcArr = src.buffer.array; 
            
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
            dst.EnsureCapacityAbs(dst.end + key.Len + 3);
            ref var dstArr = ref dst.buffer.array;
            dstArr[dst.end++] = (byte)'"';
            dst.AppendBytesReadOnly(in key);
            dstArr[dst.end++] = (byte)'"';
            dstArr[dst.end++] = (byte)':';
            if (pretty) {
                dst.EnsureCapacityAbs(dst.end + 1 );
                dstArr[dst.end++] = (byte)' ';
            }
        }

        // ----------------------------- object with properties -----------------------------





        
        // --- comment to enable source alignment in WinMerge
        /// <summary>Writes the key of key/value pair where the value will be an array</summary>
        public void MemberArrayStartRef(in Bytes key) {
            AssertMember();
            AddSeparator();
            AppendKeyBytes(ref dst, in key);
            SetStartGuard();
            ArrayStart();
        }
        
        /// <summary>Writes the key of key/value pair where the value will be an object</summary>
        public void MemberObjectStartRef(in Bytes key) {
            AssertMember();
            AddSeparator();
            AppendKeyBytes(ref dst, in key);
            SetStartGuard();
            ObjectStart();
        }
        
        /// <summary>Writes a key/value pair where the value is a "string"</summary>
        public void MemberStrRef(in Bytes key, in Bytes value) {
            AssertMember();
            AddSeparator();
            AppendKeyBytes(ref dst, in key);
            AppendEscStringBytes(ref dst, in value);
        }
        
        /// <summary>
        /// Writes a key/value pair where the value is a <see cref="string"/><br/>
        /// </summary>
#if JSON_BURST
        public void MemberStrRef(in Bytes key, Unity.Collections.FixedString32 value) {
            AssertMember();
            AddSeparator();
            AppendKeyBytes(ref dst, in key);
            AppendEscString(ref dst, in value);
        }
#else
        public void MemberStrRef(in Bytes key, string value) {
            AssertMember();
            AddSeparator();
            AppendKeyBytes(ref dst, in key);
            AppendEscString(ref dst, in value);
        }
#endif
        /// <summary>Writes a key/value pair where the value is a <see cref="double"/></summary>
        public void MemberDblRef(in Bytes key, double value) {
            AssertMember();
            AddSeparator();
            AppendKeyBytes(ref dst, in key);
            format.AppendDbl(ref dst, value);
        }
        
        /// <summary>Writes a key/value pair where the value is a <see cref="long"/></summary>
        public void MemberLngRef(in Bytes key, long value) {
            AssertMember();
            AddSeparator();
            AppendKeyBytes(ref dst, in key);
            format.AppendLong(ref dst, value);
        }
        
        /// <summary>Writes a key/value pair where the value is a <see cref="bool"/></summary>
        public void MemberBlnRef(in Bytes key, bool value) {
            AssertMember();
            AddSeparator();
            AppendKeyBytes(ref dst, in key);
            format.AppendBool(ref dst, value);
        }
        
        /// <summary>Writes a key/value pair where the value is null</summary>
        public void MemberNulRef(in Bytes key) {
            AssertMember();
            AddSeparator();
            AppendKeyBytes(ref dst, in key);
            dst.AppendStr32Ref(ref @null);
        }
        
 
        
    }
}

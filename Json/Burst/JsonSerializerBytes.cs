



namespace Friflo.Json.Burst
{
    public partial struct JsonSerializer
    {
    
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
#if JSON_BURST
        public static void AppendEscString(ref Bytes dst, ref Unity.Collections.FixedString32 src) {
            dst.AppendChar('"');
            int end = src.Length;
            var srcArr = src; 
            for (int n = 0; n < end; n++) {
                char c = (char) srcArr[n];
                switch (c) {
                    case '"':
                        dst.AppendChar2('\\', '\"');
                        break;
                    case '\\':
                        dst.AppendChar2('\\', '\\');
                        break;
                    case '\b':
                        dst.AppendChar2('\\', 'b');
                        break;
                    case '\f':
                        dst.AppendChar2('\\', 'f');
                        break;
                    case '\r':
                        dst.AppendChar2('\\', 'r');
                        break;
                    case '\n':
                        dst.AppendChar2('\\', 'n');
                        break;
                    case '\t':
                        dst.AppendChar2('\\', 't');
                        break;
                    default:
                        dst.AppendChar(c);
                        break;
                }
            }
            dst.AppendChar('"');
        }
#endif
        // --- comment to enable source alignment in WinMerge
        public static void AppendEscString(ref Bytes dst, ref Bytes src) {
            dst.AppendChar('"');
            int end = src.end;
            var srcArr = src.buffer.array; 
            for (int n = src.start; n < end; n++) {
                char c = (char) srcArr[n];

                switch (c) {
                    case '"':
                        dst.AppendChar2('\\', '\"');
                        break;
                    case '\\':
                        dst.AppendChar2('\\', '\\');
                        break;
                    case '\b':
                        dst.AppendChar2('\\', 'b');
                        break;
                    case '\f':
                        dst.AppendChar2('\\', 'f');
                        break;
                    case '\r':
                        dst.AppendChar2('\\', 'r');
                        break;
                    case '\n':
                        dst.AppendChar2('\\', 'n');
                        break;
                    case '\t':
                        dst.AppendChar2('\\', 't');
                        break;
                    default:
                        dst.AppendChar(c);
                        break;
                }
            }
            dst.AppendChar('"');
        }



        // ----------------------------- object with properties -----------------------------





        
        // --- comment to enable source alignment in WinMerge
        /// <summary>Writes the key of key/value pair where the value will be an array</summary>
        // todo add ASCII version - to avoid escaping
        public void MemberArrayStartRef(ref Bytes key) {
            AssertMember();
            AddSeparator();
            AppendEscString(ref dst, ref key);
            dst.AppendChar(':');
            SetStartGuard();
            ArrayStart();
        }
        
        /// <summary>Writes the key of key/value pair where the value will be an object</summary>
        public void MemberObjectStartRef(ref Bytes key) {
            AssertMember();
            AddSeparator();
            AppendEscString(ref dst, ref key);
            dst.AppendChar(':');
            SetStartGuard();
            ObjectStart();
        }
        
        /// <summary>Writes a key/value pair where the value is a "string"</summary>
        public void MemberStrRef(ref Bytes key, ref Bytes value) {
            AssertMember();
            AddSeparator();
            AppendEscString(ref dst, ref key);
            dst.AppendChar(':');
            AppendEscString(ref dst, ref value);
        }
        
        /// <summary>
        /// Writes a key/value pair where the value is a <see cref="string"/><br/>
        /// </summary>
#if JSON_BURST
        public void MemberStrRef(ref Bytes key, Unity.Collections.FixedString32 value) {
            AssertMember();
            AddSeparator();
            AppendEscString(ref dst, ref key);
            dst.AppendChar(':');
            AppendEscString(ref dst, ref value);
        }
#else
        public void MemberStrRef(ref Bytes key, string value) {
            AssertMember();
            AddSeparator();
            AppendEscString(ref dst, ref key);
            dst.AppendChar(':');
            AppendEscString(ref dst, ref value);
        }
#endif
        /// <summary>Writes a key/value pair where the value is a <see cref="double"/></summary>
        public void MemberDblRef(ref Bytes key, double value) {
            AssertMember();
            AddSeparator();
            AppendEscString(ref dst, ref key);
            dst.AppendChar(':');
            format.AppendDbl(ref dst, value);
        }
        
        /// <summary>Writes a key/value pair where the value is a <see cref="long"/></summary>
        public void MemberDblRef(ref Bytes key, long value) {
            AssertMember();
            AddSeparator();
            AppendEscString(ref dst, ref key);
            dst.AppendChar(':');
            format.AppendLong(ref dst, value);
        }
        
        /// <summary>Writes a key/value pair where the value is a <see cref="bool"/></summary>
        public void MemberBlnRef(ref Bytes key, bool value) {
            AssertMember();
            AddSeparator();
            AppendEscString(ref dst, ref key);
            dst.AppendChar(':');
            format.AppendBool(ref dst, value);
        }
        
        /// <summary>Writes a key/value pair where the value is null</summary>
        public void MemberNulRef(ref Bytes key) {
            AssertMember();
            AddSeparator();
            AppendEscString(ref dst, ref key);
            dst.AppendChar(':');
            dst.AppendStr32Ref(ref @null);
        }
        
 
        
    }
}

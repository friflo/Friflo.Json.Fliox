

#if JSON_BURST
    using Str32 = Unity.Collections.FixedString32;
#else
    using Str32 = System.String;
#endif

namespace Friflo.Json.Burst
{
    public partial struct JsonSerializer
    {
    
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        // --- comment to enable source alignment in WinMerge
        public static void AppendEscString(ref Bytes dst, ref Bytes src) {
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
        }



        // ----------------------------- object with properties -----------------------------





        
        // --- comment to enable source alignment in WinMerge
        /// <summary>Writes the key of key/value pair where the value will be an array</summary>
        public void MemberArrayStart(ref Bytes key) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
            ArrayStart();
        }
        
        /// <summary>Writes the key of key/value pair where the value will be an object</summary>
        public void MemberObjectKey(ref Bytes key) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
        }
        
        /// <summary>Writes a key/value pair where the value is a "string"</summary>
        public void MemberString(ref Bytes key, ref Bytes value) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
            dst.AppendChar('"');
            AppendEscString(ref dst, ref value);
            dst.AppendChar('"');
        }
        
#if !JSON_BURST
        /// <summary>
        /// Writes a key/value pair where the value is a <see cref="string"/><br/>
        /// Method cant be used in a Unity Burst Job, because of using string as parameter
        /// </summary>
        public void MemberString(ref Bytes key, string value) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
            dst.AppendChar('"');
            AppendEscString(ref dst, ref value);
            dst.AppendChar('"');
        }
#endif
        /// <summary>Writes a key/value pair where the value is a <see cref="double"/></summary>
        public void MemberDouble(ref Bytes key, double value) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
            format.AppendDbl(ref dst, value);
        }
        
        /// <summary>Writes a key/value pair where the value is a <see cref="long"/></summary>
        public void MemberLong(ref Bytes key, long value) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
            format.AppendLong(ref dst, value);
        }
        
        /// <summary>Writes a key/value pair where the value is a <see cref="bool"/></summary>
        public void MemberBool(ref Bytes key, bool value) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
            format.AppendBool(ref dst, value);
        }
        
        /// <summary>Writes a key/value pair where the value is null</summary>
        public void MemberNull(ref Bytes key) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
            dst.AppendStr32(ref @null);
        }
        
 
        
    }
}



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
                        dst.AppendString("\\\"");
                        break;
                    case '\\':
                        dst.AppendString("\\\\");
                        break;
                    case '\b':
                        dst.AppendString("\\b");
                        break;
                    case '\f':
                        dst.AppendString("\\f");
                        break;
                    case '\r':
                        dst.AppendString("\\r");
                        break;
                    case '\n':
                        dst.AppendString("\\n");
                        break;
                    case '\t':
                        dst.AppendString("\\t");
                        break;
                    default:
                        dst.AppendChar(c);
                        break;
                }
            }
        }



        // ----------------------------- object with properties -----------------------------

        // TODO implement version with Str32 key
        
        public void PropertyArray(ref Bytes key) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendString("\":");
        }
        
        public void PropertyObject(ref Bytes key) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendString("\":");
        }
        
        public void PropertyString(ref Bytes key, ref Bytes value) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendString("\":\"");
            AppendEscString(ref dst, ref value);
            dst.AppendChar('"');
        }
        
        public void PropertyDouble(ref Bytes key, double value) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);            dst.AppendString("\":");
            format.AppendDbl(ref dst, value);
        }
        
        public void PropertyLong(ref Bytes key, long value) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendString("\":");
            format.AppendLong(ref dst, value);
        }
        
        public void PropertyBool(ref Bytes key, bool value) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendString("\":");
            format.AppendBool(ref dst, value);
        }
        
        public void PropertyNull(ref Bytes key) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendString("\":null");
        }
        
 
        
    }
}

using Friflo.Json.Burst.Utils;

#if JSON_BURST
	using Str32 = Unity.Collections.FixedString32;
#else
    using Str32 = System.String;
#endif


namespace Friflo.Json.Burst
{
    public struct JsonEncoder
    {
        private ValueFormat format;
        private ValueArray<bool> subsequentElement;
        private int level;

        public void InitEncoder() {
            format.InitTokenFormat();
            if (!subsequentElement.IsCreated())
                subsequentElement = new ValueArray<bool>(32);
        }

        // ----------------------------- object with properties -----------------------------
        public void ObjectStart(ref Bytes dst) {
            dst.AppendChar('{');
        }
        
        public void ObjectEnd(ref Bytes dst) {
            dst.AppendChar('}');
        }

        private void AddSeparator() {
            
        }
        
        // TODO implement version with Str32 key
        // public void PropertyString(ref Bytes dst, ref Str32 key, ref Bytes value) { }
        
        public void PropertyString(ref Bytes dst, ref Bytes key, ref Bytes value) {
            AddSeparator();
            dst.AppendChar('"');
            dst.AppendBytes(ref key);
            dst.AppendString("\":\"");
            dst.AppendBytes(ref value);
            dst.AppendChar('"');
        }
        
        public void PropertyDouble(ref Bytes dst, ref Bytes key, double value) {
            AddSeparator();
            dst.AppendChar('"');
            dst.AppendBytes(ref key);
            dst.AppendString("\":");
            format.AppendDbl(ref dst, value);
        }
        
        public void PropertyLong(ref Bytes dst, ref Bytes key, long value) {
            AddSeparator();
            dst.AppendChar('"');
            dst.AppendBytes(ref key);
            dst.AppendString("\":");
            format.AppendLong(ref dst, value);
        }
        
        public void PropertyBool(ref Bytes dst, ref Bytes key, bool value) {
            AddSeparator();
            dst.AppendChar('"');
            dst.AppendBytes(ref key);
            dst.AppendString("\":");
            format.AppendBool(ref dst, value);
        }
        
        public void PropertyNull(ref Bytes dst, ref Bytes key) {
            AddSeparator();
            dst.AppendChar('"');
            dst.AppendBytes(ref key);
            dst.AppendString("\":null");
        }
        
        // ----------------------------- array with elements -----------------------------
        public void ArrayStart(ref Bytes dst) {
            AddSeparator();
            dst.AppendChar('{');
        }
        
        public void ArrayEnd(ref Bytes dst) {
            AddSeparator();
            dst.AppendChar('}');
        }
        
        public void ElementString(ref Bytes dst, ref Bytes value) {
            AddSeparator();
            dst.AppendChar('"');
            dst.AppendBytes(ref value);
            dst.AppendChar('"');
        }
        
        public void ElementDouble(ref Bytes dst, double value) {
            AddSeparator();
            format.AppendDbl(ref dst, value);
        }
        
        public void ElementLong(ref Bytes dst, long value) {
            AddSeparator();
            format.AppendLong(ref dst, value);
        }
        
        public void ElementBool(ref Bytes dst, bool value) {
            AddSeparator();
            format.AppendBool(ref dst, value);
        }
        
        public void ElementNull(ref Bytes dst) {
            AddSeparator();
            dst.AppendString("null");
        }

    }
}
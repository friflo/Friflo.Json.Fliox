using System;
using Friflo.Json.Burst.Utils;

#if JSON_BURST
	using Str32 = Unity.Collections.FixedString32;
#else
    using Str32 = System.String;
#endif


namespace Friflo.Json.Burst
{
    public struct JsonEncoder : IDisposable
    {
        private ValueFormat             format;
        private ValueArray<bool>        firstEntry;
        private ValueArray<ElementType> elementType;
        private int                     level;

        enum ElementType {
            Object,
            Array
        }

        public void InitEncoder() {
            format.InitTokenFormat();
            if (!firstEntry.IsCreated())
                firstEntry = new ValueArray<bool>(32);
            if (!elementType.IsCreated())
                elementType = new ValueArray<ElementType>(32);
            level = 0;
            firstEntry[0] = true;
        }

        public void Dispose() {
            if (firstEntry.IsCreated())
                firstEntry.Dispose();
            if (elementType.IsCreated())
                elementType.Dispose();
            format.Dispose();
        }
        
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

        // Is called from ObjectStart() & ArrayStart() only, if (elementType[level] == ElementType.Array)
        private void AddSeparator(ref Bytes dst) {
            if (firstEntry[level]) {
                firstEntry[level] = false;
                return;
            }
            dst.AppendChar(',');
        }

        // ----------------------------- object with properties -----------------------------
        public void ObjectStart(ref Bytes dst) {
            if (elementType[level] == ElementType.Array)
                AddSeparator(ref dst);
            dst.AppendChar('{');
            firstEntry[++level] = true;
            elementType[level] = ElementType.Object;
        }
        
        public void ObjectEnd(ref Bytes dst) {
            dst.AppendChar('}');
            firstEntry[--level] = false;
        }

        // TODO implement version with Str32 key
        // public void PropertyString(ref Bytes dst, ref Str32 key, ref Bytes value) { }
        
        public void PropertyArray(ref Bytes dst, ref Bytes key) {
            AddSeparator(ref dst);
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendString("\":");
        }
        
        public void PropertyObject(ref Bytes dst, ref Bytes key) {
            AddSeparator(ref dst);
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendString("\":");
        }
        
        public void PropertyString(ref Bytes dst, ref Bytes key, ref Bytes value) {
            AddSeparator(ref dst);
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendString("\":\"");
            AppendEscString(ref dst, ref value);
            dst.AppendChar('"');
        }
        
        public void PropertyDouble(ref Bytes dst, ref Bytes key, double value) {
            AddSeparator(ref dst);
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);            dst.AppendString("\":");
            format.AppendDbl(ref dst, value);
        }
        
        public void PropertyLong(ref Bytes dst, ref Bytes key, long value) {
            AddSeparator(ref dst);
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendString("\":");
            format.AppendLong(ref dst, value);
        }
        
        public void PropertyBool(ref Bytes dst, ref Bytes key, bool value) {
            AddSeparator(ref dst);
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendString("\":");
            format.AppendBool(ref dst, value);
        }
        
        public void PropertyNull(ref Bytes dst, ref Bytes key) {
            AddSeparator(ref dst);
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendString("\":null");
        }
        
        // ----------------------------- array with elements -----------------------------
        public void ArrayStart(ref Bytes dst) {
            if (elementType[level] == ElementType.Array)
                AddSeparator(ref dst);
            dst.AppendChar('[');
            firstEntry[++level] = true;
            elementType[level] = ElementType.Array;
        }
        
        public void ArrayEnd(ref Bytes dst) {
            dst.AppendChar(']');
            firstEntry[--level] = false;
        }
        
        public void ElementString(ref Bytes dst, ref Bytes value) {
            AddSeparator(ref dst);
            dst.AppendChar('"');
            AppendEscString(ref dst, ref value);
            dst.AppendChar('"');
        }
        
        public void ElementDouble(ref Bytes dst, double value) {
            AddSeparator(ref dst);
            format.AppendDbl(ref dst, value);
        }
        
        public void ElementLong(ref Bytes dst, long value) {
            AddSeparator(ref dst);
            format.AppendLong(ref dst, value);
        }
        
        public void ElementBool(ref Bytes dst, bool value) {
            AddSeparator(ref dst);
            format.AppendBool(ref dst, value);
        }
        
        public void ElementNull(ref Bytes dst) {
            AddSeparator(ref dst);
            dst.AppendString("null");
        }
        
    }
}

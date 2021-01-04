using System;
using Friflo.Json.Burst.Utils;

#if JSON_BURST
	using Str32 = Unity.Collections.FixedString32;
#else
    using Str32 = System.String;
#endif

namespace Friflo.Json.Burst
{
    public partial struct JsonSerializer : IDisposable
    {
        public  Bytes			        dst;
        private ValueFormat             format;
        private ValueArray<bool>        firstEntry;
        private ValueArray<ElementType> elementType;
        private int                     level;
        private Str32                   @null;

        enum ElementType {
            Object,
            Array
        }

        public void InitEncoder() {
            if (!dst.buffer.IsCreated())
                dst.InitBytes(128);
            dst.Clear();
            format.InitTokenFormat();
            if (!firstEntry.IsCreated())
                firstEntry = new ValueArray<bool>(32);
            if (!elementType.IsCreated())
                elementType = new ValueArray<ElementType>(32);
            @null = "null"; 
            level = 0;
            firstEntry[0] = true;
        }

        public void Dispose() {
            if (firstEntry.IsCreated())
                firstEntry.Dispose();
            if (elementType.IsCreated())
                elementType.Dispose();
            if (dst.buffer.IsCreated())
                dst.Dispose();
            format.Dispose();
        }
        
        // --- comment to enable source alignment in WinMerge
        public static void AppendEscString(ref Bytes dst, ref Str32 src) {
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
        }

        // Is called from ObjectStart() & ArrayStart() only, if (elementType[level] == ElementType.Array)
        private void AddSeparator() {
            if (firstEntry[level]) {
                firstEntry[level] = false;
                return;
            }
            dst.AppendChar(',');
        }

        // ----------------------------- object with properties -----------------------------
        public void ObjectStart() {
            if (elementType[level] == ElementType.Array)
                AddSeparator();
            dst.AppendChar('{');
            firstEntry[++level] = true;
            elementType[level] = ElementType.Object;
        }
        
        public void ObjectEnd() {
            dst.AppendChar('}');
            firstEntry[--level] = false;
        }

        public void PropertyArray(ref Str32 key) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
        }
        
        public void PropertyObject(ref Str32 key) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
        }
        
        public void PropertyString(ref Str32 key, ref Bytes value) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
            dst.AppendChar('"');
            AppendEscString(ref dst, ref value);
            dst.AppendChar('"');
        }
        
        public void PropertyDouble(ref Str32 key, double value) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
            format.AppendDbl(ref dst, value);
        }
        
        public void PropertyLong(ref Str32 key, long value) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
            format.AppendLong(ref dst, value);
        }
        
        public void PropertyBool(ref Str32 key, bool value) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
            format.AppendBool(ref dst, value);
        }
        
        public void PropertyNull(ref Str32 key) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
            dst.AppendStr32(@null);
        }
        
        // ------------- non-ref Str32 Property...() versions for convenience  -------------
        public void PropertyArray(Str32 key) {
            PropertyArray(ref key);
        }
        
        public void PropertyObject(Str32 key) {
            PropertyObject(ref key);
        }
        
        public void PropertyString(Str32 key, ref Bytes value) {
            PropertyString(ref key, ref value);
        }
        
        public void PropertyDouble(Str32 key, double value) {
            PropertyDouble(ref key, value);
        }
        
        public void PropertyLong(Str32 key, long value) {
            PropertyLong(ref key, value);
        }
        
        public void PropertyBool(Str32 key, bool value) {
            PropertyBool(ref key, value);
        }
        
        public void PropertyNull(Str32 key) {
            PropertyNull(ref key);
        }

        // ----------------------------- array with elements -----------------------------
        public void ArrayStart() {
            if (elementType[level] == ElementType.Array)
                AddSeparator();
            dst.AppendChar('[');
            firstEntry[++level] = true;
            elementType[level] = ElementType.Array;
        }
        
        public void ArrayEnd() {
            dst.AppendChar(']');
            firstEntry[--level] = false;
        }
        
        public void ElementString(ref Bytes value) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref value);
            dst.AppendChar('"');
        }
        
        public void ElementDouble(double value) {
            AddSeparator();
            format.AppendDbl(ref dst, value);
        }
        
        public void ElementLong(long value) {
            AddSeparator();
            format.AppendLong(ref dst, value);
        }
        
        public void ElementBool(bool value) {
            AddSeparator();
            format.AppendBool(ref dst, value);
        }
        
        public void ElementNull() {
            AddSeparator();
            dst.AppendStr32(@null);
        }

    }
}

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
        public  Bytes                   dst;
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

        // --- comment to enable source alignment in WinMerge
        public void MemberArray(ref Str32 key) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
        }
        
        public void MemberObject(ref Str32 key) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
        }
        
        public void MemberString(ref Str32 key, ref Bytes value) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
            dst.AppendChar('"');
            AppendEscString(ref dst, ref value);
            dst.AppendChar('"');
        }
        
        public void MemberDouble(ref Str32 key, double value) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
            format.AppendDbl(ref dst, value);
        }
        
        public void MemberLong(ref Str32 key, long value) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
            format.AppendLong(ref dst, value);
        }
        
        public void MemberBool(ref Str32 key, bool value) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
            format.AppendBool(ref dst, value);
        }
        
        public void MemberNull(ref Str32 key) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
            dst.AppendStr32(ref @null);
        }
        
        // ------------- non-ref Str32 Member...() versions for convenience  -------------
        public void MemberArrayKey(Str32 key) {
            MemberArray(ref key);
        }
        
        public void MemberObjectKey(Str32 key) {
            MemberObject(ref key);
        }
        
        public void MemberString(Str32 key, ref Bytes value) {
            MemberString(ref key, ref value);
        }
        
        public void MemberDouble(Str32 key, double value) {
            MemberDouble(ref key, value);
        }
        
        public void MemberLong(Str32 key, long value) {
            MemberLong(ref key, value);
        }
        
        public void MemberBool(Str32 key, bool value) {
            MemberBool(ref key, value);
        }
        
        public void MemberNull(Str32 key) {
            MemberNull(ref key);
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
            dst.AppendStr32(ref @null);
        }
        
        // ----------------- utilities
        public bool WriteObject(ref JsonParser p) {
            ObjectStart();
            JsonEvent ev;
            do {
                ev = p.NextEvent();
                switch (ev) {
                    case JsonEvent.ArrayStart:
                        MemberArrayKey(ref p.key);
                        WriteArray(ref p);
                        break;
                    case JsonEvent.ObjectStart:
                        MemberObjectKey(ref p.key);
                        WriteObject(ref p);
                        break;
                    case JsonEvent.ValueString:
                        MemberString(ref p.key, ref p.value);
                        break;
                    case JsonEvent.ValueNumber:
                        AddSeparator();
                        dst.AppendChar('"');
                        AppendEscString(ref dst, ref p.key);
                        dst.AppendChar2('\"', ':');
                        /*
                        // Conversion to long or double is expensive and not required 
                        if (p.isFloat)
                            MemberDouble(ref p.key, p.ValueAsDouble(out _));
                        else
                            MemberLong(ref p.key, p.ValueAsLong(out _));
                        */
                        dst.AppendBytes(ref p.value);
                        break;
                    case JsonEvent.ValueBool:
                        MemberBool(ref p.key, p.boolValue);
                        break;
                    case JsonEvent.ValueNull:
                        MemberNull(ref p.key);
                        break;
                    case JsonEvent.ObjectEnd:
                        ObjectEnd();
                        return true;
                    case JsonEvent.ArrayEnd:
                        // unreachable
                        return false;
                    case JsonEvent.Error:
                    case JsonEvent.EOF:
                        return false;
                }
            }
            while (p.ContinueObject(ev));
            
            return true;
        }
        
        public bool WriteArray(ref JsonParser p) {
            ArrayStart();
            JsonEvent ev;
            do {
                ev = p.NextEvent();
                switch (ev) {
                    case JsonEvent.ArrayStart:
                        WriteArray(ref p);
                        break;
                    case JsonEvent.ObjectStart:
                        WriteObject(ref p);
                        break;
                    case JsonEvent.ValueString:
                        ElementString(ref p.value);
                        break;
                    case JsonEvent.ValueNumber:
                        AddSeparator();
                        dst.AppendBytes(ref p.value);
                        break;
                    case JsonEvent.ValueBool:
                        ElementBool(p.boolValue);
                        break;
                    case JsonEvent.ValueNull:
                        ElementNull();
                        break;
                    case JsonEvent.ObjectEnd:
                        // unreachable
                        return false;
                    case JsonEvent.ArrayEnd:
                        ArrayEnd();
                        return true;
                    case JsonEvent.Error:
                    case JsonEvent.EOF:
                        return false;
                }
            }
            while (p.ContinueArray(ev));
            
            return true;
        }

        public bool WriteTree(ref JsonParser p) {
            JsonEvent ev = p.NextEvent();
            switch (ev) {
                case JsonEvent.ObjectStart:
                    return WriteObject(ref p);
                case JsonEvent.ArrayStart:
                    return WriteArray(ref p);
                case JsonEvent.ValueString:
                    ElementString(ref p.value);
                    return true;
                case JsonEvent.ValueNumber:
                    dst.AppendBytes(ref p.value);
                    return true;
                case JsonEvent.ValueBool:
                    ElementBool(p.boolValue);
                    return true;
                case JsonEvent.ValueNull:
                    ElementNull();
                    return true;
            }
            return false;
        }
    }
}

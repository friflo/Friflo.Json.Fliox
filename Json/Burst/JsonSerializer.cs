using System;
using Friflo.Json.Burst.Utils;

#if JSON_BURST
    using Str32 = Unity.Collections.FixedString32;
    using Str128 = Unity.Collections.FixedString128;
#else
    using Str32 = System.String;
#endif

namespace Friflo.Json.Burst
{
    /// <summary>
    /// The JSON serializer used to create a JSON document by using a set of appender methods to add
    /// JSON objects, object members (key/value pairs), arrays and array elements.<br/>
    ///
    /// Before using the serializer it need to be initialized with <see cref="InitSerializer()"/><br/>
    /// 
    /// To add a JSON object use <see cref="ObjectStart()"/>.
    /// Afterwards arbitrary object members can be added via the Member...() methods.<br/>
    /// E.g by <see cref="MemberLong(ref string,long)"/> to add a key/value pair using an integer as value type
    /// like { "count": 11 }<br/>
    /// After all object members are serialized <see cref="ObjectEnd()"/> closes the previous started JSON object.<br/>
    ///
    /// To add a JSON array use <see cref="ArrayStart()"/>
    /// Afterwards arbitrary array elements can be added via the Element...() methods.<br/>
    /// E.g by <see cref="ElementLong(long)"/> to add an element with an integer as value type
    /// like [ 11 ]<br/>
    /// After all array elements are serialized <see cref="ArrayEnd()"/> closes the previous started JSON array.<br/>
    ///
    /// After creating the JSON document by using the appender methods, the JSON document is available
    /// via <exception cref="dst"></exception> 
    /// </summary>
    public partial struct JsonSerializer : IDisposable
    {
        /// <summary>Contains the generated JSON document as <see cref="Bytes"/>.</summary>
        public  Bytes                   dst;
        private ValueFormat             format;
        private ValueArray<bool>        firstEntry;
        private ValueArray<ElementType> elementType;
        private Bytes                   strBuf;
        private int                     level;
        private Str32                   @null;

        enum ElementType {
            Object,
            Array
        }

        /// <summary>
        /// Before starting serializing a JSON document the serializer need to be initialized with this method to
        /// create internal buffers.
        /// </summary>
        public void InitSerializer() {
            if (!dst.buffer.IsCreated())
                dst.InitBytes(128);
            dst.Clear();
            format.InitTokenFormat();
            if (!firstEntry.IsCreated())
                firstEntry = new ValueArray<bool>(32);
            if (!elementType.IsCreated())
                elementType = new ValueArray<ElementType>(32);
            if (!strBuf.buffer.IsCreated())
                strBuf.InitBytes(128);
            @null = "null"; 
            level = 0;
            firstEntry[0] = true;
        }

        /// <summary>
        /// Dispose all internal used buffers.
        /// Only required when running with JSON_BURST within Unity. 
        /// </summary>
        public void Dispose() {
            if (firstEntry.IsCreated())
                firstEntry.Dispose();
            if (strBuf.buffer.IsCreated())
                strBuf.Dispose();
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
        /// <summary>Start a JSON object for serialization</summary>
        public void ObjectStart() {
            if (elementType[level] == ElementType.Array)
                AddSeparator();
            dst.AppendChar('{');
            firstEntry[++level] = true;
            elementType[level] = ElementType.Object;
        }
        
        /// <summary>Finished a previous started JSON object for serialization</summary>
        public void ObjectEnd() {
            dst.AppendChar('}');
            firstEntry[--level] = false;
        }

        // --- comment to enable source alignment in WinMerge
        /// <summary>Writes the key of key/value pair where the value will be an array</summary>
        public void MemberArray(ref Str32 key) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
        }
        
        /// <summary>Writes the key of key/value pair where the value will be an object</summary>
        public void MemberObject(ref Str32 key) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
        }
        
        /// <summary>Writes a key/value pair where the value is a "string"</summary>
        public void MemberString(ref Str32 key, ref Bytes value) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
            dst.AppendChar('"');
            AppendEscString(ref dst, ref value);
            dst.AppendChar('"');
        }
        
        /// <summary>
        /// Writes a key/value pair where the value is a <see cref="string"/><br/>
        /// </summary>
#if JSON_BURST
        public void MemberString(Str32 key, Str128 value) {
            strBuf.Clear();
            strBuf.AppendStr128(ref value);
            MemberString(ref key, ref strBuf);
        }
#else
        public void MemberString(string key, string value) {
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
        public void MemberDouble(ref Str32 key, double value) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
            format.AppendDbl(ref dst, value);
        }
        
        /// <summary>Writes a key/value pair where the value is a <see cref="long"/></summary>
        public void MemberLong(ref Str32 key, long value) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
            format.AppendLong(ref dst, value);
        }
        
        /// <summary>Writes a key/value pair where the value is a <see cref="bool"/></summary>
        public void MemberBool(ref Str32 key, bool value) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
            format.AppendBool(ref dst, value);
        }
        
        /// <summary>Writes a key/value pair where the value is null</summary>
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
        
        /// <summary>Write an array element of type "string"</summary>
        public void ElementString(ref Bytes value) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref value);
            dst.AppendChar('"');
        }
        
#if !JSON_BURST
        /// <summary>Write an array element of type <see cref="string"/></summary>
        public void ElementString(string value) {
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref value);
            dst.AppendChar('"');
        }
#endif
        /// <summary>Write an array element of type <see cref="double"/></summary>
        public void ElementDouble(double value) {
            AddSeparator();
            format.AppendDbl(ref dst, value);
        }
        
        /// <summary>Write an array element of type <see cref="long"/></summary>
        public void ElementLong(long value) {
            AddSeparator();
            format.AppendLong(ref dst, value);
        }
        
        /// <summary>Write an array element of type <see cref="bool"/></summary>
        public void ElementBool(bool value) {
            AddSeparator();
            format.AppendBool(ref dst, value);
        }
        
        /// <summary>Writes null as array element</summary>
        public void ElementNull() {
            AddSeparator();
            dst.AppendStr32(ref @null);
        }
        
        // ----------------- utilities
        public bool WriteObject(ref JsonParser p) {
            ObjectStart();
            while (p.NoSkipNextObjectMember()) {
                switch (p.Event) {
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
                    case JsonEvent.ArrayEnd:
                    case JsonEvent.Error:
                    case JsonEvent.EOF:
                        throw new InvalidOperationException("WriteObject() unreachable"); // because of behaviour of ContinueObject()
                }
            }

            switch (p.Event) {
                case JsonEvent.ObjectEnd:
                    ObjectEnd();
                    break;
                case JsonEvent.Error:
                case JsonEvent.EOF:
                    return false;
            }
            return true;
        }
        
        public bool WriteArray(ref JsonParser p) {
            ArrayStart();
            while (p.NoSkipNextArrayElement()) {
                switch (p.Event) {
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
                    case JsonEvent.ArrayEnd:
                    case JsonEvent.Error:
                    case JsonEvent.EOF:
                        throw new InvalidOperationException("WriteArray() unreachable");  // because of behaviour of ContinueArray()
                }
            }
            switch (p.Event) {
                case JsonEvent.ArrayEnd:
                    ArrayEnd();
                    break;
                case JsonEvent.Error:
                case JsonEvent.EOF:
                    return false;
            }
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

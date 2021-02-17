using System;
using System.Diagnostics;
using System.Text;
using Friflo.Json.Burst.Utils;

#if JSON_BURST
    using Str32 = Unity.Collections.FixedString32;
#else
    using Str32 = System.String;
    // ReSharper disable InconsistentNaming
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
    /// E.g by <see cref="MemberLng"/> to add a key/value pair using an integer as value type
    /// like { "count": 11 }<br/>
    /// After all object members are serialized <see cref="ObjectEnd()"/> closes the previous started JSON object.<br/>
    ///
    /// To add a JSON array use <see cref="ArrayStart"/>
    /// Afterwards arbitrary array elements can be added via the Element...() methods.<br/>
    /// E.g by <see cref="ElementLng"/> to add an element with an integer as value type
    /// like [ 11 ]<br/>
    /// After all array elements are serialized <see cref="ArrayEnd()"/> closes the previous started JSON array.<br/>
    ///
    /// After creating the JSON document by using the appender methods, the JSON document is available
    /// via <exception cref="json"></exception> 
    /// </summary>
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public partial struct JsonSerializer : IDisposable
    {
        /// <summary>Contains the generated JSON document as <see cref="Bytes"/>.</summary>
        public      Bytes                   json;
        private     ValueFormat             format;
        private     ValueList<NodeFlags>    nodeFlags;
        private     ValueList<NodeType>     nodeType;
        private     Bytes                   strBuf;
        private     int                     level;
        private     int                     maxDepth;
        private     Str32                   @null;
        private     bool                    pretty;
        
        public      int                     Level => level;
        
        public      int                     MaxDepth => maxDepth;
        public      void                    SetMaxDepth(int size)  { maxDepth= size; }
        
        public      bool                    Pretty => pretty;
        public      void                    SetPretty(bool pretty) { this.pretty= pretty; }
        
#pragma warning disable 649  // Field 'startGuard' is never assigned
        private     ValueList<bool>         startGuard;
#pragma warning restore 649

        enum NodeType {
            Undefined,
            Object,
            Array
        }
        
        [Flags]
        enum NodeFlags {
            None = 0,
            First = 1,
            WrapItems = 2,
        }

        /// <summary>
        /// Before starting serializing a JSON document the serializer need to be initialized with this method to
        /// create internal buffers.
        /// </summary>
        public void InitSerializer() {
            if (!json.buffer.IsCreated())
                json.InitBytes(128);
            json.Clear();
            format.InitTokenFormat();
            int initDepth = 16;
            maxDepth = JsonParser.DefaultMaxDepth;
            if (!nodeFlags.IsCreated())
                nodeFlags = new ValueList<NodeFlags>  (initDepth, AllocType.Persistent); nodeFlags.Resize(initDepth);
#if DEBUG
            if (!startGuard.IsCreated())
                startGuard = new ValueList<bool>  (initDepth, AllocType.Persistent); startGuard.Resize(initDepth);
#endif
            if (!nodeType.IsCreated())
                nodeType = new ValueList<NodeType>(initDepth, AllocType.Persistent); nodeType.  Resize(initDepth);
            if (!strBuf.buffer.IsCreated())
                strBuf.InitBytes(128);
            @null = "null"; 
            level = 0;
            nodeFlags.array[0] = NodeFlags.First | NodeFlags.WrapItems;
            nodeType.array[0] = NodeType.Undefined;
        }

        private void ResizeDepthBuffers(int size) {
            // resizing to size is enough, but allocate more in advance
            size *= 2;
            nodeFlags.Resize(size);
            startGuard.Resize(size);
            nodeType.Resize(size);
        }
        
        [Conditional("DEBUG")]
        private void AssertMember() {
            if (level == 0)
                throw new InvalidOperationException("Member...() methods and ObjectEnd() must not be called on root level");
            startGuard.array[level] = false;
            if (nodeType.array[level] == NodeType.Object)
                return;
            throw new InvalidOperationException("Member...() methods and ObjectEnd() must be called only within an object");
        }
        
        [Conditional("DEBUG")]
        private void AssertElement() {
            if (level == 0 || nodeType.array[level] == NodeType.Array)
                return;
            throw new InvalidOperationException("Element...() methods and ArrayEnd() must be called only within an array or on root level");
        }
        
        [Conditional("DEBUG")]
        private void AssertStart() {
            if (level > 0 && startGuard.array[level] == false)
                throw new InvalidOperationException("ObjectStart() and ArrayStart() requires a previous call to a ...Start() method");
        }
        
        [Conditional("DEBUG")]
        private void SetStartGuard() {
            startGuard.array[level] = true;
        }
        
        [Conditional("DEBUG")]
        private void ClearStartGuard() {
            startGuard.array[level] = false;
        }
        
        /// <summary>
        /// Dispose all internal used buffers.
        /// Only required when running with JSON_BURST within Unity. 
        /// </summary>
        public void Dispose() {
#if DEBUG
            if (startGuard.IsCreated())
                startGuard.Dispose();
#endif            
            if (nodeFlags.IsCreated())
                nodeFlags.Dispose();
            if (strBuf.buffer.IsCreated())
                strBuf.Dispose();
            if (nodeType.IsCreated())
                nodeType.Dispose();
            if (json.buffer.IsCreated())
                json.Dispose();
            format.Dispose();
        }
        
        // [UTF-16 - Wikipedia] https://en.wikipedia.org/wiki/UTF-16
        private const int SupplementaryPlanesStart  = 0x10000;
        
        private const char HighSurrogateStart       = '\ud800';
        private const char LowSurrogateStart        = '\udc00';
        private const char SurrogateEnd             = '\ue000';
        
        private const int  HighSurrogateLimit  = LowSurrogateStart - HighSurrogateStart;

        

        //         Tight loop! Avoid calling any trivial method
        // --- comment to enable source alignment in WinMerge
        public static void AppendEscString(ref Bytes dst, in string src) {
            int maxByteLen = Encoding.UTF8.GetMaxByteCount(src.Length) + 2; // + 2 * '"'
            dst.EnsureCapacityAbs(dst.end + maxByteLen);
#if UNITY_5_3_OR_NEWER
            var span = src;
#else
            ReadOnlySpan<char> span = src;
#endif
            int end = src.Length;
            
            ref var dstArr = ref dst.buffer.array;
            var srcSpan = span;
            
            // --- bounds checks degrade performance => used managed arrays
            // fixed (byte* dstArr = &dst.buffer.array[0])
            // fixed (char* srcSpan = &span[0])
            {
                dstArr[dst.end++] = (byte) '"';

                for (int index = 0; index < end; index++) {
                    int uni = srcSpan[index];
                    int surrogate = uni - HighSurrogateStart;
                    // Is surrogate?
                    if (0 <= surrogate && surrogate < SurrogateEnd - HighSurrogateStart) {
                        // found surrogate
                        if (surrogate < HighSurrogateLimit) {
                            // found high surrogate.
                            if (index < end - 1) {
                                int lowSurrogate = srcSpan[++index] - LowSurrogateStart;
                                if (0 <= lowSurrogate && lowSurrogate < HighSurrogateLimit) {
                                    // found low surrogate.
                                    uni = surrogate * 0x400 + lowSurrogate + SupplementaryPlanesStart;
                                } else {
                                    throw new ArgumentException("Invalid high surrogate. " + src);
                                }
                            } else {
                                throw new ArgumentException("Unexpected high surrogate at string end. " + src);
                            }
                        } else {
                            throw new ArgumentException("Unexpected low surrogate at index: " + index);
                        }
                    }
                    switch (uni) {
                        case '"':  dstArr[dst.end++] = (byte) '\\'; dstArr[dst.end++] = (byte) '\"'; break;
                        case '\\': dstArr[dst.end++] = (byte) '\\'; dstArr[dst.end++] = (byte) '\\'; break;
                        case '\b': dstArr[dst.end++] = (byte) '\\'; dstArr[dst.end++] = (byte) 'b'; break;
                        case '\f': dstArr[dst.end++] = (byte) '\\'; dstArr[dst.end++] = (byte) 'f'; break;
                        case '\r': dstArr[dst.end++] = (byte) '\\'; dstArr[dst.end++] = (byte) 'r'; break;
                        case '\n': dstArr[dst.end++] = (byte) '\\'; dstArr[dst.end++] = (byte) 'n'; break;
                        case '\t': dstArr[dst.end++] = (byte) '\\'; dstArr[dst.end++] = (byte) 't'; break;
                        default:
                            Utf8Utils.AppendUnicodeToBytes(ref dst, uni);
                            break;
                    }
                }
                dstArr[dst.end++] = (byte) '"';
            }
        }
        
        private void IndentBegin(NodeFlags flags) {
            if ((flags & NodeFlags.WrapItems) == 0) {
                if ((flags & NodeFlags.First) != 0)
                    return;
                json.EnsureCapacityAbs(json.end + 1);
                json.buffer.array[json.end++] = (byte) ' ';
                return;
            }
            json.EnsureCapacityAbs(json.end + level + 1);
            json.buffer.array[json.end++] = (byte) '\n';
            for (int n = 0; n < level; n++)
                json.buffer.array[json.end++] = (byte) '\t';
        }
        
        private void IndentEnd() {
            if ((nodeFlags.array[level + 1] & NodeFlags.WrapItems) == 0)
                return;
            json.EnsureCapacityAbs(json.end + level + 1);
            json.buffer.array[json.end++] = (byte)'\n';
            for (int n = 0; n < level; n++)
                json.buffer.array[json.end++] = (byte)'\t';
        }

        private void AppendKeyString(ref Bytes dst, in Str32 key) {
            AppendEscString(ref dst, in key);
            if (!pretty) {
                dst.AppendChar(':');
            } else {
                dst.AppendChar(':');
                dst.AppendChar(' ');
            }
        }


        // Is called from ObjectStart() & ArrayStart() only, if (elementType[level] == ElementType.Array)
        private void AddSeparator() {
            var flags = nodeFlags.array[level];
            if ((flags & NodeFlags.First) != 0) {
                nodeFlags.array[level] = flags & ~NodeFlags.First;
                if (pretty)
                    IndentBegin(flags);
                return;
            }
            json.AppendChar(',');
            if (pretty)
                IndentBegin(flags);
        }
        
        // ----------------------------- object with properties -----------------------------
        /// <summary>Start a JSON object for serialization</summary>
        public void ObjectStart() {
            AssertStart();
            if (nodeType.array[level] == NodeType.Array)
                AddSeparator();
            json.AppendChar('{');
            if (level >= maxDepth)
                throw new InvalidOperationException("JsonSerializer exceed maxDepth: " + maxDepth);
            if (++level >= nodeFlags.Count)
                ResizeDepthBuffers(level + 1);
            nodeFlags.array[level] = NodeFlags.First | NodeFlags.WrapItems;
            nodeType.array[level] = NodeType.Object;
            ClearStartGuard();
        }
        
        /// <summary>Finished a previous started JSON object for serialization</summary>
        public void ObjectEnd() {
            AssertMember();
            level--;
            if (pretty)
                IndentEnd();
            json.AppendChar('}');
            nodeFlags.array[level] &= ~NodeFlags.First;
        }

        // --- comment to enable source alignment in WinMerge
        /// <summary>Writes the key of key/value pair where the value will be an array</summary>
        public void MemberArrayStart(in Str32 key, bool wrapItems = true) {
            AssertMember();
            AddSeparator();
            AppendKeyString(ref json, in key);
            SetStartGuard();
            ArrayStart(wrapItems);
        }
        
        /// <summary>Writes the key of key/value pair where the value will be an object</summary>
        public void MemberObjectStart(in Str32 key) {
            AssertMember();
            AddSeparator();
            AppendKeyString(ref json, in key);
            SetStartGuard();
            ObjectStart();
        }

        /// <summary>Writes a key/value pair where the value is a "string"</summary>
        public void MemberStr(in Str32 key, in Bytes value) {
            AssertMember();
            AddSeparator();
            AppendKeyString(ref json, in key);
            AppendEscStringBytes(ref json, in value);
        }

        /// <summary>
        /// Writes a key/value pair where the value is a <see cref="string"/><br/>
        /// </summary>
#if JSON_BURST
        public void MemberStr(in Str32 key, in Unity.Collections.FixedString128 value) {
            strBuf.Clear();
            strBuf.AppendStr128(in value);
            MemberStr(in key, in strBuf);
        }
#else
        public void MemberStr(in string key, in string value) {
            AssertMember();
            AddSeparator();
            AppendKeyString(ref json, in key);
            AppendEscString(ref json, in value);
        }
#endif
        /// <summary>Writes a key/value pair where the value is a <see cref="double"/></summary>
        public void MemberDbl(in Str32 key, double value) {
            AssertMember();
            AddSeparator();
            AppendKeyString(ref json, in key);
            format.AppendDbl(ref json, value);
        }
        
        /// <summary>Writes a key/value pair where the value is a <see cref="long"/></summary>
        public void MemberLng(in Str32 key, long value) {
            AssertMember();
            AddSeparator();
            AppendKeyString(ref json, in key);
            format.AppendLong(ref json, value);
        }
        
        /// <summary>Writes a key/value pair where the value is a <see cref="bool"/></summary>
        public void MemberBln(in Str32 key, bool value) {
            AssertMember();
            AddSeparator();
            AppendKeyString(ref json, in key);
            format.AppendBool(ref json, value);
        }
        
        /// <summary>Writes a key/value pair where the value is null</summary>
        public void MemberNul(in Str32 key) {
            AssertMember();
            AddSeparator();
            AppendKeyString(ref json, in key);
            json.AppendStr32(in @null);
        }

        // ----------------------------- array with elements -----------------------------
        public void ArrayStart(bool wrapItems = true) {
            AssertStart();
            if (nodeType.array[level] == NodeType.Array)
                AddSeparator();
            json.AppendChar('[');
            if (level >= maxDepth)
                throw new InvalidOperationException("JsonSerializer exceed maxDepth: " + maxDepth);
            if (++level >= nodeFlags.Count)
                ResizeDepthBuffers(level + 1);
            nodeFlags.array[level] = wrapItems ? NodeFlags.WrapItems | NodeFlags.First : NodeFlags.First;
            nodeType.array[level] = NodeType.Array;
            SetStartGuard();
        }
        
        public void ArrayEnd() {
            if (level == 0)
                throw new InvalidOperationException("ArrayEnd...() must not be called below root level");
            AssertElement();
            level--;
            if (pretty)
                IndentEnd();
            json.AppendChar(']');
            nodeFlags.array[level] &= ~NodeFlags.First;
        }
        
        /// <summary>Write an array element of type "string"</summary>
        public void ElementStr(in Bytes value) {
            AssertElement();
            AddSeparator();
            AppendEscStringBytes(ref json, in value);
        }
        
#if JSON_BURST
        public void ElementStr(in Unity.Collections.FixedString128 value) {
            strBuf.Clear();
            strBuf.AppendStr128(in value);
            AssertElement();
            AddSeparator();
            AppendEscStringBytes(ref json, in strBuf);
        }
#else
        /// <summary>Write an array element of type <see cref="string"/></summary>
        public void ElementStr(in string value) {
            AssertElement();
            AddSeparator();
            AppendEscString(ref json, in value);
        }
#endif
        /// <summary>Write an array element of type <see cref="double"/></summary>
        public void ElementDbl(double value) {
            AssertElement();
            AddSeparator();
            format.AppendDbl(ref json, value);
        }
        
        /// <summary>Write an array element of type <see cref="long"/></summary>
        public void ElementLng(long value) {
            AssertElement();
            AddSeparator();
            format.AppendLong(ref json, value);
        }
        
        /// <summary>Write an array element of type <see cref="bool"/></summary>
        public void ElementBln(bool value) {
            AssertElement();
            AddSeparator();
            format.AppendBool(ref json, value);
        }
        
        /// <summary>Writes null as array element</summary>
        public void ElementNul() {
            AssertElement();
            AddSeparator();
            json.AppendStr32(in @null);
        }
        
        // ----------------- utilities
        public bool WriteObject(ref JsonParser p) {
            while (NextObjectMember(ref p)) {
                switch (p.Event) {
                    case JsonEvent.ArrayStart:
                        MemberArrayStart(in p.key);
                        WriteArray(ref p);
                        break;
                    case JsonEvent.ObjectStart:
                        MemberObjectStart(in p.key);
                        WriteObject(ref p);
                        break;
                    case JsonEvent.ValueString:
                        MemberStr(in p.key, in p.value);
                        break;
                    case JsonEvent.ValueNumber:
                        AddSeparator();
                        AppendKeyBytes(ref json, in p.key);
                        /*
                        // Conversion to long or double is expensive and not required 
                        if (p.isFloat)
                            MemberDouble(ref p.key, p.ValueAsDouble(out _));
                        else
                            MemberLong(ref p.key, p.ValueAsLong(out _));
                        */
                        json.AppendBytes(ref p.value);
                        break;
                    case JsonEvent.ValueBool:
                        MemberBln(in p.key, p.boolValue);
                        break;
                    case JsonEvent.ValueNull:
                        MemberNul(in p.key);
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
            while (NextArrayElement(ref p)) {
                switch (p.Event) {
                    case JsonEvent.ArrayStart:
                        ArrayStart();
                        WriteArray(ref p);
                        break;
                    case JsonEvent.ObjectStart:
                        ObjectStart();
                        WriteObject(ref p);
                        break;
                    case JsonEvent.ValueString:
                        ElementStr(in p.value);
                        break;
                    case JsonEvent.ValueNumber:
                        AddSeparator();
                        json.AppendBytes(ref p.value);
                        break;
                    case JsonEvent.ValueBool:
                        ElementBln(p.boolValue);
                        break;
                    case JsonEvent.ValueNull:
                        ElementNul();
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
                    ObjectStart();
                    return WriteObject(ref p);
                case JsonEvent.ArrayStart:
                    ArrayStart();
                    return WriteArray(ref p);
                case JsonEvent.ValueString:
                    ElementStr(in p.value);
                    return true;
                case JsonEvent.ValueNumber:
                    json.AppendBytes(ref p.value);
                    return true;
                case JsonEvent.ValueBool:
                    ElementBln(p.boolValue);
                    return true;
                case JsonEvent.ValueNull:
                    ElementNul();
                    return true;
            }
            return false;
        }
        
        private static bool NextObjectMember (ref JsonParser p) {
            JsonEvent ev = p.NextEvent();
            switch (ev) {
                case JsonEvent.ValueString:
                case JsonEvent.ValueNumber:
                case JsonEvent.ValueBool:
                case JsonEvent.ValueNull:
                case JsonEvent.ObjectStart:
                case JsonEvent.ArrayStart:
                    return true;
                case JsonEvent.ArrayEnd:
                    throw new InvalidOperationException("unexpected ArrayEnd in NoSkipNextObjectMember()");
                case JsonEvent.ObjectEnd:
                    break;
            }
            return false;
        }
        
        private static bool NextArrayElement (ref JsonParser p) {
            JsonEvent ev = p.NextEvent();
            switch (ev) {
                case JsonEvent.ValueString:
                case JsonEvent.ValueNumber:
                case JsonEvent.ValueBool:
                case JsonEvent.ValueNull:
                case JsonEvent.ObjectStart:
                case JsonEvent.ArrayStart:
                    return true;
                case JsonEvent.ArrayEnd:
                    break;
                case JsonEvent.ObjectEnd:
                    throw new InvalidOperationException("unexpected ObjectEnd in NoSkipNextArrayElement()");
            }
            return false;
        }

    }
}

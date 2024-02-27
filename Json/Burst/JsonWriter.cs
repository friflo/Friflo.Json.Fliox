// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics;
using System.Text;
using Friflo.Json.Burst.Utils;

// JSON_BURST_TAG
using Str32 = System.String;

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
    /// E.g by <c>MemberLng()</c> to add a key/value pair using an integer as value type
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
    [CLSCompliant(true)]
    public partial struct Utf8JsonWriter : IDisposable
    {
        static Utf8JsonWriter() { BurstLog.InitialBurstLog(); }
        
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
            if (!json.IsCreated())
                json.InitBytes(128);
            json.Clear();
            format.InitTokenFormat();
            int initDepth = 16;
            maxDepth = Utf8JsonParser.DefaultMaxDepth;
            if (!nodeFlags.IsCreated())
                nodeFlags = new ValueList<NodeFlags>  (initDepth, AllocType.Persistent); nodeFlags.Resize(initDepth);
#if DEBUG
            if (!startGuard.IsCreated())
                startGuard = new ValueList<bool>  (initDepth, AllocType.Persistent); startGuard.Resize(initDepth);
#endif
            if (!nodeType.IsCreated())
                nodeType = new ValueList<NodeType>(initDepth, AllocType.Persistent); nodeType.  Resize(initDepth);
            if (!strBuf.IsCreated())
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
            if (strBuf.IsCreated())
                strBuf.Dispose();
            if (nodeType.IsCreated())
                nodeType.Dispose();
            if (json.IsCreated())
                json.Dispose();
            format.Dispose();
        }
        
        // [UTF-16 - Wikipedia] https://en.wikipedia.org/wiki/UTF-16
        private const int SupplementaryPlanesStart  = 0x10000;
        
        private const char HighSurrogateStart       = '\ud800';
        private const char LowSurrogateStart        = '\udc00';
        private const char SurrogateEnd             = '\ue000';
        
        private const int  HighSurrogateLimit  = LowSurrogateStart - HighSurrogateStart;

        private static readonly Encoding Utf8 = Encoding.UTF8;

        //         Tight loop! Avoid calling any trivial method
        // --- comment to enable source alignment in WinMerge
        public static void AppendEscString(ref Bytes dst, in ReadOnlySpan<char> span) {
            int maxByteLen = Utf8.GetMaxByteCount(span.Length) + 2; // + 2 * '"'
            dst.EnsureCapacityAbs(dst.end + maxByteLen);
            int end = span.Length;
            
            ref var dstArr = ref dst.buffer; // could be without ref
            
            // --- bounds checks degrade performance => used managed arrays
            // fixed (byte* dstArr = &dst.buffer.array[0])
            // fixed (char* srcSpan = &span[0])
            {
                dstArr[dst.end++] = (byte) '"';

                for (int index = 0; index < end; index++) {
                    int uni = span[index];
                    int surrogate = uni - HighSurrogateStart;
                    // Is surrogate?
                    if (0 <= surrogate && surrogate < SurrogateEnd - HighSurrogateStart) {
                        // found surrogate
                        if (surrogate < HighSurrogateLimit) {
                            // found high surrogate.
                            if (index < end - 1) {
                                int lowSurrogate = span[++index] - LowSurrogateStart;
                                if (0 <= lowSurrogate && lowSurrogate < HighSurrogateLimit) {
                                    // found low surrogate.
                                    uni = surrogate * 0x400 + lowSurrogate + SupplementaryPlanesStart;
                                } else {
                                    throw new ArgumentException("Invalid high surrogate. " + span.ToString());
                                }
                            } else {
                                throw new ArgumentException("Unexpected high surrogate at string end. " + span.ToString());
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
                json.buffer[json.end++] = (byte) ' ';
                return;
            }
            IndentJsonNode(ref json, level);
        }

        private void IndentEnd() {
            if ((nodeFlags.array[level + 1] & NodeFlags.WrapItems) == 0)
                return;
            IndentJsonNode(ref json, level);
        }
        
        public static void IndentJsonNode(ref Bytes json, int level) {
            json.EnsureCapacityAbs(json.end + 1 + 4 * level); // LF + level * four spaces
            json.buffer[json.end++] = (byte)'\n';
            for (int n = 0; n < level; n++) {
                json.buffer[json.end++] = (byte) ' ';
                json.buffer[json.end++] = (byte) ' ';
                json.buffer[json.end++] = (byte) ' ';
                json.buffer[json.end++] = (byte) ' ';
            }
        }

        private void AppendKeyString(ref Bytes dst, Str32 key) {
            AppendEscString(ref dst, key.AsSpan());
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
                throw new InvalidOperationException("JsonWriter exceed maxDepth: " + maxDepth);
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
        public void MemberArrayStart(Str32 key, bool wrapItems) {
            AssertMember();
            AddSeparator();
            AppendKeyString(ref json, key);
            SetStartGuard();
            ArrayStart(wrapItems);
        }
        
        /// <summary>Writes the key of key/value pair where the value will be an object</summary>
        public void MemberObjectStart(Str32 key) {
            AssertMember();
            AddSeparator();
            AppendKeyString(ref json, key);
            SetStartGuard();
            ObjectStart();
        }

        /// <summary>Writes a key/value pair where the value is a "string"</summary>
        public void MemberStr(Str32 key, ReadOnlySpan<byte> value) {
            AssertMember();
            AddSeparator();
            AppendKeyString(ref json, key);
            AppendEscStringBytes(ref json, value);
        }

        /// <summary>
        /// Writes a key/value pair where the value is a <see cref="string"/><br/>
        /// </summary>
        public void MemberStr(string key, string value) {
            AssertMember();
            AddSeparator();
            AppendKeyString(ref json, key);
            AppendEscString(ref json, value.AsSpan());
        }
        
        /// <summary>Writes a key/value pair where the value is a <see cref="double"/></summary>
        public void MemberDbl(Str32 key, double value) {
            AssertMember();
            AddSeparator();
            AppendKeyString(ref json,  key);
            format.AppendDbl(ref json, value);
        }
        
        /// <summary>Writes a key/value pair where the value is a <see cref="long"/></summary>
        public void MemberLng(Str32 key, long value) {
            AssertMember();
            AddSeparator();
            AppendKeyString(ref json,   key);
            format.AppendLong(ref json, value);
        }
        
        /// <summary>Writes a key/value pair where the value is a "string"</summary>
        public void MemberBytes(ReadOnlySpan<byte> key, in Bytes value) {
            AddSeparator();
            AppendKeyBytes(ref json, key);
            /*
            // Conversion to long or double is expensive and not required 
            if (p.isFloat)
                MemberDouble(ref key, ValueAsDouble(out _));
            else
                MemberLong(ref key, ValueAsLong(out _));
            */
            json.AppendBytes(value);
        }
        
        /// <summary>Writes a key/value pair where the value is a <see cref="bool"/></summary>
        public void MemberBln(Str32 key, bool value) {
            AssertMember();
            AddSeparator();
            AppendKeyString(ref json,   key);
            format.AppendBool(ref json, value);
        }
        
        /// <summary>Writes a key/value pair where the value is null</summary>
        public void MemberNul(Str32 key) {
            AssertMember();
            AddSeparator();
            AppendKeyString(ref json, key);
            json.AppendStr32(@null);
        }
        
        // ----------------------------- array with elements -----------------------------
        public void ArrayStart(bool wrapItems) {
            AssertStart();
            if (nodeType.array[level] == NodeType.Array)
                AddSeparator();
            json.AppendChar('[');
            if (level >= maxDepth)
                throw new InvalidOperationException("JsonWriter exceed maxDepth: " + maxDepth);
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
        public void ElementStr(ReadOnlySpan<byte> value) {
            AssertElement();
            AddSeparator();
            AppendEscStringBytes(ref json, value);
        }

        /// <summary>Write an array element of type <see cref="string"/></summary>
        public void ElementStr(string value) {
            AssertElement();
            AddSeparator();
            AppendEscString(ref json, value.AsSpan());
        }

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
        
        public void ElementBytes(in Bytes value) {
            AddSeparator();
            json.AppendBytes(value);
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
            json.AppendStr32(@null);
        }
        
        // ----------------- utilities
        public bool WriteObject(ref Utf8JsonParser p)
        {
            while (NextObjectMember(ref p))
            {
                switch (p.Event)
                {
                    case JsonEvent.ArrayStart:
                        MemberArrayStart(p.key.AsSpan());
                        WriteArray(ref p);
                        break;
                    case JsonEvent.ObjectStart:
                        MemberObjectStart(p.key.AsSpan());
                        WriteObject(ref p);
                        break;
                    case JsonEvent.ValueString:
                        MemberStr(p.key.AsSpan(), p.value.AsSpan());
                        break;
                    case JsonEvent.ValueNumber:
                        MemberBytes(p.key.AsSpan(), p.value);
                        break;
                    case JsonEvent.ValueBool:
                        MemberBln(p.key.AsSpan(), p.boolValue);
                        break;
                    case JsonEvent.ValueNull:
                        MemberNul(p.key.AsSpan());
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
        
        public bool WriteArray(ref Utf8JsonParser p) {
            while (NextArrayElement(ref p)) {
                switch (p.Event) {
                    case JsonEvent.ArrayStart:
                        ArrayStart(true);
                        WriteArray(ref p);
                        break;
                    case JsonEvent.ObjectStart:
                        ObjectStart();
                        WriteObject(ref p);
                        break;
                    case JsonEvent.ValueString:
                        ElementStr(p.value.AsSpan());
                        break;
                    case JsonEvent.ValueNumber:
                        ElementBytes(p.value);
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

        public bool WriteTree(ref Utf8JsonParser p) {
            switch (p.Event) {
                case JsonEvent.ObjectStart:
                    ObjectStart();
                    return WriteObject(ref p);
                case JsonEvent.ArrayStart:
                    ArrayStart(true);
                    return WriteArray(ref p);
                case JsonEvent.ValueString:
                    ElementStr(p.value.AsSpan());
                    return true;
                case JsonEvent.ValueNumber:
                    ElementBytes(p.value);
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
        
        public bool WriteMember(ReadOnlySpan<byte> key, ref Utf8JsonParser p) {
            switch (p.Event) {
                case JsonEvent.ArrayStart:
                    MemberArrayStart(key);
                    WriteArray(ref p);
                    break;
                case JsonEvent.ObjectStart:
                    MemberObjectStart(key);
                    WriteObject(ref p);
                    break;
                case JsonEvent.ValueString:
                    MemberStr(key, p.value.AsSpan());
                    break;
                case JsonEvent.ValueNumber:
                    MemberBytes(key, p.value);
                    break;
                case JsonEvent.ValueBool:
                    MemberBln(key, p.boolValue);
                    break;
                case JsonEvent.ValueNull:
                    MemberNul(key);
                    break;
            }
            return false;
        }
        
        public static bool NextObjectMember (ref Utf8JsonParser p) {
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
        
        public static bool NextArrayElement (ref Utf8JsonParser p) {
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

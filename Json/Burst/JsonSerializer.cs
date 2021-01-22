using System;
using System.Diagnostics;
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
    /// E.g by <see cref="MemberDblRef(ref string,long)"/> to add a key/value pair using an integer as value type
    /// like { "count": 11 }<br/>
    /// After all object members are serialized <see cref="ObjectEnd()"/> closes the previous started JSON object.<br/>
    ///
    /// To add a JSON array use <see cref="ArrayStart()"/>
    /// Afterwards arbitrary array elements can be added via the Element...() methods.<br/>
    /// E.g by <see cref="ElementLng"/> to add an element with an integer as value type
    /// like [ 11 ]<br/>
    /// After all array elements are serialized <see cref="ArrayEnd()"/> closes the previous started JSON array.<br/>
    ///
    /// After creating the JSON document by using the appender methods, the JSON document is available
    /// via <exception cref="dst"></exception> 
    /// </summary>
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public partial struct JsonSerializer : IDisposable
    {
        /// <summary>Contains the generated JSON document as <see cref="Bytes"/>.</summary>
        public      Bytes                   dst;
        private     ValueFormat             format;
        private     ValueList<bool>         firstEntry;
        private     ValueList<NodeType>     nodeType;
        private     Bytes                   strBuf;
        private     int                     level;
        private     int                     maxDepth;
        private     Str32                   @null;

        public      int                     Level => level;
        public      int                     MaxDepth => maxDepth;
        public      void                    SetMaxDepth(int size) { maxDepth= size; }
        
#pragma warning disable 649  // Field 'startGuard' is never assigned
        private     ValueList<bool>         startGuard;
#pragma warning restore 649

        enum NodeType {
            Undefined,
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
            int initDepth = 16;
            maxDepth = 1000;
            if (!firstEntry.IsCreated())
                firstEntry = new ValueList<bool>  (initDepth, AllocType.Persistent); firstEntry.Resize(initDepth);
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
            firstEntry.array[0] = true;
            nodeType.array[0] = NodeType.Undefined;
        }

        private void ResizeDepthBuffers(int size) {
            // resizing to size is enough, but allocate more in advance
            size *= 2;
            firstEntry.Resize(size);
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
            if (firstEntry.IsCreated())
                firstEntry.Dispose();
            if (strBuf.buffer.IsCreated())
                strBuf.Dispose();
            if (nodeType.IsCreated())
                nodeType.Dispose();
            if (dst.buffer.IsCreated())
                dst.Dispose();
            format.Dispose();
        }
#if !JSON_BURST
        // --- comment to enable source alignment in WinMerge
        public static void AppendEscString(ref Bytes dst, ref Str32 src) {
            int end = src.Length;
            var srcArr = src; 
            for (int n = 0; n < end; n++) {
                int c = char.ConvertToUtf32 (srcArr, n);
                if (char.IsSurrogatePair(srcArr, n))
                    n++;
                
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
                        Utf8Utils.AppendUnicodeToBytes(ref dst, c);
                        break;
                }
            }
        }
#endif

        // Is called from ObjectStart() & ArrayStart() only, if (elementType[level] == ElementType.Array)
        private void AddSeparator() {
            if (firstEntry.array[level]) {
                firstEntry.array[level] = false;
                return;
            }
            dst.AppendChar(',');
        }
        
        // ----------------------------- object with properties -----------------------------
        /// <summary>Start a JSON object for serialization</summary>
        public void ObjectStart() {
            AssertStart();
            if (nodeType.array[level] == NodeType.Array)
                AddSeparator();
            dst.AppendChar('{');
            if (level >= maxDepth)
                throw new InvalidOperationException("JsonSerializer exceed maxDepth: " + maxDepth);
            if (++level >= firstEntry.Count)
                ResizeDepthBuffers(level + 1);
            firstEntry.array[level] = true;
            nodeType.array[level] = NodeType.Object;
            ClearStartGuard();
        }
        
        /// <summary>Finished a previous started JSON object for serialization</summary>
        public void ObjectEnd() {
            AssertMember();
            dst.AppendChar('}');
            firstEntry.array[--level] = false;
        }

        // --- comment to enable source alignment in WinMerge
        /// <summary>Writes the key of key/value pair where the value will be an array</summary>
        public void MemberArrayStartRef(ref Str32 key) {
            AssertMember();
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
            SetStartGuard();
            ArrayStart();
        }
        
        /// <summary>Writes the key of key/value pair where the value will be an object</summary>
        public void MemberObjectStartRef(ref Str32 key) {
            AssertMember();
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
            SetStartGuard();
            ObjectStart();
        }

        /// <summary>Writes a key/value pair where the value is a "string"</summary>
        public void MemberStrRef(ref Str32 key, ref Bytes value) {
            AssertMember();
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
        public void MemberStrRef(ref Str32 key, ref Unity.Collections.FixedString128 value) {
            strBuf.Clear();
            strBuf.AppendStr128(ref value);
            MemberStrRef(ref key, ref strBuf);
        }
#else
        public void MemberStrRef(ref string key, ref string value) {
            AssertMember();
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
        public void MemberDblRef(ref Str32 key, double value) {
            AssertMember();
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
            format.AppendDbl(ref dst, value);
        }
        
        /// <summary>Writes a key/value pair where the value is a <see cref="long"/></summary>
        public void MemberDblRef(ref Str32 key, long value) {
            AssertMember();
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
            format.AppendLong(ref dst, value);
        }
        
        /// <summary>Writes a key/value pair where the value is a <see cref="bool"/></summary>
        public void MemberBlnRef(ref Str32 key, bool value) {
            AssertMember();
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
            format.AppendBool(ref dst, value);
        }
        
        /// <summary>Writes a key/value pair where the value is null</summary>
        public void MemberNulRef(ref Str32 key) {
            AssertMember();
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref key);
            dst.AppendChar2('\"', ':');
            dst.AppendStr32Ref(ref @null);
        }
        
        // ------------- non-ref Str32 Member...() versions for convenience  -------------
#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
#endif
        public void MemberArrayStart (Str32 key) {
            MemberArrayStartRef(ref key);
        }
        
#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
#endif
        public void MemberObjectStart (Str32 key) {
            MemberObjectStartRef(ref key);
        }
        
#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
        public void MemberStr (Str32 key, Unity.Collections.FixedString128 value) {
            MemberStrRef(ref key, ref value);
        }
#else
        public void MemberStr (string key, string value) {
            MemberStrRef(ref key, ref value);
        }
#endif
        
#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
#endif
        public void MemberStr (Str32 key, ref Bytes value) {
            MemberStrRef(ref key, ref value);
        }
        
#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
#endif
        public void MemberDbl (Str32 key, double value) {
            MemberDblRef(ref key, value);
        }
        
#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
#endif
        public void MemberDbl (Str32 key, long value) {
            MemberDblRef(ref key, value);
        }
        
#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
#endif
        public void MemberBln (Str32 key, bool value) {
            MemberBlnRef(ref key, value);
        }
        
#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
#endif
        public void MemberNul (Str32 key) {
            MemberNulRef(ref key);
        }

        // ----------------------------- array with elements -----------------------------
        public void ArrayStart() {
            AssertStart();
            if (nodeType.array[level] == NodeType.Array)
                AddSeparator();
            dst.AppendChar('[');
            if (level >= maxDepth)
                throw new InvalidOperationException("JsonSerializer exceed maxDepth: " + maxDepth);
            if (++level >= firstEntry.Count)
                ResizeDepthBuffers(level + 1);
            firstEntry.array[level] = true;
            nodeType.array[level] = NodeType.Array;
            SetStartGuard();
        }
        
        public void ArrayEnd() {
            if (level == 0)
                throw new InvalidOperationException("ArrayEnd...() must not be called below root level");
            AssertElement();
            dst.AppendChar(']');
            firstEntry.array[--level] = false;
        }
        
        /// <summary>Write an array element of type "string"</summary>
        public void ElementStr(ref Bytes value) {
            AssertElement();
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref value);
            dst.AppendChar('"');
        }
        
#if JSON_BURST
        public void ElementStr(Unity.Collections.FixedString128 value) {
            strBuf.Clear();
            strBuf.AppendStr128(ref value);
            AssertElement();
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref strBuf);
            dst.AppendChar('"');
        }
#else
        /// <summary>Write an array element of type <see cref="string"/></summary>
        public void ElementStr(string value) {
            AssertElement();
            AddSeparator();
            dst.AppendChar('"');
            AppendEscString(ref dst, ref value);
            dst.AppendChar('"');
        }
#endif
        /// <summary>Write an array element of type <see cref="double"/></summary>
        public void ElementDbl(double value) {
            AssertElement();
            AddSeparator();
            format.AppendDbl(ref dst, value);
        }
        
        /// <summary>Write an array element of type <see cref="long"/></summary>
        public void ElementLng(long value) {
            AssertElement();
            AddSeparator();
            format.AppendLong(ref dst, value);
        }
        
        /// <summary>Write an array element of type <see cref="bool"/></summary>
        public void ElementBln(bool value) {
            AssertElement();
            AddSeparator();
            format.AppendBool(ref dst, value);
        }
        
        /// <summary>Writes null as array element</summary>
        public void ElementNul() {
            AssertElement();
            AddSeparator();
            dst.AppendStr32Ref(ref @null);
        }
        
        // ----------------- utilities
        public bool WriteObject(ref JsonParser p) {
            while (NextObjectMember(ref p)) {
                switch (p.Event) {
                    case JsonEvent.ArrayStart:
                        MemberArrayStartRef(ref p.key);
                        WriteArray(ref p);
                        break;
                    case JsonEvent.ObjectStart:
                        MemberObjectStartRef(ref p.key);
                        WriteObject(ref p);
                        break;
                    case JsonEvent.ValueString:
                        MemberStrRef(ref p.key, ref p.value);
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
                        MemberBlnRef(ref p.key, p.boolValue);
                        break;
                    case JsonEvent.ValueNull:
                        MemberNulRef(ref p.key);
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
                        ElementStr(ref p.value);
                        break;
                    case JsonEvent.ValueNumber:
                        AddSeparator();
                        dst.AppendBytes(ref p.value);
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
                    ElementStr(ref p.value);
                    return true;
                case JsonEvent.ValueNumber:
                    dst.AppendBytes(ref p.value);
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

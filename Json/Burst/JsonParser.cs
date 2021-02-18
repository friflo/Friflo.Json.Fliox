// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Runtime.CompilerServices;
using Friflo.Json.Burst.Utils;

#if JSON_BURST
    using Str32 = Unity.Collections.FixedString32;
    using Str128 = Unity.Collections.FixedString128;
    // ReSharper disable InconsistentNaming
#else
    using Str32 = System.String;
    using Str128 = System.String;
#endif

namespace Friflo.Json.Burst
{
    /// <summary>
    /// Contains the count of JSON nodes (object members and array elements) skipped while parsing a JSON document.
    ///
    /// These count numbers are categorized by: arrays, booleans, floats, integers, nulls, objects and strings.
    /// The count numbers are incremented while skipping via one of the <see cref="JsonParser"/> Skip...() methods like
    /// <see cref="JsonParser.SkipTree()"/> and <see cref="JsonParser.SkipEvent"/>. 
    /// </summary>
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public struct SkipInfo {
        public int arrays;
        public int booleans;
        public int floats;
        public int integers;
        public int nulls;
        public int objects;
        public int strings;

        public int Sum => arrays + booleans + floats + integers + nulls + objects + strings;

        public bool IsEqual(SkipInfo si) {
            return arrays == si.arrays && booleans == si.booleans && floats == si.floats && integers == si.integers &&
                   nulls == si.nulls && objects == si.objects && strings == si.strings;
        }

        private Str128 ToStr128() {
            return $"[ arrays={arrays}, booleans= {booleans}, floats= {floats}, integers= {integers}, nulls= {nulls}, objects= {objects}, strings= {strings} ]";
        }
        
        public override string ToString() {
            // ReSharper disable once RedundantToStringCall - required for JSON_BURST
            return ToStr128().ToString();
        }
    }
    
    public class Local<TDisposable> : IDisposable where TDisposable : IDisposable
    {
        public TDisposable instance;
        
        public Local()                      { }
        public Local(TDisposable instance)  { this.instance = instance; }

        public void Dispose() {
            instance.Dispose();
        }
    }

    /// <summary>
    /// The basic JSON Parser API required to parse a JSON document.
    ///
    /// The parser has a forward iterator interface returning a <see cref="JsonEvent"/> for each call to <see cref="NextEvent()"/>.
    /// To start parsing a JSON document the parser need to be initialized with <see cref="InitParser(Friflo.Json.Burst.Bytes)"/>
    /// From this point <see cref="NextEvent()"/> can be used.
    /// Depending on the returned event additional fields contain the data captured by the event.
    /// 
    /// After a JSON document was iterated successfully an additional call to <see cref="NextEvent()"/> returns <see cref="JsonEvent.EOF"/>
    /// In case of an invalid JSON document <see cref="NextEvent()"/> returns <see cref="JsonEvent.Error"/>.
    /// At this point any subsequent call to <see cref="NextEvent()"/> will return the same error.
    ///
    /// To maximize performance the <see cref="JsonParser"/> instance should be reused. This avoids unnecessary allocations on the heap.
    /// </summary>
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public partial struct JsonParser : IDisposable
    {
        public const int DefaultMaxDepth = 100;
        
        private     int                 pos;
        private     Bytes               buf;
        private     int                 bufEnd;
        internal    int                 stateLevel;
        private     int                 maxDepth;
        private     int                 startPos;
    
        private     State               preErrorState;
        internal    ValueList<State>    state;
        private     ValueList<int>      pathPos; // used for current path
        private     ValueList<int>      arrIndex; // used for current path

        public      JsonError           error;

        /// <summary>
        /// Contains the <see cref="JsonEvent"/> set by the last call to <see cref="NextEvent()"/>,
        /// </summary>
        public      JsonEvent           Event => lastEvent;
        internal    JsonEvent           lastEvent;

        /// <summary>Contains the boolean value of an object member or an array element after <see cref="NextEvent()"/>
        /// returned <see cref="JsonEvent.ValueBool"/></summary>
        public      bool                boolValue;

        /// <summary>Contains the key on an object member after <see cref="NextEvent()"/> returned one of the
        /// <see cref="JsonEvent"/>'s starting with Value... and the previous event was <see cref="JsonEvent.ObjectStart"/>
        /// </summary>
        public      Bytes               key;

        /// <summary>Contains the (string) value of an object member or an array element after <see cref="NextEvent()"/>
        /// returned <see cref="JsonEvent.ValueString"/></summary>
        public      Bytes               value;

        private     Bytes               path; // used for current path storing the path segments names
        private     Bytes               errVal; // used for conversion of an additional value in error message creation
        private     Bytes               getPathBuf; // MUST be used only in GetPath()
    
        private     ValueFormat         format;
        private     ValueParser         valueParser;
    
        private     Str32               @true;
        private     Str32               @false;
        private     Str32               @null;
        private     Str32               emptyArray;
        private     Str128              emptyString;  // todo: remove this. It is currently only required of API: Error()
        /// <summary>In case the event returned by <see cref="NextEvent()"/> was <see cref="JsonEvent.ValueNumber"/> the flag
        /// indicates that the value of an object member or array element is a floating point number (e.g. 2.34).<br/>
        /// Otherwise false indicates that the value is of an integral type (e.g. 11) 
        /// </summary>
        public      bool                isFloat;
        /// <summary>Contains number of skipped JSON nodes when using one of the Skip...() methods like <see cref="SkipTree()"/> while parsing</summary>
        public      SkipInfo            skipInfo;

        private     int                 previousBytes;
        public      int                 ProcessedBytes => previousBytes + bufferCount + pos;
        
        private     int                 bufferCount;
        public      int                 InputPos => bufferCount + pos;
        
        // --- input array, stream, string, ... used in InitParser() methods
        private     const int           BufSize = 4096; // Test with 1 to find edge cases
        private     InputType           inputType;
        private     bool                inputStreamOpen;
#if JSON_BURST
        private     int                 readerHandle;
#else
        private     IBytesReader        bytesReader;
#endif
        private     ByteList            inputByteList;
        private     int                 inputArrayPos;
        private     int                 inputArrayEnd;


        internal enum State {
            ExpectMember        = 0,
            ExpectMemberFirst   = 1,

            ExpectElement       = 2,
            ExpectElementFirst  = 3,

            ExpectRoot          = 4, // only set at state[0]
            ExpectEof           = 5, // only set at state[0]

            JsonError           = 6,
            Finished            = 7, // only set at state[0]
        }

        /// <summary>Returns the current depth inside the JSON document while parsing</summary>
        public int          Level => stateLevel;
        public int          MaxDepth => maxDepth;
        public void         SetMaxDepth(int value) { this.maxDepth = value; }
        
        enum ErrorType {
            JsonError,
            Assertion,
            ExternalError
        }

        // ---------------------- error message creation - begin
        void Error (in Str32 module, ErrorType errorType, in Str128 msg, ref Bytes value) {
            lastEvent = JsonEvent.Error;
            preErrorState = state.array[stateLevel]; 
            state.array[stateLevel] = State.JsonError;
            if (error.ErrSet)
                throw new InvalidOperationException("JSON Error already set"); // If setting error again the relevant previous error would be overwritten.
            
            int position = bufferCount + pos - startPos;
            // Note 1:  Creating error messages complete avoid creating string on the heap to ensure no allocation
            //          in case of errors. Using string interpolation like $"...{}..." create objects on the heap
            // Note 2:  Even cascaded string interpolation does not work in Unity Burst. So using appenders make sense too.

            // Pseudo format: $"{module} error - {msg}{value} path: {path} at position: {position}";
#pragma warning disable 618 // Performance degradation
            ref Bytes errMsg = ref error.msg;
            errMsg.Clear();
            errMsg.AppendStr32(in module);
            switch (errorType) {
                case ErrorType.JsonError:           errMsg.AppendStr32 ("/JSON error: ");    break;
                case ErrorType.Assertion:           errMsg.AppendStr32 ("/assertion: ");     break;
                case ErrorType.ExternalError:       errMsg.AppendStr32 ("/error: ");         break;
            }

            errMsg.AppendStr128(in msg);
            errMsg.AppendBytes(ref value);
            errMsg.AppendStr32 (" path: '");
            AppendPath(ref errMsg);
            errMsg.AppendStr32 ("' at position: ");
            format.AppendInt(ref errMsg, position);
#pragma warning restore 618
            error.Error(position);
        }
        
        /// <summary>
        /// Set the parser to error state.<br/>
        /// Subsequent calls to <see cref="NextEvent()"/> will return <see cref="JsonEvent.Error"/> 
        /// </summary>
        /// <param name="module">Name of the module raising the error</param>
        /// <param name="msg">The message info of the error. Should be a sting literal to enable searching it the the source code</param>
        public void ErrorMsg (in Str32 module, in Str128 msg) {
            errVal.Clear();
            Error(module, ErrorType.ExternalError, in msg, ref errVal);
        }
        
        public void ErrorMsg(in Str32 module, ref Bytes msg) {
            Error(module, ErrorType.ExternalError, in emptyString, ref msg);
        }
        
        public void ErrorMsgParam(in Str32 module, in Str128 msg, ref Bytes value) {
            Error(module, ErrorType.ExternalError, in msg, ref value);
        }
        
        private JsonEvent SetError (in Str128 msg) {
            errVal.Clear();
            Error("JsonParser", ErrorType.JsonError, in msg, ref errVal);
            return JsonEvent.Error;
        }
        
        private JsonEvent SetErrorChar (in Str128 msg, char c) {
            errVal.Clear();
            errVal.AppendChar(c);
            Error("JsonParser", ErrorType.JsonError, in msg, ref errVal);
            return JsonEvent.Error;
        }
        
        private JsonEvent SetErrorInt (in Str128 msg, int value) {
            errVal.Clear();
            format.AppendInt(ref errVal, value);
            Error("JsonParser", ErrorType.JsonError, in msg, ref errVal);
            return JsonEvent.Error;
        }
        
        private bool SetErrorValue (in Str128 msg, ref Bytes value) {
            Error("JsonParser", ErrorType.JsonError, in msg, ref value);
            return false;
        }

        private bool SetErrorFalse (in Str128 msg) {
            errVal.Clear();
            Error("JsonParser", ErrorType.JsonError, in msg, ref errVal);
            return false;
        }
        
        // ReSharper disable once UnusedMember.Local
        private bool SetApplicationError (in Str128 msg) {
            errVal.Clear();
            Error("JsonParser", ErrorType.Assertion, in msg, ref errVal);
            return false;
        }
        
        private bool SetErrorEvent (in Str128 msg, JsonEvent ev)
        {
            errVal.Clear();
            JsonEventUtils.AppendEvent(ev, ref errVal);
            Error("JsonParser", ErrorType.JsonError, in msg, ref errVal);
            return false;
        }
        // ---------------------- error message creation - end

        /// <summary>
        /// Returns the current JSON path while iterating as a <see cref="string"/>.  E.g. "map.key1"
        /// </summary>
        /// <returns>The current JSON path</returns>
        public string GetPath() {
            getPathBuf.Clear();
            AppendPath(ref getPathBuf);
            return getPathBuf.ToString();
        }
        
        /// <summary>
        /// Returns the current JSON path and position while iterating as a <see cref="string"/>.<br/>
        /// E.g. { path: "map.key1", pos: 20 }<br/>
        /// This method is intended only for debugging purposes.
        /// </summary>
        /// <returns>The current JSON path and position</returns>
        public override string ToString() {
            return $"{{ path: \"{GetPath()}\", pos: {bufferCount + pos} }}";
        }
        
        /// <summary>
        /// Add the current JSON path to the given <see cref="str"/> buffer. E.g. "map.key1"
        /// </summary>
        /// <param name="str">The destination the current JSON path is added to</param>
        private void AppendPath(ref Bytes str) {
            int initialEnd = str.end;
            int lastPos = 0;
            int level = stateLevel;
            bool errored = state.array[level] == State.JsonError;
            if (errored)
                level++;
            for (int n = 1; n <= level; n++) {
                State curState;
                int index = n;
                if (errored && n == level) {
                    curState = preErrorState;
                    index = n - 1;
                } else {
                    curState = state.array[n];
                }
                switch (curState) {
                    case State.ExpectMember:
                        if (index > 1)
                            str.AppendChar('.');
                        str.AppendArray(ref path.buffer, lastPos, lastPos= pathPos.array[index]);
                        break;
                    case State.ExpectMemberFirst:
                        str.AppendArray(ref path.buffer, lastPos, lastPos= pathPos.array[index]);
                        break;
                    case State.ExpectElement:
                    case State.ExpectElementFirst:
                        if (arrIndex.array[index] != -1)
                        {
                            str.AppendChar('[');
                            format.AppendInt(ref str, arrIndex.array[index]);
                            str.AppendChar(']');
                        }
                        else
                            str.AppendStr32(in emptyArray);
                        break;
                }
                // Limit path "string" to reasonable size. Otherwise DDoS may abuse unlimited error messages.
                if (str.Len > 500) {
#pragma warning disable 618 // Performance degradation by string copy
                    str.AppendStr32("...");
#pragma warning restore 618
                    return;
                }
            }
#pragma warning disable 618 // Performance degradation by string copy
            if (initialEnd == str.end)
                str.AppendStr32 ("(root)"); 
#pragma warning restore 618
        }

        private void ResizeDepthBuffers(int size) {
            // resizing to size is enough, but allocate more in advance
            size *= 2;
            state.Resize(size);
            pathPos.Resize(size);
            arrIndex.Resize(size);
        }

        private void InitContainers() {
            if (state.IsCreated())
                return;
            maxDepth = DefaultMaxDepth;
            int initSize = 16;
            state =    new ValueList<State>(initSize, AllocType.Persistent); state.   Resize(initSize);
            pathPos =  new ValueList<int>  (initSize, AllocType.Persistent); pathPos. Resize(initSize);
            arrIndex = new ValueList<int>  (initSize, AllocType.Persistent); arrIndex.Resize(initSize);
            buf.InitBytes(BufSize);
            buf.buffer.Resize(BufSize);
            error.InitJsonError(128);
            key.InitBytes(32);
            path.InitBytes(32);
            errVal.InitBytes(32);
            getPathBuf.InitBytes(32);
            value.InitBytes(32);
            format.InitTokenFormat();
            @true =         "true";
            @false =        "false";
            @null =         "null";
            emptyArray =    "[]";
            emptyString =   "";
            valueParser.InitValueParser();
        }

        /// <summary>
        /// Dispose all internal used arrays.
        /// Only required when running with JSON_BURST within Unity. 
        /// </summary>
        public void Dispose() {
            valueParser.Dispose();
            format.Dispose();
            value.Dispose();
            getPathBuf.Dispose();
            errVal.Dispose();
            path.Dispose();
            key.Dispose();
            error.Dispose();
            buf.Dispose();
            if (arrIndex.IsCreated())   arrIndex.Dispose();
            if (pathPos.IsCreated())    pathPos.Dispose();
            if (state.IsCreated())      state.Dispose();
        }

        private void Start() {
            InitContainers();
            stateLevel = 0;
            state.array[0] = State.ExpectRoot;

            previousBytes += bufferCount + pos - startPos; // for statistics
            bufferCount = 0;
            pos = 0;
            startPos = 0;
            bufEnd = 0;
            skipInfo = default(SkipInfo);
            inputStreamOpen = true;
            error.Clear();
        }

        /// <summary>
        /// Used to iterate a JSON document.<br/>
        ///
        /// See <see cref="JsonEvent"/> for all possible events.
        /// Depending on the returned event additional fields or methods are valid for access. E.g.<br/>
        /// <see cref="key"/>, <see cref="value"/>, <see cref="boolValue"/>, <see cref="isFloat"/>,
        /// <see cref="ValueAsInt"/>, <see cref="ValueAsFloat"/> and <see cref="ValueAsDouble"/><br/> 
        /// Before starting iteration the parser need to be initialized with <see cref="InitParser(Friflo.Json.Burst.Bytes)"/>
        /// </summary>
        /// <returns>Returns successively all <see cref="JsonEvent"/>'s while iterating a JSON document.<br/>
        /// After full iteration of a JSON document an additional call to <see cref="NextEvent"/>
        /// returns <see cref="JsonEvent.EOF"/></returns>
        public JsonEvent NextEvent()
        {
            int c = ReadWhiteSpace();
            State curState = state.array[stateLevel];
            switch (curState)
            {
            case State.ExpectMember:
            case State.ExpectMemberFirst:
                switch (c)
                {
                    case ',':
                        if (curState == State.ExpectMemberFirst)
                            return SetError ("unexpected member separator");
                        c = ReadWhiteSpace();
                        if (c != '"')
                            return SetErrorChar ("expect key. Found: ", (char)c);
                        break;
                    case '}':
                        stateLevel--;
                        return lastEvent = JsonEvent.ObjectEnd;
                    case  -1:
                        return SetError("unexpected EOF > expect key");
                    case '"':
                        if (curState == State.ExpectMember)
                            return SetError ("expect member separator");
                        break;
                    default:
                        return SetErrorChar("unexpected character > expect key. Found: ", (char)c);
                }
                // case: c == '"'
                state.array[stateLevel] = State.ExpectMember;
                if (!ReadString(ref key))
                    return JsonEvent.Error;
                // update current path
                path.SetEnd(pathPos.array[stateLevel-1]);  // "Clear"
                path.AppendBytes(ref key);
                pathPos.array[stateLevel] = path.EndPos;
                //
                c = ReadWhiteSpace();
                if (c != ':')
                    return SetErrorChar ("Expected ':' after key. Found: ", (char)c);
                c = ReadWhiteSpace();
                break;
            
            case State.ExpectElement:
            case State.ExpectElementFirst:
                arrIndex.array[stateLevel]++;
                if (c == ']')
                {
                    stateLevel--;
                    return lastEvent = JsonEvent.ArrayEnd;
                }
                if (curState == State.ExpectElement)
                {
                    if (c != ',')
                        return SetErrorChar("expected array separator ','. Found: ", (char)c);
                    c = ReadWhiteSpace();
                }
                else
                    state.array[stateLevel] = State.ExpectElement;
                break;
            
            case State.ExpectRoot:
                state.array[0] = State.ExpectEof;
                switch (c)
                {
                    case '{':
                        stateLevel++; // no index check, iterator is on root level (stateLevel == 0)
                        pathPos.array[stateLevel] = pathPos.array[stateLevel - 1];
                        state.array[stateLevel] = State.ExpectMemberFirst;
                        return lastEvent = JsonEvent.ObjectStart;
                    case '[':
                        stateLevel++; // no index check, iterator is on root level (stateLevel == 0)
                        pathPos.array[stateLevel] = pathPos.array[stateLevel - 1];
                        state.array[stateLevel] = State.ExpectElementFirst;
                        arrIndex.array[stateLevel] = -1;
                        return lastEvent = JsonEvent.ArrayStart;
                    case -1:
                        return SetError("unexpected EOF on root");
                    // default: read following bytes as value  
                }
                break;
            
            case State.ExpectEof:
                if (c == -1) {
                    state.array[0] = State.Finished;
                    return lastEvent = JsonEvent.EOF;
                }
                return SetError("Expected EOF");
            
            case State.Finished:
                return SetError("Parsing already finished");
            
            case State.JsonError:
                return JsonEvent.Error;
            }
        
            // ---- read value of key/value pairs or array items ---
            switch (c)
            {
                case '"':
                    if (ReadString(ref value))
                        return lastEvent = JsonEvent.ValueString;
                    return JsonEvent.Error;
                case '{':
                    if (stateLevel >= maxDepth)
                        return SetErrorInt("nesting in JSON document exceed maxDepth: ", maxDepth);
                    if (++stateLevel >= pathPos.Count)
                        ResizeDepthBuffers(stateLevel + 1);
                    pathPos.array[stateLevel] = pathPos.array[stateLevel - 1];
                    state.array[stateLevel] = State.ExpectMemberFirst;
                    return lastEvent = JsonEvent.ObjectStart;
                case '[':
                    if (stateLevel >= maxDepth)
                        return SetErrorInt("nesting in JSON document exceed maxDepth: ", maxDepth);
                    if (++stateLevel >= pathPos.Count)
                        ResizeDepthBuffers(stateLevel + 1);
                    pathPos.array[stateLevel] = pathPos.array[stateLevel - 1];
                    state.array[stateLevel] = State.ExpectElementFirst;
                    arrIndex.array[stateLevel] = -1;
                    return lastEvent = JsonEvent.ArrayStart;
                case '0':   case '1':   case '2':   case '3':   case '4':
                case '5':   case '6':   case '7':   case '8':   case '9':
                case '-':   case '+':   case '.':
                    if (ReadNumber(c))
                        return lastEvent = JsonEvent.ValueNumber;
                    return JsonEvent.Error;
                case 't':
                    if (!ReadKeyword('t', in @true ))
                        return JsonEvent.Error;
                    boolValue= true;
                    return lastEvent = JsonEvent.ValueBool;
                case 'f':
                    if (!ReadKeyword('f', in @false))
                        return JsonEvent.Error;
                    boolValue= false;
                    return lastEvent = JsonEvent.ValueBool;
                case 'n':
                    if (!ReadKeyword('n', in @null))
                        return JsonEvent.Error;
                    return lastEvent = JsonEvent.ValueNull;
                case  -1:
                    return SetError("unexpected EOF while reading value");
                default:
                    return SetErrorChar("unexpected character while reading value. Found: ", (char)c);
            }
            // unreachable
        }

        private int ReadWhiteSpace()
        {
            // using locals improved performance
            while (true) {
                int p       = pos;
                int end     = bufEnd;
                for (; p < end;)
                {
                    int c = buf.buffer.array[p++];
                    if (c > ' ') {
                        pos = p;
                        return c;
                    }
                    if (c != ' ' &&
                        c != '\t' &&
                        c != '\n' &&
                        c != '\r') {
                        pos = p;
                        return c;
                    }
                }
                pos = p;
                if (Read())
                    continue;
                return -1;
            }
        }
    
        private bool ReadNumber (int firstChar)
        {
            value.Clear();
            value.AppendChar((char)firstChar);
            isFloat = false;
            
            while (true) {
                for (; pos < bufEnd; pos++)
                {
                    int c = buf.buffer.array[pos];
                    switch (c)
                    {
                        case '0':   case '1':   case '2':   case '3':   case '4':
                        case '5':   case '6':   case '7':   case '8':   case '9':
                        case '-':   case '+':
                            value.AppendChar((char)c);
                            continue;
                        case '.':   case 'e':   case 'E':
                            value.AppendChar((char)c);
                            isFloat = true;
                            continue;
                    }
                    switch (c) {
                        case ',': case '}': case ']':
                        case ' ': case '\r': case '\n': case '\t':
                            return true;
                    }
                    SetErrorChar("unexpected character while reading number. Found : ", (char)c);
                    return false;
                }

                if (Read())
                    continue;
                
                if (state.array[stateLevel] == State.ExpectEof)
                    return true;
                
                return SetErrorFalse("unexpected EOF while reading number");
            }
        }
    
        private bool ReadString(ref Bytes token)
        {
            token.Clear();
            while (true)
            {
                // using locals improved performance
                int p       = pos;
                int end     = bufEnd;
                
                for (; p < end; p++)
                {
                    int c = buf.buffer.array[p];
                    if (c == '\"')
                    {
                        pos = p + 1;
                        return true;
                    }
                    if (c == '\r' || c == '\n')
                        return SetErrorFalse("unexpected line feed while reading string");
                    if (c != '\\') {
                        token.AppendChar((char)c);
                    } else {
                        if (++p >= end) {
                            pos = p;
                            if (!Read())
                                return SetErrorFalse("unexpected EOF while reading string");
                            p   = pos;
                            end = bufEnd;
                        }
                        c = buf.buffer.array[p];
                        switch (c)
                        {
                        case '"':   token.AppendChar('"');  break;
                        case '\\':  token.AppendChar('\\'); break;
                        case '/':   token.AppendChar('/');  break;
                        case 'b':   token.AppendChar('\b'); break;
                        case 'f':   token.AppendChar('\f'); break;
                        case 'r':   token.AppendChar('\r'); break;
                        case 'n':   token.AppendChar('\n'); break;
                        case 't':   token.AppendChar('\t'); break;                  
                        case 'u':
                            pos = p + 1;
                            if (!ReadUnicode(ref token))
                                return false;
                            if (pos >= bufEnd) {
                                if (!Read())
                                    return SetErrorFalse("unexpected EOF while reading string");
                            }
                            p   = pos - 1;
                            end = bufEnd;
                            continue;
                        }
                    }
                }
                pos = p;
                if (Read())
                    continue;
                
                return SetErrorFalse("unexpected EOF while reading string");
            }
        }
    
        private bool ReadUnicode (ref Bytes token)
        {
            if (pos >= bufEnd) {
                if (!Read())
                    return SetErrorFalse("Expect 4 hex digits after '\\u' in value");
            }
            int d1 = Digit2Int(buf.buffer.array[pos++]);
            if (pos >= bufEnd) {
                if (!Read())
                    return SetErrorFalse("Expect 4 hex digits after '\\u' in value");
            }
            int d2 = Digit2Int(buf.buffer.array[pos++]);
            if (pos >= bufEnd) {
                if (!Read())
                    return SetErrorFalse("Expect 4 hex digits after '\\u' in value");
            }
            int d3 = Digit2Int(buf.buffer.array[pos++]);
            if (pos >= bufEnd) {
                if (!Read())
                    return SetErrorFalse("Expect 4 hex digits after '\\u' in value");
            }
            int d4 = Digit2Int(buf.buffer.array[pos++]);

            if (d1 == -1 || d2 == -1 || d3 == -1 || d4 == -1)
                return SetErrorFalse("Invalid hex digits after '\\u' in value");

            int uni = d1 << 12 | d2 << 8 | d3 << 4 | d4;
        
            // UTF-8 Encoding
            token.EnsureCapacity(4);
            ref var str = ref token.buffer.array;
            int i = token.EndPos;
            if (uni < 0x80)
            {
                str[i] =    (byte)uni;
                token.SetEnd(i + 1);
                return true;
            }
            if (uni < 0x800)
            {
                str[i]   =  (byte)(m_11oooooo | (uni >> 6));
                str[i+1] =  (byte)(m_1ooooooo | (uni         & m_oo111111));
                token.SetEnd(i + 2);
                return true;
            }
            if (uni < 0x10000)
            {
                str[i]   =  (byte)(m_111ooooo |  (uni >> 12));
                str[i+1] =  (byte)(m_1ooooooo | ((uni >> 6)  & m_oo111111));
                str[i+2] =  (byte)(m_1ooooooo |  (uni        & m_oo111111));
                token.SetEnd(i + 3);
                return true;
            }
            str[i]   =      (byte)(m_1111oooo |  (uni >> 18));
            str[i+1] =      (byte)(m_1ooooooo | ((uni >> 12) & m_oo111111));
            str[i+2] =      (byte)(m_1ooooooo | ((uni >> 6)  & m_oo111111));
            str[i+3] =      (byte)(m_1ooooooo |  (uni        & m_oo111111));
            token.SetEnd(i + 4);
            return true;
        }
    
        private static readonly int     m_1ooooooo = 0x80;
        private static readonly int     m_11oooooo = 0xc0;
        private static readonly int     m_111ooooo = 0xe0;
        private static readonly int     m_1111oooo = 0xf0;
    
        private static readonly int     m_oo111111 = 0x3f;
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Digit2Int (int c)
        {
            if ('0' <= c && c <= '9')
                return c - '0';
            if ('a' <= c && c <= 'f')
                return c - 'a' + 10;
            if ('A' <= c && c <= 'F')
                return c - 'A' + 10;
            return -1;
        }
    
        private bool ReadKeyword (char firstChar, in Str32 keyword)
        {
            value.Clear();
            value.AppendChar(firstChar);
            int keyWordPos = 1;
            
            while (true) {
                
                for (; pos < bufEnd; pos++)
                {
                    int c = buf.buffer.array[pos];
                    value.AppendChar((char)c);
                    if (c == keyword[keyWordPos++]) {
                        if (keyWordPos != keyword.Length)
                            continue;
                        pos++;
                        return true;
                    }
                    pos++;
                    return SetErrorValue("invalid value: ", ref value);
                }
                if (Read())
                    continue;
                
                return SetErrorFalse("Unexpected EOF while keyword");
            }
        }

        /// <summary>
        /// Skip parsing a complete JSON node which can be object member, an array element or a value on root.<br/>
        /// While skipping a tree of nodes inside a JSON document all counts inside <see cref="skipInfo"/> are incremented. 
        /// </summary>
        /// <returns>Returns true if skipping was successful</returns>
        public bool SkipTree()
        {
            State curState = state.array[stateLevel];
            switch (curState)
            {
            case State.ExpectMember:
            case State.ExpectMemberFirst:
                return SkipObject();
            case State.ExpectElement:
            case State.ExpectElementFirst:
                return SkipArray();
            case State.ExpectRoot:
                NextEvent();
                return SkipEvent();
            default:
                // dont set error. It would overwrite a previous error (parser state did not change)
                return false;
            }
        }
        
        private bool SkipObject()
        {
            skipInfo.objects++;
            while (true)
            {
                JsonEvent ev = NextEvent();
                switch (ev)
                {
                case JsonEvent. ValueString:
                    skipInfo.strings++;
                    break;
                case JsonEvent. ValueNumber:
                    if (isFloat)
                        skipInfo.floats++;
                    else
                        skipInfo.integers++;
                    break;
                case JsonEvent. ValueBool:
                    skipInfo.booleans++;
                    break;
                case JsonEvent. ValueNull:
                    skipInfo.nulls++;
                    break;
                case JsonEvent. ObjectStart:
                    if (!SkipObject())
                        return false;
                    break;
                case JsonEvent. ArrayStart:
                    if(!SkipArray())
                        return false;
                    break;
                case JsonEvent. ObjectEnd:
                    return true;
                case JsonEvent. Error:
                    return false;
                default:
                    return SetErrorEvent("unexpected state: ", ev);
                }
            }
        }
        
        private bool SkipArray()
        {
            skipInfo.arrays++;
            while (true)
            {
                JsonEvent ev = NextEvent();
                switch (ev)
                {
                case JsonEvent. ValueString:
                    skipInfo.strings++;
                    break;
                case JsonEvent. ValueNumber:
                    if (isFloat)
                        skipInfo.floats++;
                    else
                        skipInfo.integers++;
                    break;
                case JsonEvent. ValueBool:
                    skipInfo.booleans++;
                    break;
                case JsonEvent. ValueNull:
                    skipInfo.nulls++;
                    break;
                case JsonEvent. ObjectStart:
                    if (!SkipObject())
                        return false;
                    break;
                case JsonEvent. ArrayStart:
                    if(!SkipArray())
                        return false;
                    break;
                case JsonEvent. ArrayEnd:
                    return true;
                case JsonEvent. Error:
                    return false;
                default:
                    return SetErrorEvent("unexpected state: ", ev);
                }
            }
        }
        
        /// <summary>
        /// Skip parsing a complete JSON node which can be object member, an array element or a value on root
        /// with an already consumed <see cref="JsonEvent"/><br/>
        /// In case of a (primitive) Value... event of <see cref="JsonEvent"/> it only increments the
        /// related <see cref="skipInfo"/> count.<br/>
        /// In case of <see cref="JsonEvent.ObjectStart"/> or <see cref="JsonEvent.ArrayStart"/> it skips the
        /// whole JSON tree while incrementing the counts of <see cref="skipInfo"/> of all iterated JSON nodes. 
        /// </summary>
        /// <returns></returns>
        public bool SkipEvent () {
            switch (lastEvent) {
                case JsonEvent.ArrayStart:
                case JsonEvent.ObjectStart:
                    return SkipTree();
                case JsonEvent.ArrayEnd:
                case JsonEvent.ObjectEnd:
                    return false;
                case JsonEvent. ValueString:
                    skipInfo.strings++;
                    return true;
                case JsonEvent. ValueNumber:
                    if (isFloat)
                        skipInfo.floats++;
                    else
                        skipInfo.integers++;
                    return true;
                case JsonEvent. ValueBool:
                    skipInfo.booleans++;
                    return true;
                case JsonEvent. ValueNull:
                    skipInfo.nulls++;
                    return true;
                case JsonEvent.Error:
                case JsonEvent.EOF:
                    return false;
            }
            return true; // unreachable
        }

        /// <summary>
        /// Returns the <see cref="double"/> value of an object member or an array element after <see cref="NextEvent()"/>
        /// returned <see cref="JsonEvent.ValueNumber"/> and <see cref="isFloat"/> is true
        /// </summary>
        public double ValueAsDoubleStd(out bool success) {
            double result = valueParser.ParseDoubleStd(ref value, ref errVal, out success);
            if (!success)
                SetErrorValue("", ref errVal);
            return result;
        }
        
        /// <summary>
        /// Returns the <see cref="float"/> value of an object member or an array element after <see cref="NextEvent()"/>
        /// returned <see cref="JsonEvent.ValueNumber"/> and <see cref="isFloat"/> is true
        /// </summary>
        public float ValueAsFloatStd(out bool success) {
            float result = valueParser.ParseFloatStd(ref value, ref errVal, out success);
            if (!success)
                SetErrorValue("", ref errVal);
            return result;
        }
        
        /// <summary>
        /// Returns the <see cref="double"/> value of an object member or an array element after <see cref="NextEvent()"/>
        /// returned <see cref="JsonEvent.ValueNumber"/> and <see cref="isFloat"/> is true
        /// </summary>
        public double ValueAsDouble(out bool success) {
            double result = valueParser.ParseDouble(ref value, ref errVal, out success);
            if (!success) 
                SetErrorValue("", ref errVal);
            return result;
        }
        
        /// <summary>
        /// Returns the <see cref="float"/> value of an object member or an array element after <see cref="NextEvent()"/>
        /// returned <see cref="JsonEvent.ValueNumber"/> and <see cref="isFloat"/> is true
        /// </summary>
        public float ValueAsFloat(out bool success) {
            double result = valueParser.ParseDouble(ref value, ref errVal, out success);
            if (!success) 
                SetErrorValue("", ref errVal);
            if (result < float.MinValue) {
                SetErrorValue("value is less than float.MinValue. ", ref value);
                return 0;
            }
            if (result > float.MaxValue) {
                SetErrorValue("value is greater than float.MaxValue. ", ref value);
                return 0;
            }
            return (float)result;
        }
        
        /// <summary>
        /// Returns the <see cref="long"/> value of an object member or an array element after <see cref="NextEvent()"/>
        /// returned <see cref="JsonEvent.ValueNumber"/> and <see cref="isFloat"/> is false
        /// </summary>
        public long ValueAsLong(out bool success) {
            long result = valueParser.ParseLong(ref value, ref errVal, out success);
            if ( !success)
                SetErrorValue("", ref errVal);
            return result;
        }
        
        /// <summary>
        /// Returns the <see cref="int"/> value of an object member or an array element after <see cref="NextEvent()"/>
        /// returned <see cref="JsonEvent.ValueNumber"/> and <see cref="isFloat"/> is false
        /// </summary>
        public int ValueAsInt(out bool success) {
            int result = valueParser.ParseInt(ref value, ref errVal, out success);
            if (!success)
                SetErrorValue("", ref errVal);
            return result;
        }
        
        public short ValueAsShort(out bool success) {
            long result = valueParser.ParseInt(ref value, ref errVal, out success);
            if (!success)
                SetErrorValue("", ref errVal);
            if (result < short.MinValue) {
                SetErrorValue("value is less than short.MinValue. ", ref value);
                return 0;
            }
            if (result > short.MaxValue) {
                SetErrorValue("value is greater than short.MaxValue. ", ref value);
                return 0;
            }
            return (short)result;
        }
        
        public byte ValueAsByte(out bool success) {
            long result = valueParser.ParseInt(ref value, ref errVal, out success);
            if (!success)
                SetErrorValue("", ref errVal);
            if (result < byte.MinValue) {
                SetErrorValue("value is less than byte.MinValue. ", ref value);
                return 0;
            }
            if (result > byte.MaxValue) {
                SetErrorValue("value is greater than byte.MaxValue. ", ref value);
                return 0;
            }
            return (byte)result;
        }
        
        public bool ValueAsBool(out bool success) {
            if (lastEvent != JsonEvent.ValueBool) {
                success = false;
                return false;
            }
            success = true;
            return boolValue;
        }


    }
}
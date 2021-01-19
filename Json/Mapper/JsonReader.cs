// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Mapper.Types;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper
{
    // JsonReader
    public class JsonReader : IDisposable
    {
        public          JsonParser      parser;
        public readonly TypeCache       typeCache;

        public readonly Bytes           discriminator   = new Bytes("$type");
        public          Bytes           strBuf          = new Bytes(0);
        public readonly BytesString     bytesRef        = new BytesString();

        public          JsonError       Error => parser.error;
        public          SkipInfo        SkipInfo => parser.skipInfo;
        
        public          bool            ThrowException {
            get => parser.error.throwException;
            set => parser.error.throwException = value;
        }

        public JsonReader(TypeStore typeStore) {
            typeCache = new TypeCache(typeStore);
            parser = new JsonParser {error = {throwException = false}};
        }

        public void Dispose() {
            discriminator.Dispose();
            parser.Dispose();
            strBuf.Dispose();
        }
        
        /// <summary>
        /// Dont throw exceptions in error case, if not enabled by <see cref="ThrowException"/>
        /// In error case this information is available via <see cref="Error"/> 
        /// </summary>
        public T ReadValue <T>(Bytes bytes) where T : struct {
            int start = bytes.Start;
            int len = bytes.Len;
            Var slot = new Var();
            bool success = ReadStart(bytes.buffer, start, len, typeof(T), ref slot);
            parser.NextEvent(); // EOF
            if (!success) {
                if (!Error.ErrSet)
                    throw new InvalidOperationException("expect error is set");
                return default;
            }
            return (T) slot.Get();
        }
        
        public T Read<T>(Bytes bytes) {
            int start = bytes.Start;
            int len = bytes.Len;
            Var slot = new Var();
            bool success = ReadStart(bytes.buffer, start, len, typeof(T), ref slot);
            parser.NextEvent(); // EOF
            if (typeof(T).IsValueType && !success && parser.error.ErrSet)
                throw new InvalidOperationException(parser.error.msg.ToString() + "\nNote: Use non generic Read() method");
            return (T) slot.Get();
        }
        
        public bool Read<T>(Bytes bytes, ref Var result) {
            int start = bytes.Start;
            int len = bytes.Len;
            result.Clear();
            bool success = ReadStart(bytes.buffer, start, len, typeof(T), ref result);
            parser.NextEvent(); // EOF
            return success;
        }
        
        public Object Read(Bytes bytes, Type type) {
            int start = bytes.Start;
            int len = bytes.Len;
            Var slot = new Var();
            if (!ReadStart(bytes.buffer, start, len, type, ref slot))
                return default;
            parser.NextEvent(); // EOF
            return slot.Get();
        }

        public Object Read(ByteList buffer, int offset, int len, Type type) {
            Var slot = new Var();
            ReadStart(buffer, offset, len, type, ref slot);
            parser.NextEvent(); // EOF
            return slot.Get();
        }

        private bool ReadStart(ByteList bytes, int offset, int len, Type type, ref Var slot) {
            parser.InitParser(bytes, offset, len);
            
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                    case JsonEvent.ValueNull:
                        StubType valueType = typeCache.GetType(type);
                        return valueType.map.Read(this, ref slot, valueType);
                    case JsonEvent.Error:
                        return false;
                    default:
                        return ErrorNull("unexpected state in Read() : ", ev);
                }
            }
        }

        public bool ReadTo<T>(Bytes bytes, T obj) where T : class {
            int start = bytes.Start;
            int len = bytes.Len;
            Var slot = new Var { Obj = obj };
            bool success = ReadTo(bytes.buffer, start, len, ref slot);
            parser.NextEvent(); // EOF
            return success;
        }

        public bool ReadTo(ByteList bytes, int offset, int len, ref Var slot) {
            parser.InitParser(bytes, offset, len);

            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ArrayStart:
                        StubType valueType = typeCache.GetType(slot.Obj.GetType()); // lookup required
                        return valueType.map.Read(this, ref slot, valueType);
                    case JsonEvent.Error:
                        return false;
                    default:
                        return ErrorNull("ReadTo() can only used on an JSON object or array", ev);
                }
            }
        }

        public bool ErrorIncompatible(string msg, StubType expectType, ref JsonParser parser) {
            ErrorIncompatible(msg, "", expectType, ref parser);
            return false;
        }

        public bool ErrorIncompatible(string msg, string msgParam, StubType expectType, ref JsonParser parser) {
            /*
            string evType = null;
            string value = null;
            switch (parser.Event) {
                case JsonEvent.ValueBool:   evType = "bool";   value = parser.boolValue ? "true" : "false"; break;
                case JsonEvent.ValueString: evType = "string"; value = "'" + parser.value + "'";            break;
                case JsonEvent.ValueNumber: evType = "number"; value = parser.value.ToString();             break;
                case JsonEvent.ValueNull:   evType = "null";   value = "null";                              break;
                case JsonEvent.ArrayStart:  evType = "array";  value = "[...]";                             break;
                case JsonEvent.ObjectStart: evType = "object"; value = "{...}";                             break;
            } */
            // Error format: $"Cannot assign {evType} to {msg}. Expect: {expectType.type.Name}, got: {value}";
            strBuf.Clear();
            strBuf.AppendString("Cannot assign ");
            switch (parser.Event) {
                case JsonEvent.ValueBool:   strBuf.AppendString("bool");    break;
                case JsonEvent.ValueString: strBuf.AppendString("string");  break;
                case JsonEvent.ValueNumber: strBuf.AppendString("number");  break;
                case JsonEvent.ValueNull:   strBuf.AppendString("null");    break;
                case JsonEvent.ArrayStart:  strBuf.AppendString("array");   break;
                case JsonEvent.ObjectStart: strBuf.AppendString("object");  break;
            }
            strBuf.AppendString(" to ");
            strBuf.AppendString(msg);
            strBuf.AppendString(msgParam);
            strBuf.AppendString(". Expect: ");
            strBuf.AppendString(expectType.type.Name);
            strBuf.AppendString(", got: ");
            switch (parser.Event) {
                case JsonEvent.ValueBool:   strBuf.AppendString(parser.boolValue ? "true" : "false");       break;
                case JsonEvent.ValueString:
                    strBuf.AppendChar('\'');strBuf.AppendBytes(ref parser.value); strBuf.AppendChar('\'');  break;
                case JsonEvent.ValueNumber: strBuf.AppendBytes(ref parser.value);                           break;
                case JsonEvent.ValueNull:   strBuf.AppendString("null");                                    break;
                case JsonEvent.ArrayStart:  strBuf.AppendString("[...]");                                   break;
                case JsonEvent.ObjectStart: strBuf.AppendString("{...}");                                   break;
            }
            parser.Error("JsonReader", ref strBuf);
            return false;
        }
        
        public bool ErrorNull(string msg, string value) {
            strBuf.Clear();
            strBuf.AppendString(msg);
            strBuf.AppendString(value);
            parser.Error("JsonReader", ref strBuf);
            return false;
        }

        public bool ErrorNull(string msg, JsonEvent ev) {
            strBuf.Clear();
            strBuf.AppendString(msg);
            JsonEventUtils.AppendEvent(ev, ref strBuf);
            parser.Error("JsonReader", ref strBuf);
            return false;
        }

        public bool ErrorNull(string msg, ref Bytes value) {
            parser.Error("JsonReader", msg, ref value);
            return false;
        }
        
        /** Method only exist to find places, where token (numbers) are parsed. E.g. in or double */
        public bool ValueParseError() {
            return false; // ErrorNull(parser.parseCx.GetError().ToString());
        }

        public static readonly int minLen = 8;

        public static int Inc(int len) {
            return len < 5 ? minLen : 2 * len;
        }

    }
}

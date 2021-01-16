// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Managed.Codecs;
using Friflo.Json.Managed.Types;

namespace Friflo.Json.Managed
{
    // JsonReader
    public class JsonReader : IDisposable
    {
        public          JsonParser      parser;
        public readonly TypeCache       typeCache;

        public readonly Bytes           discriminator = new Bytes("$type");

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
        }
        
        /// <summary>
        /// Dont throw exceptions in error case, if not enabled by <see cref="ThrowException"/>
        /// In error case this information is available via <see cref="Error"/> 
        /// </summary>
        public T ReadValue <T>(Bytes bytes) where T : struct {
            int start = bytes.Start;
            int len = bytes.Len;
            Slot slot = new Slot();
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
            Slot slot = new Slot();
            bool success = ReadStart(bytes.buffer, start, len, typeof(T), ref slot);
            parser.NextEvent(); // EOF
            if (typeof(T).IsValueType && !success && parser.error.ErrSet)
                throw new InvalidOperationException(parser.error.msg.ToString());
            return (T) slot.Get();
        }
        
        public Object Read(Bytes bytes, Type type) {
            int start = bytes.Start;
            int len = bytes.Len;
            Slot slot = new Slot();
            ReadStart(bytes.buffer, start, len, type, ref slot);
            parser.NextEvent(); // EOF
            return slot.Get();
        }

        public Object Read(ByteList buffer, int offset, int len, Type type) {
            Slot slot = new Slot();
            ReadStart(buffer, offset, len, type, ref slot);
            parser.NextEvent(); // EOF
            return slot.Get();
        }

        private bool ReadStart(ByteList bytes, int offset, int len, Type type, ref Slot slot) {
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
                        return valueType.codec.Read(this, ref slot, valueType);
                    case JsonEvent.Error:
                        return false;
                    default:
                        return ErrorNull("unexpected state in Read() : ", ev);
                }
            }
        }

        public Object ReadTo(Bytes bytes, Object obj) {
            int start = bytes.Start;
            int len = bytes.Len;
            var ret = ReadTo(bytes.buffer, start, len, obj);
            parser.NextEvent();
            return ret;
        }

        public Object ReadTo(ByteList bytes, int offset, int len, Object obj) {
            parser.InitParser(bytes, offset, len);

            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ArrayStart:
                        Slot value = new Slot();
                        StubType valueType = typeCache.GetType(obj.GetType()); // lookup required
                        valueType.codec.Read(this, ref value, valueType);
                        return value.Get();
                    case JsonEvent.Error:
                        return null;
                    default:
                        return ErrorNull("ReadTo() can only used on an JSON object or array", ev);
                }
            }
        }
        
        
        public bool ErrorIncompatible(string msg, StubType expectType, ref JsonParser parser) {
            // TODO use message / value pattern as in JsonParser to avoid allocations by string interpolation
            // Cannot assign null to Dictionary value.
            string evType = null;
            string value = null;
            switch (parser.Event) {
                case JsonEvent.ValueBool:   evType = "bool";   value = parser.boolValue ? "true" : "false"; break;
                case JsonEvent.ValueString: evType = "string"; value = "'" + parser.value + "'";            break;
                case JsonEvent.ValueNumber: evType = "number"; value = parser.value.ToString();             break;
                case JsonEvent.ValueNull:   evType = "null";   value = "null";                              break;
                case JsonEvent.ArrayStart:  evType = "array";  value = "[...]";                             break;
                case JsonEvent.ObjectStart: evType = "object"; value = "{...}";                             break;
            }
            parser.Error("JsonReader", "Cannot assign " + evType + " to " + msg + ". Expect: " + expectType.type.Name + ", got: " + value);
            return false;
        }
        
        public bool ErrorNull(string msg, string value) {
            // TODO use message / value pattern as in JsonParser to avoid allocations by string interpolation
            parser.Error("JsonReader", msg + value);
            return false;
        }

        public bool ErrorNull(string msg, JsonEvent ev) {
            // TODO use message / value pattern as in JsonParser to avoid allocations by string interpolation
            parser.Error("JsonReader", msg + ev.ToString());
            return false;
        }

        public bool ErrorNull(string msg, ref Bytes value) {
            // TODO use message / value pattern as in JsonParser to avoid allocations by string interpolation
            parser.Error("JsonReader", msg + value.ToStr32());
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

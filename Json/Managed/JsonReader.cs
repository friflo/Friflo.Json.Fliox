// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
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
            var ret = ReadStart(bytes.buffer, start, len, typeof(T));
            parser.NextEvent(); // EOF
            if (ret == null) {
                if (!Error.ErrSet)
                    throw new InvalidOperationException("expect error is set");
                return default;
            }
            return (T) ret;
        }
        
        public T Read<T>(Bytes bytes) {
            int start = bytes.Start;
            int len = bytes.Len;
            var ret = ReadStart(bytes.buffer, start, len, typeof(T));
            parser.NextEvent(); // EOF
            if (typeof(T).IsValueType && ret == null && parser.error.ErrSet)
                throw new InvalidOperationException(parser.error.msg.ToString());
            return (T) ret;
        }
        
        public Object Read(Bytes bytes, Type type) {
            int start = bytes.Start;
            int len = bytes.Len;
            var ret = ReadStart(bytes.buffer, start, len, type);
            parser.NextEvent(); // EOF
            return ret;
        }

        public Object Read(ByteList buffer, int offset, int len, Type type) {
            var ret = ReadStart(buffer, offset, len, type);
            parser.NextEvent(); // EOF
            return ret;
        }

        private Object ReadStart(ByteList bytes, int offset, int len, Type type) {
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
                        return valueType.codec.Read(this, null, valueType); // lookup required
                    case JsonEvent.Error:
                        return null;
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
                        StubType valueType = typeCache.GetType(obj.GetType()); // lookup required
                        return valueType.codec.Read(this, obj, valueType);
                    case JsonEvent.Error:
                        return null;
                    default:
                        return ErrorNull("ReadTo() can only used on an JSON object or array", ev);
                }
            }
        }
        
        
        public Object ErrorIncompatible(string msg, StubType expectType, ref JsonParser parser) {
            // TODO use message / value pattern as in JsonParser to avoid allocations by string interpolation
            parser.Error("JsonReader", msg + expectType.type.Name + ", got: '" + parser.value + "'");
            return null;
        }
        
        public Object ErrorNull(string msg, string value) {
            // TODO use message / value pattern as in JsonParser to avoid allocations by string interpolation
            parser.Error("JsonReader", msg + value);
            return null;
        }

        public Object ErrorNull(string msg, JsonEvent ev) {
            // TODO use message / value pattern as in JsonParser to avoid allocations by string interpolation
            parser.Error("JsonReader", msg + ev.ToString());
            return null;
        }

        public Object ErrorNull(string msg, ref Bytes value) {
            // TODO use message / value pattern as in JsonParser to avoid allocations by string interpolation
            parser.Error("JsonReader", msg + value.ToStr32());
            return null;
        }
        
        /** Method only exist to find places, where token (numbers) are parsed. E.g. in or double */
        public Object ValueParseError() {
            return null; // ErrorNull(parser.parseCx.GetError().ToString());
        }

        public static readonly int minLen = 8;

        public static int Inc(int len) {
            return len < 5 ? minLen : 2 * len;
        }

    }
}

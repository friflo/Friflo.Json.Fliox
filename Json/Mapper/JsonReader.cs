// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Types;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class JsonReader : IDisposable
    {
        public              JsonParser      parser;
        public              int             maxDepth;
        /// <summary>Caches type mata data per thread and provide stats to the cache utilization</summary>
        public   readonly   TypeCache       typeCache;

        internal readonly   Bytes           discriminator   = new Bytes("$type");
        /// <summary>Can be used for custom mappers to create a temporary "string"
        /// without creating a string on the heap.</summary>
        public              Bytes           strBuf          = new Bytes(0);
        /// <summary>Can be used for custom mappers to lookup for a "string" in a Dictionary
        /// without creating a string on the heap.</summary>
        public readonly     BytesString     keyRef          = new BytesString();

        public              JsonError       Error => parser.error;
        public              SkipInfo        SkipInfo => parser.skipInfo;
        
        public              bool            ThrowException {
            get => parser.error.throwException;
            set => parser.error.throwException = value;
        }

        public JsonReader(TypeStore typeStore) {
            typeCache = new TypeCache(typeStore);
            parser = new JsonParser {error = {throwException = false}};
            maxDepth = 100;
        }

        public void Dispose() {
            typeCache.Dispose();
            discriminator.Dispose();
            parser.Dispose();
            strBuf.Dispose();
        }
        
        /// <summary>
        /// Dont throw exceptions in error case, if not enabled by <see cref="ThrowException"/>
        /// In error case this information is available via <see cref="Error"/> 
        /// </summary>
        public T Read<T>(Bytes bytes) {
            int start = bytes.StartPos;
            int len = bytes.Len;
            Var slot = new Var();
            StubType stubType = typeCache.GetType(typeof(T));
            bool success = ReadStart(bytes.buffer, start, len, stubType, ref slot);
            if (!success)
                return default;
            parser.NextEvent(); // EOF
            return (T) slot.Get();
        }
        
        public bool Read<T>(Bytes bytes, out T result) {
            int start = bytes.StartPos;
            int len = bytes.Len;
            Var slot = new Var();
            StubType stubType = typeCache.GetType(typeof(T));
            bool success = ReadStart(bytes.buffer, start, len, stubType, ref slot);
            if (success)
                result = (T)slot.Get();
            else
                result = default;
            parser.NextEvent(); // EOF
            return success;
        }
        
        public bool Read<T>(Bytes bytes, ref Var result) {
            int start = bytes.StartPos;
            int len = bytes.Len;
            StubType stubType = typeCache.GetType(typeof(T));
            bool success = ReadStart(bytes.buffer, start, len, stubType, ref result);
            parser.NextEvent(); // EOF
            return success;
        }
        
        public object Read(Bytes bytes, Type type) {
            int start = bytes.StartPos;
            int len = bytes.Len;
            Var slot = new Var();
            StubType stubType = typeCache.GetType(type);
            if (!ReadStart(bytes.buffer, start, len, stubType, ref slot))
                return default;
            parser.NextEvent(); // EOF
            return slot.Get();
        }

        private bool ReadStart(ByteList bytes, int offset, int len, StubType stubType, ref Var slot) {
            parser.InitParser(bytes, offset, len);
            parser.SetMaxDepth(maxDepth);
            
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                        return stubType.map.Read(this, ref slot, stubType);
                    case JsonEvent.ValueNull:
                        if (!stubType.isNullable)
                            return ReadUtils.ErrorIncompatible(this, stubType.map.DataTypeName(), stubType, ref parser);
                        slot.SetNull(stubType.varType);
                        return true;
                    case JsonEvent.Error:
                        return false;
                    default:
                        return ReadUtils.ErrorMsg(this, "unexpected state in Read() : ", ev);
                }
            }
        }

        public T ReadTo<T>(Bytes bytes, T obj, out bool success) where T : class {
            int start = bytes.StartPos;
            int len = bytes.Len;
            Var slot = new Var { Obj = obj };
            StubType stubType = typeCache.GetType(slot.Obj.GetType());
            success = ReadToStart(bytes.buffer, start, len, stubType, ref slot);
            if (!success)
                return default;
            parser.NextEvent(); // EOF
            return (T)slot.Obj;
        }

        private bool ReadToStart(ByteList bytes, int offset, int len, StubType stubType, ref Var slot) {
            parser.InitParser(bytes, offset, len);

            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ArrayStart:
                        return stubType.map.Read(this, ref slot, stubType);
                    case JsonEvent.Error:
                        return false;
                    default:
                        return ReadUtils.ErrorMsg(this, "ReadTo() can only used on an JSON object or array", ev);
                }
            }
        }
    }
}

// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Types;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public partial class JsonReader : IDisposable, IErrorHandler
    {
        public              JsonParser          parser;
        public              int                 maxDepth;
        /// <summary>Caches type mata data per thread and provide stats to the cache utilization</summary>
        public   readonly   TypeCache           typeCache;

        internal readonly   Bytes               discriminator   = new Bytes("$type");
        /// <summary>Can be used for custom mappers to create a temporary "string"
        /// without creating a string on the heap.</summary>
        public              Bytes               strBuf          = new Bytes(0);
        /// <summary>Can be used for custom mappers to lookup for a "string" in a Dictionary
        /// without creating a string on the heap.</summary>
        public readonly     BytesString         keyRef          = new BytesString();

        public              JsonError           Error => parser.error;
        public              SkipInfo            SkipInfo => parser.skipInfo;

        public              bool                throwException;
        public              IErrorHandler       errorHandler = null;

        public JsonReader(TypeStore typeStore) {
            typeCache   = new TypeCache(typeStore);
            parser      = new JsonParser {
#if !JSON_BURST
                error = { errorHandler = this }
#endif 
            };
            maxDepth    = 100;
            useIL = typeStore.typeResolver.GetConfig().useIL;
        }

        public void Dispose() {
            typeCache.      Dispose();
            discriminator.  Dispose();
            parser.         Dispose();
            strBuf.         Dispose();
            DisposePayloads();
        }
        
        public void HandleError(int pos, ref Bytes message) {
            if (errorHandler != null)
                errorHandler.HandleError(pos, ref message);
            if (throwException)
                throw new FrifloException(parser.error.msg.ToString());
        }
        
        /// <summary>
        /// Dont throw exceptions in error case, if not enabled by <see cref="throwException"/>
        /// In error case this information is available via <see cref="Error"/> 
        /// </summary>
        public T Read<T>(Bytes bytes) {
            T       value   = default;
            int     start   = bytes.StartPos;
            int     len     = bytes.Len;
            var     mapper  = (TypeMapper<T>)typeCache.GetTypeMapper(typeof(T));
            
            T result = ReadStart(bytes.buffer, start, len, mapper, value, out bool success);
            if (success == parser.error.ErrSet)
                throw new InvalidOperationException("Expect success != parser.error.ErrSet");
            if (!success)
                return default;
            parser.NextEvent(); // EOF
            return result;
        }
        
        public T Read<T>(Bytes bytes, out bool success) {
            T       value   = default;
            int     start   = bytes.StartPos;
            int     len     = bytes.Len;
            var     mapper  = (TypeMapper<T>)typeCache.GetTypeMapper(typeof(T));
            
            T result  = ReadStart(bytes.buffer, start, len, mapper, value, out success);
            parser.NextEvent(); // EOF
            return result;
        }
        
        public object ReadObject(Bytes bytes, Type type, out bool success) {
            int         start   = bytes.StartPos;
            int         len     = bytes.Len;
            TypeMapper  mapper  = typeCache.GetTypeMapper(type);
            object result = ReadStart(bytes.buffer, start, len, mapper, null, out success);
            if (success == parser.error.ErrSet)
                throw new InvalidOperationException("Expect success != parser.error.ErrSet");
            if (!success)
                return null;
            parser.NextEvent(); // EOF
            return result;
        }
        
        private object ReadStart(ByteList bytes, int offset, int len, TypeMapper mapper, object value, out bool success) {
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
                        return mapper.ReadObject(this, value, out success);
                    case JsonEvent.ValueNull:
                        if (!mapper.isNullable)
                            return ReadUtils.ErrorIncompatible<object>(this, mapper.DataTypeName(), mapper, ref parser, out success);
                        success = true;
                        return default;
                    case JsonEvent.Error:
                        success = false;
                        return default;
                    default:
                        return ReadUtils.ErrorMsg<object>(this, "unexpected state in Read() : ", ev, out success);
                }
            }
        }

        private T ReadStart<T>(ByteList bytes, int offset, int len, TypeMapper<T> mapper, T value, out bool success) {
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
                        return mapper.Read(this, value, out success);
                    case JsonEvent.ValueNull:
                        if (!mapper.isNullable)
                            return ReadUtils.ErrorIncompatible<T>(this, mapper.DataTypeName(), mapper, ref parser, out success);
                        success = true;
                        return default;
                    case JsonEvent.Error:
                        success = false;
                        return default;
                    default:
                        return ReadUtils.ErrorMsg<T>(this, "unexpected state in Read() : ", ev, out success);
                }
            }
        }

        public T ReadTo<T>(Bytes bytes, T obj, out bool success)  {
            int     start   = bytes.StartPos;
            int     len     = bytes.Len;
            var     mapper  = (TypeMapper<T>) typeCache.GetTypeMapper(obj.GetType());
            
            T result = ReadToStart(bytes.buffer, start, len, mapper, obj, out success);
            if (success == parser.error.ErrSet)
                throw new InvalidOperationException("Expect success != parser.error.ErrSet");
            if (!success)
                return default;
            parser.NextEvent(); // EOF
            return result;
        }

        private T ReadToStart<T>(ByteList bytes, int offset, int len, TypeMapper<T> mapper, T value, out bool success) {
            parser.InitParser(bytes, offset, len);
            classLevel = 0;

            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ArrayStart:
                        
                        return mapper.Read(this, value, out success);
                    case JsonEvent.Error:
                        success = false;
                        return default;
                    default:
                        return ReadUtils.ErrorMsg<T>(this, "ReadTo() can only used on an JSON object or array", ev, out success);
                }
            }
        }
        
        public object ReadObjectTo(Bytes bytes, object obj, out bool success)  {
            int     start   = bytes.StartPos;
            int     len     = bytes.Len;
            var     mapper  = typeCache.GetTypeMapper(obj.GetType());
            
            object result = ReadToStart(bytes.buffer, start, len, mapper, obj, out success);
            if (success == parser.error.ErrSet)
                throw new InvalidOperationException("Expect success != parser.error.ErrSet");
            if (!success)
                return default;
            parser.NextEvent(); // EOF
            return result;
        }

        private object ReadToStart(ByteList bytes, int offset, int len, TypeMapper mapper, object value, out bool success) {
            parser.InitParser(bytes, offset, len);
            classLevel = 0;

            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ArrayStart:
                        
                        return mapper.ReadObject(this, value, out success);
                    case JsonEvent.Error:
                        success = false;
                        return default;
                    default:
                        return ReadUtils.ErrorMsg<object>(this, "ReadTo() can only used on an JSON object or array", ev, out success);
                }
            }
        }
    }
}

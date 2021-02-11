// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Map.Utils;
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

        internal readonly   Bytes               discriminator;
        /// <summary>Can be used for custom mappers to create a temporary "string"
        /// without creating a string on the heap.</summary>
        public              Bytes               strBuf          = new Bytes(0);
        public              Bytes32             searchKey       = new Bytes32();
        public              char[]              charBuf         = new char[128];
        /// <summary>Can be used for custom mappers to lookup for a "string" in a Dictionary
        /// without creating a string on the heap.</summary>
        public readonly     BytesString         keyRef          = new BytesString();

        public              JsonError           Error => parser.error;
        public              bool                Success => !parser.error.ErrSet;
        public              SkipInfo            SkipInfo => parser.skipInfo;

        private readonly    IErrorHandler       errorHandler;

        public JsonReader(TypeStore typeStore, IErrorHandler errorHandler = null) {
            typeCache   = new TypeCache(typeStore);
            discriminator = new Bytes(typeStore.config.discriminator);
            parser = new JsonParser();
            maxDepth    = 100;
            this.errorHandler = errorHandler; 
#if !JSON_BURST
            parser.error.errorHandler = this;
#endif
// #if !UNITY_5_3_OR_NEWER
//             useIL = typeStore.config.useIL;
// #endif 
        }
        
        private void InitJsonReader(ref ByteList bytes, int offset, int len) {
            parser.InitParser(bytes, offset, len);
            parser.SetMaxDepth(maxDepth);
            InitMirrorStack();
        }

        public void Dispose() {
            typeCache.      Dispose();
            discriminator.  Dispose();
            parser.         Dispose();
            strBuf.         Dispose();
            DisposeMirrorStack();
        }
        
        public void HandleError(int pos, ref Bytes message) {
            if (errorHandler != null)
                errorHandler.HandleError(pos, ref message);
            else
                throw new JsonReaderException(parser.error.msg.ToString(), pos);
        }
        
        // --- Read()
        public T Read<T>(Bytes bytes) {
            var     mapper  = (TypeMapper<T>)typeCache.GetTypeMapper(typeof(T));
            InitJsonReader(ref bytes.buffer, bytes.StartPos, bytes.Len);
            
            T result = ReadStart(mapper, default, out bool success);
            if (!success)
                return default;
            parser.NextEvent(); // EOF
            return result;
        }
        
        public object ReadObject(Bytes bytes, Type type) {
            TypeMapper  mapper  = typeCache.GetTypeMapper(type);
            InitJsonReader(ref bytes.buffer, bytes.StartPos, bytes.Len);
            
            object result = ReadStart(mapper, null, out bool success);
            if (!success)
                return null;
            parser.NextEvent(); // EOF
            return result;
        }

        // --- ReadTo()
        public T ReadTo<T>(Bytes bytes, T obj)  {
            var     mapper  = (TypeMapper<T>) typeCache.GetTypeMapper(obj.GetType());
            InitJsonReader(ref bytes.buffer, bytes.StartPos, bytes.Len);
            
            T result = ReadToStart(mapper, obj, out bool success);
            if (!success)
                return default;
            parser.NextEvent(); // EOF
            return result;
        }

        public object ReadObjectTo(Bytes bytes, object obj)  {
            var     mapper  = typeCache.GetTypeMapper(obj.GetType());
            InitJsonReader(ref bytes.buffer, bytes.StartPos, bytes.Len);
            
            object result = ReadToStart(mapper, obj, out bool success);
            if (!success)
                return default;
            parser.NextEvent(); // EOF
            return result;
        }
        
        // --------------------------------------- private --------------------------------------- 
        private object ReadStart(TypeMapper mapper, object value, out bool success) {
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                        try {
                            return mapper.ReadObject(this, value, out success);
                        }
                        finally { ClearMirrorStack(); }
                    case JsonEvent.ValueNull:
                        if (!mapper.isNullable)
                            return ReadUtils.ErrorIncompatible<object>(this, mapper.DataTypeName(), mapper, out success);
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

        private T ReadStart<T>(TypeMapper<T> mapper, T value, out bool success) {
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                        try {
                            return mapper.Read(this, value, out success);
                        }
                        finally { ClearMirrorStack(); }
                    case JsonEvent.ValueNull:
                        if (!mapper.isNullable)
                            return ReadUtils.ErrorIncompatible<T>(this, mapper.DataTypeName(), mapper, out success);
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
        
        private T ReadToStart<T>(TypeMapper<T> mapper, T value, out bool success) {
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ArrayStart:
                        try {
                            return mapper.Read(this, value, out success);
                        }
                        finally { ClearMirrorStack(); }
                    case JsonEvent.Error:
                        success = false;
                        return default;
                    default:
                        return ReadUtils.ErrorMsg<T>(this, "ReadTo() can only used on an JSON object or array", ev, out success);
                }
            }
        }

        private object ReadToStart(TypeMapper mapper, object value, out bool success) {
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ArrayStart:
                        try {
                            return mapper.ReadObject(this, value, out success);
                        }
                        finally { ClearMirrorStack(); }
                    case JsonEvent.Error:
                        success = false;
                        return default;
                    default:
                        return ReadUtils.ErrorMsg<object>(this, "ReadTo() can only used on an JSON object or array", ev, out success);
                }
            }
        }

        public static readonly NoThrowHandler NoThrow = new NoThrowHandler();
    }
    
    public class NoThrowHandler : IErrorHandler
    {
        public void HandleError(int pos, ref Bytes message) { }
    }
    
    public class JsonReaderException : Exception {
        public readonly int position;
        
        internal JsonReaderException(string message, int position) : base(message) {
            this.position = position;
        }
    }
}

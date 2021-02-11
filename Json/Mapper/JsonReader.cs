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
            InitJsonReader(ref bytes.buffer, bytes.StartPos, bytes.Len);
            return ReadStart<T>(default);
        }
        
        public object ReadObject(Bytes bytes, Type type) {
            InitJsonReader(ref bytes.buffer, bytes.StartPos, bytes.Len);
            return ReadStart(type, null);
        }

        // --- ReadTo()
        public T ReadTo<T>(Bytes bytes, T obj)  {
            InitJsonReader(ref bytes.buffer, bytes.StartPos, bytes.Len);
            return ReadToStart(obj);
        }

        public object ReadObjectTo(Bytes bytes, object obj)  {
            InitJsonReader(ref bytes.buffer, bytes.StartPos, bytes.Len);
            return ReadToStart(obj);
        }
        
        // --------------------------------------- private --------------------------------------- 
        private object ReadStart(Type type, object value) {
            TypeMapper  mapper  = typeCache.GetTypeMapper(type);

            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                        try {
                            object result = mapper.ReadObject(this, value, out bool success);
                            if (success)
                                parser.NextEvent(); // EOF
                            return result;
                        }
                        finally { ClearMirrorStack(); }
                    case JsonEvent.ValueNull:
                        if (!mapper.isNullable)
                            return ReadUtils.ErrorIncompatible<object>(this, mapper.DataTypeName(), mapper, out bool _);
                        
                        parser.NextEvent(); // EOF
                        return default;
                    case JsonEvent.Error:
                        return default;
                    default:
                        return ReadUtils.ErrorMsg<object>(this, "unexpected state in Read() : ", ev, out bool _);
                }
            }
        }

        private T ReadStart<T>(T value) {
            TypeMapper<T>  mapper  = (TypeMapper<T>)typeCache.GetTypeMapper(typeof(T));
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                        try {
                            T result = mapper.Read(this, value, out bool success);
                            if (success)
                                parser.NextEvent(); // EOF
                            return result;
                        }
                        finally { ClearMirrorStack(); }
                    case JsonEvent.ValueNull:
                        if (!mapper.isNullable)
                            return ReadUtils.ErrorIncompatible<T>(this, mapper.DataTypeName(), mapper, out _);
                        
                        parser.NextEvent(); // EOF
                        return default;
                    case JsonEvent.Error:
                        return default;
                    default:
                        return ReadUtils.ErrorMsg<T>(this, "unexpected state in Read() : ", ev, out _);
                }
            }
        }
        
        private T ReadToStart<T>(T value) {
            TypeMapper<T> mapper  = (TypeMapper<T>) typeCache.GetTypeMapper(value.GetType());
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ArrayStart:
                        try {
                            T result = mapper.Read(this, value, out bool success);
                            if (success)
                                parser.NextEvent(); // EOF
                            return result;
                        }
                        finally { ClearMirrorStack(); }
                    case JsonEvent.Error:
                        return default;
                    default:
                        return ReadUtils.ErrorMsg<T>(this, "ReadTo() can only used on an JSON object or array", ev, out _);
                }
            }
        }

        private object ReadToStart(object value) {
            TypeMapper mapper  = typeCache.GetTypeMapper(value.GetType());
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ArrayStart:
                        try {
                            object result = mapper.ReadObject(this, value, out bool success);
                            if (success)
                                parser.NextEvent(); // EOF
                            return result;
                        }
                        finally { ClearMirrorStack(); }
                    case JsonEvent.Error:
                        return default;
                    default:
                        return ReadUtils.ErrorMsg<object>(this, "ReadTo() can only used on an JSON object or array", ev, out _);
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

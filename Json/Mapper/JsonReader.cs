// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.MapIL.Obj;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper
{

    public partial struct Reader : IErrorHandler, IDisposable {
        public              JsonParser          parser;
        internal readonly   Bytes               discriminator;
        public              Bytes               strBuf;
        public              Bytes32             searchKey;
        /// <summary>Can be used for custom mappers to create a temporary "string"
        /// without creating a string on the heap.</summary>
        public              char[]              charBuf;
        /// <summary>Can be used for custom mappers to lookup for a "string" in a Dictionary
        /// without creating a string on the heap.</summary>
        public readonly     BytesString         keyRef;
        public readonly     TypeCache           typeCache;
        private readonly    IErrorHandler       errorHandler;
#if !UNITY_5_3_OR_NEWER
        private             int                 classLevel;
        private  readonly   List<ClassMirror>   mirrorStack;
#endif

        public Reader(TypeStore typeStore, IErrorHandler errorHandler) {
            parser = new JsonParser();
            this.errorHandler = errorHandler;

            typeCache       = new TypeCache(typeStore);
            discriminator   = new Bytes(typeStore.config.discriminator);
            strBuf          = new Bytes(0);
            searchKey       = new Bytes32();
            charBuf         = new char[128];
            keyRef          = new BytesString();
#if !UNITY_5_3_OR_NEWER
            mirrorStack     = new List<ClassMirror>(16);
            classLevel      = 0;
#endif
#if !JSON_BURST
            parser.error.errorHandler = this;
#endif
        }
        
        public void HandleError(int pos, ref Bytes message) {
            if (errorHandler != null)
                errorHandler.HandleError(pos, ref message);
            else
                throw new JsonReaderException(message.ToString(), pos);
        }

        public void Dispose() {
            strBuf.         Dispose();
            discriminator.Dispose();
            typeCache.Dispose();
            parser.Dispose();
        }
    }
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class JsonReader : IDisposable
    {
        private             int                 maxDepth;
        /// <summary>Caches type mata data per thread and provide stats to the cache utilization</summary>
        // ReSharper disable once InconsistentNaming
        private             Reader              intern;

        public              JsonEvent           JsonEvent       => intern.parser.Event;
        public              JsonError           Error           => intern.parser.error;
        public              bool                Success         =>!intern.parser.error.ErrSet;
        public              SkipInfo            SkipInfo        => intern.parser.skipInfo;
        public              long                ProcessedBytes  => intern.parser.ProcessedBytes;
        public              TypeCache           TypeCache       => intern.typeCache; 

        public              int                 MaxDepth {
            get => maxDepth;
            set => maxDepth = value;
        }



        public JsonReader(TypeStore typeStore, IErrorHandler errorHandler = null) {

            intern = new Reader (typeStore, errorHandler);
            maxDepth    = 100;
// #if !UNITY_5_3_OR_NEWER
//             useIL = typeStore.config.useIL;
// #endif 
        }
        
        private void InitJsonReaderBytes(ref ByteList bytes, int offset, int len) {
            intern.parser.InitParser(bytes, offset, len);
            intern.parser.SetMaxDepth(maxDepth);
            intern.InitMirrorStack();
        }
        
        private void InitJsonReaderStream(Stream stream) {
            intern.parser.InitParser(stream);
            intern.parser.SetMaxDepth(maxDepth);
            intern.InitMirrorStack();
        }

        public void Dispose() {
            intern.         Dispose();
            intern.parser.         Dispose();
            intern.DisposeMirrorStack();
        }
        

        
        /// <summary> <see cref="JsonError.Error"/> dont call <see cref="JsonError.errorHandler"/> in
        /// JSON_BURST compilation caused by absence of interfaces. </summary>
        [Conditional("JSON_BURST")]
        private void JsonBurstError() {
            intern.HandleError(intern.parser.error.Pos, ref intern.parser.error.msg);
        }
        
        // --------------- Bytes ---------------
        // --- Read()
        public T Read<T>(Bytes bytes) {
            InitJsonReaderBytes(ref bytes.buffer, bytes.StartPos, bytes.Len);
            T result =  ReadStart<T>(default);
            JsonBurstError();
            return result;
        }
        
        public object ReadObject(Bytes bytes, Type type) {
            InitJsonReaderBytes(ref bytes.buffer, bytes.StartPos, bytes.Len);
            object result = ReadStart(type, null);
            JsonBurstError();
            return result;
        }

        // --- ReadTo()
        public T ReadTo<T>(Bytes bytes, T obj)  {
            InitJsonReaderBytes(ref bytes.buffer, bytes.StartPos, bytes.Len);
            T result = ReadToStart(obj);
            JsonBurstError();
            return result;
        }

        public object ReadObjectTo(Bytes bytes, object obj)  {
            InitJsonReaderBytes(ref bytes.buffer, bytes.StartPos, bytes.Len);
            object result = ReadToStart(obj);
            JsonBurstError();
            return result;
        }
        
        // --------------- Bytes ---------------
        // --- Read()
        public T Read<T>(Stream stream) {
            InitJsonReaderStream(stream);
            T result = ReadStart<T>(default);
            JsonBurstError();
            return result;
        }
        
        public object ReadObject(Stream stream, Type type) {
            InitJsonReaderStream(stream);
            object result = ReadStart(type, null);
            JsonBurstError();
            return result;
        }

        // --- ReadTo()
        public T ReadTo<T>(Stream stream, T obj)  {
            InitJsonReaderStream(stream);
            T result = ReadToStart(obj);
            JsonBurstError();
            return result;
        }

        public object ReadObjectTo(Stream stream, object obj)  {
            InitJsonReaderStream(stream);
            object result = ReadToStart(obj);
            JsonBurstError();
            return result;
        }

        
        // --------------------------------------- private --------------------------------------- 
        private object ReadStart(Type type, object value) {
            TypeMapper  mapper  = intern.typeCache.GetTypeMapper(type);

            while (true) {
                JsonEvent ev = intern.parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                        try {
                            object result = mapper.ReadObject(ref intern, value, out bool success);
                            if (success)
                                intern.parser.NextEvent(); // EOF
                            return result;
                        }
                        finally { intern.ClearMirrorStack(); }
                    case JsonEvent.ValueNull:
                        if (!mapper.isNullable)
                            return ReadUtils.ErrorIncompatible<object>(ref intern, mapper.DataTypeName(), mapper, out bool _);
                        
                        intern.parser.NextEvent(); // EOF
                        return default;
                    case JsonEvent.Error:
                        return default;
                    default:
                        return ReadUtils.ErrorMsg<object>(ref intern, "unexpected state in Read() : ", ev, out bool _);
                }
            }
        }

        private T ReadStart<T>(T value) {
            TypeMapper<T>  mapper  = (TypeMapper<T>)intern.typeCache.GetTypeMapper(typeof(T));
            while (true) {
                JsonEvent ev = intern.parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                        try {
                            T result = mapper.Read(ref intern, value, out bool success);
                            if (success)
                                intern.parser.NextEvent(); // EOF
                            return result;
                        }
                        finally { intern.ClearMirrorStack(); }
                    case JsonEvent.ValueNull:
                        if (!mapper.isNullable)
                            return ReadUtils.ErrorIncompatible<T>(ref intern, mapper.DataTypeName(), mapper, out _);
                        
                        intern.parser.NextEvent(); // EOF
                        return default;
                    case JsonEvent.Error:
                        return default;
                    default:
                        return ReadUtils.ErrorMsg<T>(ref intern, "unexpected state in Read() : ", ev, out _);
                }
            }
        }
        
        private T ReadToStart<T>(T value) {
            TypeMapper<T> mapper  = (TypeMapper<T>) intern.typeCache.GetTypeMapper(value.GetType());
            while (true) {
                JsonEvent ev = intern.parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ArrayStart:
                        try {
                            T result = mapper.Read(ref intern, value, out bool success);
                            if (success)
                                intern.parser.NextEvent(); // EOF
                            return result;
                        }
                        finally { intern.ClearMirrorStack(); }
                    case JsonEvent.Error:
                        return default;
                    default:
                        return ReadUtils.ErrorMsg<T>(ref intern, "ReadTo() can only used on an JSON object or array", ev, out _);
                }
            }
        }

        private object ReadToStart(object value) {
            TypeMapper mapper  = intern.typeCache.GetTypeMapper(value.GetType());
            while (true) {
                JsonEvent ev = intern.parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ArrayStart:
                        try {
                            object result = mapper.ReadObject(ref intern, value, out bool success);
                            if (success)
                                intern.parser.NextEvent(); // EOF
                            return result;
                        }
                        finally { intern.ClearMirrorStack(); }
                    case JsonEvent.Error:
                        return default;
                    default:
                        return ReadUtils.ErrorMsg<object>(ref intern, "ReadTo() can only used on an JSON object or array", ev, out _);
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

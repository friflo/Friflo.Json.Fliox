// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
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
    public interface IJsonReader
    {
        // --------------- Bytes ---------------
        // ---  Read()
        T       Read<T>(Bytes utf8Bytes);
        object  ReadObject(Bytes utf8Bytes, Type type);
        // ---  ReadTo()
        T       ReadTo<T>(Bytes utf8Bytes, T obj);
        object  ReadToObject(Bytes utf8Bytes, object obj); 
        
        // --------------- Stream ---------------
        // --- Read()
        T       Read<T>(Stream utf8Stream);
        object  ReadObject(Stream utf8Stream, Type type);
        // --- ReadTo()
        T       ReadTo<T>(Stream utf8Stream, T obj);
        object  ReadToObject(Stream utf8Stream, object obj);  
        
        // --------------- string ---------------
        // --- Read()
        T       Read<T>(string json);
        object  ReadObject(string json, Type type);
        // --- ReadTo()
        T       ReadTo<T>(string json, T obj);
        object  ReadToObject(string json, object obj); 
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class JsonReader : IJsonReader, IDisposable
    {
        private             int                 maxDepth;
        /// <summary>Caches type mata data per thread and provide stats to the cache utilization</summary>
        // ReSharper disable once InconsistentNaming
        private             Reader              intern;
        private             Bytes               inputStringBuf = new Bytes(0);

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
        
        private void InitJsonReaderString(string json) {
            inputStringBuf.Clear();
            inputStringBuf.Set(json);
            intern.parser.InitParser(inputStringBuf.buffer, inputStringBuf.start, inputStringBuf.Len);
            intern.parser.SetMaxDepth(maxDepth);
            intern.InitMirrorStack();
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
            intern.DisposeMirrorStack();
            inputStringBuf.Dispose();
        }
        

        
        /// <summary> <see cref="JsonError.Error"/> dont call <see cref="JsonError.errorHandler"/> in
        /// JSON_BURST compilation caused by absence of interfaces. </summary>
        [Conditional("JSON_BURST")]
        private void JsonBurstError() {
            if (intern.parser.error.ErrSet)
                intern.HandleError(intern.parser.error.Pos, ref intern.parser.error.msg);
        }
        
        // --------------- Bytes ---------------
        // --- Read()
        public T Read<T>(Bytes utf8Bytes) {
            InitJsonReaderBytes(ref utf8Bytes.buffer, utf8Bytes.StartPos, utf8Bytes.Len);
            T result =  ReadStart<T>(default);
            JsonBurstError();
            return result;
        }
        
        public object ReadObject(Bytes utf8Bytes, Type type) {
            InitJsonReaderBytes(ref utf8Bytes.buffer, utf8Bytes.StartPos, utf8Bytes.Len);
            object result = ReadStart(type, null);
            JsonBurstError();
            return result;
        }

        // --- ReadTo()
        public T ReadTo<T>(Bytes utf8Bytes, T obj)  {
            InitJsonReaderBytes(ref utf8Bytes.buffer, utf8Bytes.StartPos, utf8Bytes.Len);
            T result = ReadToStart(obj);
            JsonBurstError();
            return result;
        }

        public object ReadToObject(Bytes utf8Bytes, object obj)  {
            InitJsonReaderBytes(ref utf8Bytes.buffer, utf8Bytes.StartPos, utf8Bytes.Len);
            object result = ReadToStart(obj);
            JsonBurstError();
            return result;
        }
        
        // --------------- Stream ---------------
        // --- Read()
        public T Read<T>(Stream utf8Stream) {
            InitJsonReaderStream(utf8Stream);
            T result = ReadStart<T>(default);
            JsonBurstError();
            return result;
        }
        
        public object ReadObject(Stream utf8Stream, Type type) {
            InitJsonReaderStream(utf8Stream);
            object result = ReadStart(type, null);
            JsonBurstError();
            return result;
        }

        // --- ReadTo()
        public T ReadTo<T>(Stream utf8Stream, T obj)  {
            InitJsonReaderStream(utf8Stream);
            T result = ReadToStart(obj);
            JsonBurstError();
            return result;
        }

        public object ReadToObject(Stream utf8Stream, object obj)  {
            InitJsonReaderStream(utf8Stream);
            object result = ReadToStart(obj);
            JsonBurstError();
            return result;
        }
        
        // --------------- string ---------------
        // --- Read()
        public T Read<T>(string json) {
            InitJsonReaderString(json);
            T result = ReadStart<T>(default);
            JsonBurstError();
            return result;
        }
        
        public object ReadObject(string json, Type type) {
            InitJsonReaderString(json);
            object result = ReadStart(type, null);
            JsonBurstError();
            return result;
        }

        // --- ReadTo()
        public T ReadTo<T>(string json, T obj)  {
            InitJsonReaderString(json);
            T result = ReadToStart(obj);
            JsonBurstError();
            return result;

        }

        public object ReadToObject(string json, object obj)  {
            InitJsonReaderString(json);
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

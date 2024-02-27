// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics;
using System.IO;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Mapper.Utils;
using Friflo.Json.Fliox.Pools;

namespace Friflo.Json.Fliox.Mapper
{
    
    public interface IJsonReader
    {
        // --- Bytes
        T       Read<T>     (Bytes utf8Bytes);
        object  ReadObject  (Bytes utf8Bytes, Type type);

        T       ReadTo<T>   (Bytes utf8Bytes, T         obj, bool setMissingFields);
        object  ReadToObject(Bytes utf8Bytes, object    obj, bool setMissingFields); 
        
        // --- Stream
        T       Read<T>     (Stream utf8Stream);
        object  ReadObject  (Stream utf8Stream, Type type);

        T       ReadTo<T>   (Stream utf8Stream, T       obj, bool setMissingFields);
        object  ReadToObject(Stream utf8Stream, object  obj, bool setMissingFields);  
        
        // --- string
        T       Read<T>     (string json);
        object  ReadObject  (string json, Type type);

        T       ReadTo<T>   (string json, T         obj, bool setMissingFields);
        object  ReadToObject(string json, object    obj, bool setMissingFields); 
        
        //
        // --- Utf8Array
        T       Read<T>     (in JsonValue utf8Array);
        object  ReadObject  (in JsonValue utf8Array, Type type);

        T       ReadTo<T>   (in JsonValue utf8Array, T         obj, bool setMissingFields);
        object  ReadToObject(in JsonValue utf8Array, object    obj, bool setMissingFields);
    }
    
    public sealed class ObjectReader : IJsonReader, IDisposable
    {
        private             int                 maxDepth;
        private             Reader              intern;
        private             Bytes               inputStringBuf = new Bytes(0);

        public              JsonEvent           JsonEvent       => intern.parser.Event;
        public              JsonError           Error           => intern.parser.error;
        public              bool                Success         =>!intern.parser.error.ErrSet;
        public              SkipInfo            SkipInfo        => intern.parser.skipInfo;
        public              long                ProcessedBytes  => intern.parser.ProcessedBytes;
        /// <summary>Caches type meta data per thread and provide stats to the cache utilization</summary>
        public              TypeCache           TypeCache       => intern.typeCache;

        public              int                 MaxDepth {
            get => maxDepth;
            set => maxDepth = value;
        }
        
        public              IErrorHandler       ErrorHandler {
            get => intern.ErrorHandler;
            set => intern.ErrorHandler = value;
        }
        
        public              ReaderPool          ReaderPool {
            get => intern.readerPool;
            set => intern.readerPool = value;
        }

        public ObjectReader(TypeStore typeStore) {
            intern      = new Reader (typeStore);
            maxDepth    = Utf8JsonParser.DefaultMaxDepth;
        }
        
        private void InitJsonReaderString(string json, bool setMissingFields) {
            inputStringBuf.Clear();
            inputStringBuf.Set(json);
            intern.parser.InitParser(inputStringBuf.buffer, inputStringBuf.start, inputStringBuf.Len);
            intern.parser.SetMaxDepth(maxDepth);
            intern.setMissingFields = setMissingFields;
        }
        
        private void InitJsonReaderArray(in JsonValue json, bool setMissingFields) {
            intern.parser.InitParser(json);
            intern.parser.SetMaxDepth(maxDepth);
            intern.setMissingFields = setMissingFields;
        }

        private void InitJsonReaderBytes(in Bytes bytes, bool setMissingFields) {
            intern.parser.InitParser(bytes.buffer, bytes.start, bytes.Len);
            intern.parser.SetMaxDepth(maxDepth);
            intern.setMissingFields = setMissingFields;
        }
        
        private void InitJsonReaderStream(Stream stream, bool setMissingFields) {
            intern.parser.InitParser(stream);
            intern.parser.SetMaxDepth(maxDepth);
            intern.setMissingFields = setMissingFields;
        }

        public void Dispose() {
            intern.         Dispose();
            inputStringBuf.Dispose();
        }

        /// <summary> <see cref="JsonError.Error"/> don't call <see cref="JsonError.errorHandler"/> in
        /// JSON_BURST compilation caused by absence of interfaces. </summary>
        [Conditional("JSON_BURST")]
        private void JsonBurstError() {
            if (intern.parser.error.ErrSet) {
                intern.ErrorHandler?.HandleError(intern.parser.error.Pos, intern.parser.error.msg);
            }
        }
        
        // --------------- Bytes ---------------
        // --- Read()
        public T Read<T>(Bytes utf8Bytes) {
            InitJsonReaderBytes(utf8Bytes, ReaderPool != null);
            var mapper = (TypeMapper<T>)intern.typeCache.GetTypeMapper(typeof(T));
            T   result = ReadStart(default, mapper);
            JsonBurstError();
            return result;
        }
        
        public object ReadObject(Bytes utf8Bytes, Type type) {
            InitJsonReaderBytes(utf8Bytes, ReaderPool != null);
            Var result = ReadStartObject(type);
            JsonBurstError();
            return result.ToObject();
        }

        // --- ReadTo()
        public T ReadTo<T>(Bytes utf8Bytes, T obj, bool setMissingFields)  {
            InitJsonReaderBytes(utf8Bytes, setMissingFields);
            var mapper  = (TypeMapper<T>) intern.typeCache.GetTypeMapper(typeof(T));
            T   result  = ReadToStart(obj, mapper);
            JsonBurstError();
            return result;
        }

        public object ReadToObject(Bytes utf8Bytes, object obj, bool setMissingFields)  {
            InitJsonReaderBytes(utf8Bytes, setMissingFields);
            object result = ReadToStartObject(obj);
            JsonBurstError();
            return result;
        }
        
        // --------------- Stream ---------------
        // --- Read()
        public T Read<T>(Stream utf8Stream) {
            InitJsonReaderStream(utf8Stream, ReaderPool != null);
            var mapper = (TypeMapper<T>)intern.typeCache.GetTypeMapper(typeof(T));
            T   result = ReadStart(default, mapper);
            JsonBurstError();
            return result;
        }
        
        public object ReadObject(Stream utf8Stream, Type type) {
            InitJsonReaderStream(utf8Stream, ReaderPool != null);
            Var result = ReadStartObject(type);
            JsonBurstError();
            return result.ToObject();
        }

        // --- ReadTo()
        public T ReadTo<T>(Stream utf8Stream, T obj, bool setMissingFields)  {
            InitJsonReaderStream(utf8Stream, setMissingFields);
            var mapper  = (TypeMapper<T>) intern.typeCache.GetTypeMapper(typeof(T));
            T   result  = ReadToStart(obj, mapper);
            JsonBurstError();
            return result;
        }

        public object ReadToObject(Stream utf8Stream, object obj, bool setMissingFields)  {
            InitJsonReaderStream(utf8Stream, setMissingFields);
            object result = ReadToStartObject(obj);
            JsonBurstError();
            return result;
        }
        
        // --------------- string ---------------
        // --- Read()
        public T Read<T>(string json) {
            InitJsonReaderString(json, ReaderPool != null);
            var mapper = (TypeMapper<T>)intern.typeCache.GetTypeMapper(typeof(T));
            T   result = ReadStart(default, mapper);
            JsonBurstError();
            return result;
        }
        
        public object ReadObject(string json, Type type) {
            InitJsonReaderString(json, ReaderPool != null);
            Var result = ReadStartObject(type);
            JsonBurstError();
            return result.ToObject();
        }

        // --- ReadTo()
        public T ReadTo<T>(string json, T obj, bool setMissingFields)  {
            InitJsonReaderString(json, setMissingFields);
            var mapper  = (TypeMapper<T>) intern.typeCache.GetTypeMapper(typeof(T));
            T   result  = ReadToStart(obj, mapper);
            JsonBurstError();
            return result;

        }

        public object ReadToObject(string json, object obj, bool setMissingFields)  {
            InitJsonReaderString(json, setMissingFields);
            object result = ReadToStartObject(obj);
            JsonBurstError();
            return result;
        }
        
        // --------------- JsonValue ---------------
        // --- Read()
        public T Read<T>(in JsonValue json) {
            InitJsonReaderArray(json, ReaderPool != null);
            var mapper = (TypeMapper<T>)intern.typeCache.GetTypeMapper(typeof(T));
            T   result = ReadStart(default, mapper);
            JsonBurstError();
            return result;
        }
        
        /// Read() using micro optimization to avoid lookup of TypeMapper.
        public T ReadMapper<T>(TypeMapper<T> mapper, in JsonValue json) {
            InitJsonReaderArray(json, ReaderPool != null);
            T   result = ReadStart(default, mapper);
            JsonBurstError();
            return result;
        }
        
        public object ReadObject(in JsonValue json, Type type) {
            InitJsonReaderArray(json, ReaderPool != null);
            Var result = ReadStartObject(type);
            JsonBurstError();
            return result.ToObject();
        }
        
        internal Var ReadObjectVar(in JsonValue json, Type type) {
            InitJsonReaderArray(json, ReaderPool != null);
            Var result = ReadStartObject(type);
            JsonBurstError();
            return result;
        }

        // --- ReadTo()
        public T ReadTo<T>(in JsonValue json, T obj, bool setMissingFields)  {
            InitJsonReaderArray(json, setMissingFields);
            var mapper  = (TypeMapper<T>) intern.typeCache.GetTypeMapper(typeof(T));
            T   result  = ReadToStart(obj, mapper);
            JsonBurstError();
            return result;
        }
        
        /// ReadTo() using micro optimization to avoid lookup of TypeMapper.
        public T ReadToMapper<T>(TypeMapper<T> mapper, in JsonValue json, T obj, bool setMissingFields)  {
            InitJsonReaderArray(json, setMissingFields);
            T   result  = ReadToStart(obj, mapper);
            JsonBurstError();
            return result;
        }

        public object ReadToObject(in JsonValue json, object obj, bool setMissingFields)  {
            InitJsonReaderArray(json, setMissingFields);
            object result = ReadToStartObject(obj);
            JsonBurstError();
            return result;
        }

        
        // --------------------------------------- private --------------------------------------- 
        private Var ReadStartObject(Type type) {
            TypeMapper  mapper  = intern.typeCache.GetTypeMapper(type);
            Var defaultValue    = mapper.varType.DefaultValue;

            JsonEvent ev = intern.parser.NextEvent();
            switch (ev) {
                case JsonEvent.ObjectStart:
                case JsonEvent.ArrayStart:
                case JsonEvent.ValueString:
                case JsonEvent.ValueNumber:
                case JsonEvent.ValueBool:
                    Var result = mapper.ReadVar(ref intern, defaultValue, out bool success);
                    if (success)
                        intern.parser.NextEvent(); // EOF
                    return result;
                case JsonEvent.ValueNull:
                    if (!mapper.isNullable) {
                        intern.ErrorIncompatible<bool>(mapper.DataTypeName(), mapper, out bool _);
                        return defaultValue;
                    }
                    intern.parser.NextEvent(); // EOF
                    return defaultValue;
                case JsonEvent.Error:
                    return defaultValue;
                default:
                    intern.ErrorMsg<bool>("unexpected state in Read() : ", ev, out bool _);
                    return defaultValue;
            }
        }

        private T ReadStart<T>(T value, TypeMapper<T> mapper) {
            JsonEvent ev = intern.parser.NextEvent();
            switch (ev) {
                case JsonEvent.ObjectStart:
                case JsonEvent.ArrayStart:
                case JsonEvent.ValueString:
                case JsonEvent.ValueNumber:
                case JsonEvent.ValueBool:
                    T result = mapper.Read(ref intern, value, out bool success);
                    if (success)
                        intern.parser.NextEvent(); // EOF
                    return result;
                case JsonEvent.ValueNull:
                    if (!mapper.isNullable)
                        return intern.ErrorIncompatible<T>(mapper.DataTypeName(), mapper, out _);
                    
                    intern.parser.NextEvent(); // EOF
                    return default;
                case JsonEvent.Error:
                    return default;
                default:
                    return intern.ErrorMsg<T>("unexpected state in Read() : ", ev, out _);
            }
        }
        
        private T ReadToStart<T>(T value, TypeMapper<T> mapper) {
            JsonEvent ev = intern.parser.NextEvent();
            switch (ev) {
                case JsonEvent.ObjectStart:
                case JsonEvent.ArrayStart:
                    T result = mapper.Read(ref intern, value, out bool success);
                    if (success)
                        intern.parser.NextEvent(); // EOF
                    return result;
                case JsonEvent.Error:
                    return default;
                default:
                    return intern.ErrorMsg<T>("ReadTo() can only used on an JSON object or array. Found: ", ev, out _);
            }
        }

        private object ReadToStartObject(object value) {
            TypeMapper mapper  = intern.typeCache.GetTypeMapper(value.GetType());

            JsonEvent ev = intern.parser.NextEvent();
            switch (ev) {
                case JsonEvent.ObjectStart:
                case JsonEvent.ArrayStart:
                    Var valueVar = mapper.varType.FromObject(value);
                    Var result   = mapper.ReadVar(ref intern, valueVar, out bool success);
                    if (success)
                        intern.parser.NextEvent(); // EOF
                    return result.ToObject();
                case JsonEvent.Error:
                    return mapper.varType.DefaultValue;
                default:
                    intern.ErrorMsg<bool>("ReadTo() can only used on an JSON object or array. Found: ", ev, out _);
                    return mapper.varType.DefaultValue;
            }
        }
        
        public JsonEvent    NextEvent() => intern.parser.NextEvent();
        public ref Bytes    Key()       => ref intern.parser.key;
        public ref Bytes    Value()     => ref intern.parser.value;
        
        public string ErrorMsg(string module, string msg) {
            intern.parser.ErrorMsg(module, msg);
            return intern.parser.error.msg.AsString();
        }

        public bool InternalReadToObject<T>(T value) {
            TypeMapper<T> mapper  = (TypeMapper<T>) intern.typeCache.GetTypeMapper(value.GetType());
            mapper.ReadObject(ref intern, value, out bool success);
            if (success)
                intern.parser.NextEvent(); // EOF
            return success;
        }

        public static readonly NoThrowHandler NoThrow = new NoThrowHandler();
    }
    
    public sealed class NoThrowHandler : IErrorHandler
    {
        public void HandleError(int pos, in Bytes message) { }
    }
    
    public sealed class JsonReaderException : Exception {
        public readonly int position;
        
        public JsonReaderException(string message, int position) : base(message) {
            this.position = position;
        }
    }
}

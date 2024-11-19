// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.IO;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Mapper
{
    [CLSCompliant(true)]
    public interface IJsonWriter
    {
        // --- Bytes
        void        Write<T>    (T      value, ref Bytes bytes);
        void        WriteObject (object value, ref Bytes bytes);
        
        // --- Stream
        void        Write<T>    (T      value, Stream stream);
        void        WriteObject (object value, Stream stream);
        
        // --- string
        string      Write<T>    (T      value);
        string      WriteObject (object value);
        
        // --- byte[]
        JsonValue   WriteAsValue<T>     (T value);
        byte[]      WriteAsArray<T>     (T value);
        byte[]      WriteObjectAsArray  (object value);
    }
    
    [CLSCompliant(true)]
    public sealed class ObjectWriter : IJsonWriter, IDisposable
    {
        private     Writer      intern;

        public      int         Level => intern.level;
        public      int         MaxDepth {
            get => intern.maxDepth;
            set => intern.maxDepth = value;
        }
        public      bool        Pretty {
            get => intern.pretty;
            set => intern.pretty = value;
        }
        
        public      bool        WriteNullMembers {
            get => intern.writeNullMembers;
            set => intern.writeNullMembers = value;
        }
        
        public      TypeCache TypeCache => intern.typeCache;

        public ObjectWriter(TypeStore typeStore) {
            intern = new Writer(typeStore);
        }
        
        public void Dispose() {
            intern.Dispose();
        }
        
        public void SetMapperContext<T>(T mapperContext)  where T : class, IMapperContext {
            MapperContext.SetMapperContext(ref intern.contextMap, mapperContext);
        }
        
        private void InitJsonWriter() {
            intern.bytes.Clear();
            intern.level = 0;
        }

        private void InitJsonWriterBytes() {
            intern.outputType  = OutputType.ByteList;
            InitJsonWriter();
        }

        private void InitJsonWriterStream(Stream stream) {
            intern.outputType = OutputType.ByteWriter;
            IBytesWriter bytesWriter = new StreamBytesWriter(stream);
#if JSON_BURST
            intern.writerHandle = NonBurstWriter.AddWriter(bytesWriter);      
#else
            intern.bytesWriter = bytesWriter;
#endif
            InitJsonWriter();
        }
        
        private void InitJsonWriterString() {
            intern.outputType = OutputType.ByteList;
            InitJsonWriter();
        }
        
        // --------------- Bytes ---------------
        public void Write<T>(T value, ref Bytes bytes) {
            InitJsonWriterBytes();
            var mapper = (TypeMapper<T>)intern.typeCache.GetTypeMapper(typeof(T));
            WriteStart(value, mapper);
            bytes.Clear();
            bytes.AppendBytes(intern.bytes);
        }

        public void WriteObject(object value, ref Bytes bytes) {
            InitJsonWriterBytes();
            WriteStartObject(value);
            bytes.Clear();
            bytes.AppendBytes(intern.bytes);
        }
        
        // --------------- Stream ---------------

        public void Write<T>(T value, Stream stream) {
            InitJsonWriterStream(stream);
            var mapper = (TypeMapper<T>)intern.typeCache.GetTypeMapper(typeof(T));
            WriteStart(value, mapper);
            intern.Flush();
        }

        public void WriteObject(object value, Stream stream) {
            InitJsonWriterStream(stream);
            WriteStartObject(value);
            intern.Flush();
        }
        
        // --------------- string ---------------
        public string Write<T>(T value) {
            InitJsonWriterString();
            var mapper = (TypeMapper<T>)intern.typeCache.GetTypeMapper(typeof(T));
            WriteStart(value, mapper);
            return intern.bytes.AsString();
        }

        public string WriteObject(object value) {
            InitJsonWriterString();
            WriteStartObject(value);
            return intern.bytes.AsString();
        }
        
        // --------------- byte[] ---------------
        public JsonValue WriteAsValue<T>(T value) {
            InitJsonWriterString();
            var mapper = (TypeMapper<T>)intern.typeCache.GetTypeMapper(typeof(T));
            WriteStart(value, mapper);
            return new JsonValue(intern.bytes.AsArray());
        }

        public byte[] WriteAsArray<T>(T value) {
            InitJsonWriterString();
            var mapper = (TypeMapper<T>)intern.typeCache.GetTypeMapper(typeof(T));
            WriteStart(value, mapper);
            return intern.bytes.AsArray();
        }
        
        public Bytes WriteAsBytes<T>(T value) {
            InitJsonWriterString();
            var mapper = (TypeMapper<T>)intern.typeCache.GetTypeMapper(typeof(T));
            WriteStart(value, mapper);
            return intern.bytes;
        }
        
        public Bytes WriteAsBytesMapper<T>(T value, TypeMapper<T> mapper) {
            InitJsonWriterString();
            WriteStart(value, mapper);
            return intern.bytes;
        }

        public byte[] WriteObjectAsArray(object value) {
            InitJsonWriterString();
            WriteStartObject(value);
            return intern.bytes.AsArray();
        }
        
        internal byte[] WriteVarAsArray(in Var value) {
            InitJsonWriterString();
            WriteStartVar(value);
            return intern.bytes.AsArray();
        }

        // --------------------------------------- private --------------------------------------- 
        private void WriteStartObject(object value) {
            if (value == null) {
                intern.AppendNull();
                return;
            }
            TypeMapper mapper   = intern.typeCache.GetTypeMapper(value.GetType());
            var objectVar       = mapper.varType.FromObject(value);
            mapper.WriteVar(ref intern, objectVar);

            if (intern.level != 0)
                throw new InvalidOperationException($"Unexpected level after JsonWriter.Write(). Expect 0, Found: {intern.level}");
        }
        
        private void WriteStart<T>(T value, TypeMapper<T> mapper) {
            if (mapper.IsNull(ref value))
                intern.AppendNull();
            else
                mapper.Write(ref intern, value);

            if (intern.level != 0)
                throw new InvalidOperationException($"Unexpected level after JsonWriter.Write(). Expect 0, Found: {intern.level}");
        }
        
        private void WriteStartVar(in Var value) {
            if (value.IsNull) {
                intern.AppendNull();
                return;
            }
            TypeMapper mapper = intern.typeCache.GetTypeMapper(value.GetType());
            mapper.WriteVar(ref intern, value);

            if (intern.level != 0)
                throw new InvalidOperationException($"Unexpected level after JsonWriter.Write(). Expect 0, Found: {intern.level}");
        }
    }
}

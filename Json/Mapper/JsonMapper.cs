// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.IO;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map;

// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Mapper
{
    public class JsonMapper : IJsonReader, IJsonWriter, IDisposable
    {
        public readonly TypeStore   typeStore;
        public readonly JsonReader  reader;
        public readonly JsonWriter  writer;

        private         int         maxDepth;
        
        public          int         MaxDepth {
            get => maxDepth;
            set {
                maxDepth = value;
                reader.MaxDepth = value;
                writer.MaxDepth = value;
            }
        }

        public JsonMapper(ITypeResolver resolver = null, StoreConfig config = null) {
            config = config ?? new StoreConfig(TypeAccess.IL);
            typeStore   = new TypeStore(resolver, config);
            reader      = new JsonReader(typeStore);
            writer      = new JsonWriter(typeStore);
            MaxDepth    = JsonParser.DefaultMaxDepth;
        }

        public void Dispose() {
            writer.Dispose();
            reader.Dispose();
            typeStore.Dispose();
        }
        
        // --------------- Bytes ---------------
        // --- Read()
        public T Read<T>(Bytes utf8Bytes) {
            return reader.Read<T>(utf8Bytes);
        }
        
        public object ReadObject(Bytes utf8Bytes, Type type) {
            return reader.ReadObject(utf8Bytes, type);
        }

        // --- ReadTo()
        public T ReadTo<T>(Bytes utf8Bytes, T obj)  {
            return reader.ReadTo(utf8Bytes, obj);
        }

        public object ReadToObject(Bytes utf8Bytes, object obj)  {
            return reader.ReadToObject(utf8Bytes, obj);
        }
        
        // --- Write()
        public void Write<T>(T value, ref Bytes bytes) {
            writer.Write(value, ref bytes);
        }

        public void WriteObject(object value, ref Bytes bytes) {
            writer.WriteObject(value, ref bytes);
        }
        

        // --------------- Stream ---------------
        // --- Read()
        public T Read<T>(Stream utf8Stream) {
            return reader.Read<T>(utf8Stream);
        }
        
        public object ReadObject(Stream utf8Stream, Type type) {
            return reader.ReadObject(utf8Stream, type);
        }

        // --- ReadTo()
        public T ReadTo<T>(Stream utf8Stream, T obj)  {
            return reader.ReadTo(utf8Stream, obj);
        }

        public object ReadToObject(Stream utf8Stream, object obj)  {
            return reader.ReadToObject(utf8Stream, obj);
        }
        
        // --- Write()
        public void Write<T>(T value, Stream stream) {
            writer.Write(value, stream);
        }

        public void WriteObject(object value, Stream stream) {
            writer.WriteObject(value, stream);
        }
        

        // --------------- string ---------------
        // --- Read()
        public T Read<T>(string json) {
            return reader.Read<T>(json);
        }
        
        public object ReadObject(string json, Type type) {
            return reader.ReadObject(json, type);
        }

        // --- ReadTo()
        public T ReadTo<T>(string json, T obj)  {
            return reader.ReadTo(json, obj);
        }

        public object ReadToObject(string json, object obj)  {
            return reader.ReadToObject(json, obj);
        }
        
        // --- Write()
        public string Write<T>(T value) {
            return writer.Write(value);
        }

        public string WriteObject(object value) {
            return writer.WriteObject(value);
        }
        
    }
}
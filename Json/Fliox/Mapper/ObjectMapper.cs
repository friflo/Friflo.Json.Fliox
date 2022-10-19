// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.IO;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Utils;

// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.Mapper
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class ObjectMapper : IJsonReader, IJsonWriter, IDisposable, IResetable
    {
        public readonly ObjectReader    reader;
        public readonly ObjectWriter    writer;
        private         int             maxDepth;
        
        public          int             MaxDepth {
            get => maxDepth;
            set {
                maxDepth = value;
                reader.MaxDepth = value;
                writer.MaxDepth = value;
            }
        }
        
        public      bool        WriteNullMembers {
            get => writer.WriteNullMembers;
            set => writer.WriteNullMembers = value;
        }
        
        public      bool        Pretty {
            get => writer.Pretty;
            set => writer.Pretty = value;
        }
        
        public              IErrorHandler       ErrorHandler {
            get => reader.ErrorHandler;
            set => reader.ErrorHandler = value;
        }
        public ObjectMapper() : this (TypeStore.Global) { }
            
        public ObjectMapper(TypeStore typeStore) {
            typeStore       = typeStore ?? throw new ArgumentNullException(nameof(typeStore));
            reader          = new ObjectReader(typeStore);
            writer          = new ObjectWriter(typeStore);
            Reset();
        }

        public void Dispose() {
            writer.Dispose();
            reader.Dispose();
        }
        
        public void Reset() {
            ErrorHandler        = Reader.DefaultErrorHandler;
            MaxDepth            = Utf8JsonParser.DefaultMaxDepth;
            WriteNullMembers    = true;
            Pretty              = false;
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
        
        // --------------- Utf8Array ---------------
        // --- Read()
        public T Read<T>(JsonValue utf8Array) {
            return reader.Read<T>(utf8Array);
        }
        
        public object ReadObject(JsonValue utf8Array, Type type) {
            return reader.ReadObject(utf8Array, type);
        }

        // --- ReadTo()
        public T ReadTo<T>(JsonValue utf8Array, T obj)  {
            return reader.ReadTo(utf8Array, obj);
        }

        public object ReadToObject(JsonValue utf8Array, object obj)  {
            return reader.ReadToObject(utf8Array, obj);
        }
        
        // --- Write()
        public JsonValue WriteAsValue<T>(T value) {
            return writer.WriteAsValue(value);
        }
        
        public byte[] WriteAsArray<T>(T value) {
            return writer.WriteAsArray(value);
        }

        public byte[] WriteObjectAsArray(object value) {
            return writer.WriteObjectAsArray(value);
        }
        
    }
}
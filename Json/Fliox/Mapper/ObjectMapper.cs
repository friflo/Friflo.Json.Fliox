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
    [CLSCompliant(true)]
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
            reader.ReaderPool   = null;
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
        public T ReadTo<T>(Bytes utf8Bytes, T obj, bool setMissingFields)  {
            return reader.ReadTo(utf8Bytes, obj, setMissingFields);
        }

        public object ReadToObject(Bytes utf8Bytes, object obj, bool setMissingFields)  {
            return reader.ReadToObject(utf8Bytes, obj, setMissingFields);
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
        public T ReadTo<T>(Stream utf8Stream, T obj, bool setMissingFields)  {
            return reader.ReadTo(utf8Stream, obj, setMissingFields);
        }

        public object ReadToObject(Stream utf8Stream, object obj, bool setMissingFields)  {
            return reader.ReadToObject(utf8Stream, obj, setMissingFields);
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
        public T ReadTo<T>(string json, T obj, bool setMissingFields)  {
            return reader.ReadTo(json, obj, setMissingFields);
        }

        public object ReadToObject(string json, object obj, bool setMissingFields)  {
            return reader.ReadToObject(json, obj, setMissingFields);
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
        public T Read<T>(in JsonValue utf8Array) {
            return reader.Read<T>(utf8Array);
        }
        
        public object ReadObject(in JsonValue utf8Array, Type type) {
            return reader.ReadObject(utf8Array, type);
        }

        // --- ReadTo()
        public T ReadTo<T>(in JsonValue utf8Array, T obj, bool setMissingFields)  {
            return reader.ReadTo(utf8Array, obj, setMissingFields);
        }

        public object ReadToObject(in JsonValue utf8Array, object obj, bool setMissingFields)  {
            return reader.ReadToObject(utf8Array, obj, setMissingFields);
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
// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.IO;
using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Mapper
{
    internal static class Convert
    {
        // ReSharper disable once InconsistentNaming
        public static readonly Json JSON = new Json();
    }

    internal class Json : IJsonReader, IJsonWriter
    {
        public readonly TypeStore typeStore = new TypeStore();
        
        // --------------- Bytes ---------------
        // --- Read()
        public T Read<T>(Bytes utf8Bytes) {
            using (var reader = new ObjectReader(typeStore)) {
                return reader.Read<T>(utf8Bytes);
            }
        }
        
        public object ReadObject(Bytes utf8Bytes, Type type) {
            using (var reader = new ObjectReader(typeStore)) {
                return reader.ReadObject(utf8Bytes, type);
            }
        }

        // --- ReadTo()
        public T ReadTo<T>(Bytes utf8Bytes, T obj)  {
            using (var reader = new ObjectReader(typeStore)) {
                return reader.ReadTo(utf8Bytes, obj);
            }
        }

        public object ReadToObject(Bytes utf8Bytes, object obj)  {
            using (var reader = new ObjectReader(typeStore)) {
                return reader.ReadToObject(utf8Bytes, obj);
            }
        }
        
        // --- Write()
        public void Write<T>(T value, ref Bytes bytes) {
            using (var writer = new ObjectWriter(typeStore)) {
                writer.Write(value, ref bytes);
            }
        }

        public void WriteObject(object value, ref Bytes bytes) {
            using (var writer = new ObjectWriter(typeStore)) {
                writer.WriteObject(value, ref bytes);
            }
        }
        
        // --------------- Stream ---------------
        // --- Read()
        public T Read<T>(Stream utf8Stream) {
            using (var reader = new ObjectReader(typeStore)) {
                return reader.Read<T>(utf8Stream);
            }
        }
        
        public object ReadObject(Stream utf8Stream, Type type) {
            using (var reader = new ObjectReader(typeStore)) {
                return reader.ReadObject(utf8Stream, type);
            }
        }

        // --- ReadTo()
        public T ReadTo<T>(Stream utf8Stream, T obj)  {
            using (var reader = new ObjectReader(typeStore)) {
                return reader.ReadTo(utf8Stream, obj);
            }
        }

        public object ReadToObject(Stream utf8Stream, object obj)  {
            using (var reader = new ObjectReader(typeStore)) {
                return reader.ReadToObject(utf8Stream, obj);
            }
        }
        
        // --- Write()
        public void Write<T>(T value, Stream stream) {
            using (var writer = new ObjectWriter(typeStore)) {
                writer.Write(value, stream);
            }
        }

        public void WriteObject(object value, Stream stream) {
            using (var writer = new ObjectWriter(typeStore)) {
                writer.WriteObject(value, stream);
            }
        }
        
        // --------------- string ---------------
        // --- Read()
        public T Read<T>(string json) {
            using (var reader = new ObjectReader(typeStore)) {
                return reader.Read<T>(json);
            }
        }
        
        public object ReadObject(string json, Type type) {
            using (var reader = new ObjectReader(typeStore)) {
                return reader.ReadObject(json, type);
            }
        }

        // --- ReadTo()
        public T ReadTo<T>(string json, T obj)  {
            using (var reader = new ObjectReader(typeStore)) {
                return reader.ReadTo(json, obj);
            }
        }

        public object ReadToObject(string json, object obj)  {
            using (var reader = new ObjectReader(typeStore)) {
                return reader.ReadToObject(json, obj);
            }
        }
        
        // --- Write()
        public string Write<T>(T value) {
            using (var writer = new ObjectWriter(typeStore)) {
                return writer.Write(value);
            }
        }

        public string WriteObject(object value) {
            using (var writer = new ObjectWriter(typeStore)) {
                return writer.WriteObject(value);
            }
        }
        
        // --------------- Utf8Array ---------------
        // --- Read()
        public T Read<T>(Utf8Json json) {
            using (var reader = new ObjectReader(typeStore)) {
                return reader.Read<T>(json);
            }
        }
        
        public object ReadObject(Utf8Json json, Type type) {
            using (var reader = new ObjectReader(typeStore)) {
                return reader.ReadObject(json, type);
            }
        }

        // --- ReadTo()
        public T ReadTo<T>(Utf8Json json, T obj)  {
            using (var reader = new ObjectReader(typeStore)) {
                return reader.ReadTo(json, obj);
            }
        }

        public object ReadToObject(Utf8Json json, object obj)  {
            using (var reader = new ObjectReader(typeStore)) {
                return reader.ReadToObject(json, obj);
            }
        }
        
        // --------------- byte[] ---------------
        // --- Read()
        public byte[] WriteAsArray<T>(T value) {
            using (var writer = new ObjectWriter(typeStore)) {
                return writer.WriteAsArray(value);
            }
        }

        public byte[] WriteObjectAsArray(object value) {
            using (var writer = new ObjectWriter(typeStore)) {
                return writer.WriteObjectAsArray(value);
            }
        }
        
    }
}
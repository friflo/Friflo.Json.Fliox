// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Utils;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox
{
    public sealed class SerializerOptions {
        public  bool    Pretty              { get; set; }
        public  bool    WriteNullMembers    { get; set; }
        public  int     MaxDepth            { get; set; } = 100;
    }
    
    [CLSCompliant(true)]
    public static class JsonSerializer
    {
        public   static readonly    TypeStore                   DebugTypeStore  = new TypeStore();
        private  static readonly    ObjectPool<ObjectWriter>    WriterPool      = new ObjectPool<ObjectWriter> (() => new ObjectWriter(DebugTypeStore));
        private  static readonly    ObjectPool<ObjectReader>    ReaderPool      = new ObjectPool<ObjectReader> (() => new ObjectReader(DebugTypeStore));
        
        public static void Dispose() {
            WriterPool.Dispose();
        }
        
        // ------------------------------------ Serialize() ------------------------------------
        public static string Serialize<T>(T value, SerializerOptions options = null) {
            using (var pooled = WriterPool.Get()) {
                var writer = pooled.instance;
                AssignOptions (writer, options);
                return writer.Write(value);
            }
        }
        
        public static void Serialize<T>(T value, Stream stream, SerializerOptions options = null) {
            using (var pooled = WriterPool.Get()) {
                var writer = pooled.instance;
                AssignOptions (writer, options);
                writer.Write(value, stream);
            }
        }
        
        public static byte[] SerializeAsArray<T>(T value, SerializerOptions options = null) {
            using (var pooled = WriterPool.Get()) {
                var writer = pooled.instance;
                AssignOptions (writer, options);
                return writer.WriteAsArray(value);
            }
        }
        
        // ------------------------------------ Deserialize() ------------------------------------
        public static T Deserialize<T>(string json, SerializerOptions options = null) {
            using (var pooled = ReaderPool.Get()) {
                var reader = pooled.instance;
                AssignOptions(reader, options);
                return reader.Read<T>(json);
            }
        }
        
        public static T Deserialize<T>(Stream stream, SerializerOptions options = null) {
            using (var pooled = ReaderPool.Get()) {
                var reader = pooled.instance;
                AssignOptions(reader, options);
                return reader.Read<T>(stream);
            }
        }
        
        public static T Deserialize<T>(in JsonValue json, SerializerOptions options = null) {
            using (var pooled = ReaderPool.Get()) {
                var reader = pooled.instance;
                AssignOptions(reader, options);
                return reader.Read<T>(json);
            }
        }
        
        // --- private
        private static void AssignOptions(ObjectWriter writer, SerializerOptions options) {
            writer.WriteNullMembers = options?.WriteNullMembers ?? false;
            writer.Pretty           = options?.Pretty           ?? false;
            writer.MaxDepth         = options?.MaxDepth         ?? 100;
        }
        private static void AssignOptions(ObjectReader reader, SerializerOptions options) {
            reader.MaxDepth         = options?.MaxDepth         ?? 100;
        }
    }
}
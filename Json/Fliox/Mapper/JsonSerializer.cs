// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.IO;
using Friflo.Json.Fliox.Utils;

namespace Friflo.Json.Fliox.Mapper
{
    public class SerializerOptions {
        public bool Pretty              { get; set; }
        public bool WriteNullMembers    { get; set; }
    }
    
    public static class JsonSerializer
    {
        public   static readonly    TypeStore                   DebugTypeStore  = new TypeStore();
        private  static readonly    SharedPool<ObjectWriter>    WriterPool      = new SharedPool<ObjectWriter> (() => new ObjectWriter(DebugTypeStore));
        private  static readonly    SharedPool<ObjectReader>    ReaderPool      = new SharedPool<ObjectReader> (() => new ObjectReader(DebugTypeStore));
        
        public static void Dispose() {
            WriterPool.Dispose();
        }
        
        // ------------------------------------ Serialize() ------------------------------------
        public static string Serialize<T>(T value, SerializerOptions options = null) {
            using (var pooled = WriterPool.Get()) {
                var writer = pooled.instance;
                writer.WriteNullMembers = options?.WriteNullMembers ?? false;
                writer.Pretty           = options?.Pretty           ?? false;
                return writer.Write(value);
            }
        }
        
        public static void Serialize<T>(T value, Stream stream, SerializerOptions options = null) {
            using (var pooled = WriterPool.Get()) {
                var writer = pooled.instance;
                writer.WriteNullMembers = options?.WriteNullMembers ?? false;
                writer.Pretty           = options?.Pretty           ?? false;
                writer.Write(value, stream);
            }
        }
        
        public static byte[] SerializeAsArray<T>(T value, SerializerOptions options = null) {
            using (var pooled = WriterPool.Get()) {
                var writer = pooled.instance;
                writer.WriteNullMembers = options?.WriteNullMembers ?? false;
                writer.Pretty           = options?.Pretty           ?? false;
                return writer.WriteAsArray(value);
            }
        }
        
        // ------------------------------------ Deserialize() ------------------------------------
        public static T Deserialize<T>(string json, SerializerOptions options = null) {
            using (var pooled = ReaderPool.Get()) {
                var reader = pooled.instance;
                return reader.Read<T>(json);
            }
        }
        
        public static T Deserialize<T>(Stream stream, SerializerOptions options = null) {
            using (var pooled = ReaderPool.Get()) {
                var reader = pooled.instance;
                return reader.Read<T>(stream);
            }
        }
        
        public static T Deserialize<T>(JsonValue json, SerializerOptions options = null) {
            using (var pooled = ReaderPool.Get()) {
                var reader = pooled.instance;
                return reader.Read<T>(json);
            }
        }
    }
}
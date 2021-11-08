// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

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
        
        public static void Dispose() {
            WriterPool.Dispose();
        }
        
        public static string Serialize<T>(T value, SerializerOptions options = null) {
            using (var pooledWriter = WriterPool.Get()) {
                var writer = pooledWriter.instance;
                writer.WriteNullMembers = options?.WriteNullMembers ?? false;
                writer.Pretty           = options?.Pretty           ?? false;
                return writer.Write(value);
            }
        }
    }
}
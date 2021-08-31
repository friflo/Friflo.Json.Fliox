// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Utils;

namespace Friflo.Json.Fliox.Mapper
{
    public static class JsonDebug
    {
        public   static readonly    TypeStore                   DebugTypeStore  = new TypeStore();
        private  static readonly    SharedPool<ObjectWriter>    WriterPool      = new SharedPool<ObjectWriter> (() => new ObjectWriter(DebugTypeStore));
        private  static             bool                        _loggedWarning;
        
        public static void Dispose() {
            WriterPool.Dispose();
        }
        
        /// <summary>
        /// <see cref="ToJson{T}"/> should be used only for debugging purposes - not for production code as it
        /// uses a global <see cref="DebugTypeStore"/> instance which should be avoided.
        /// </summary>
        public static string ToJson<T>(T value, bool pretty) {
            if (!_loggedWarning) {
                Console.WriteLine("warn: JsonDebug.ToJson() should be called only for debugging purposes.");
                _loggedWarning = true;
            }
            using (var pooledWriter = WriterPool.Get()) {
                var writer = pooledWriter.instance;
                writer.Pretty = pretty;
                return writer.Write(value);
            }
        }
    }
}
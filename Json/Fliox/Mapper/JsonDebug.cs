// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Fliox.Mapper
{
    public static class JsonDebug
    {
        public   static readonly    TypeStore   DebugTypeStore  = new TypeStore();
        private  static             bool        _loggedWarning;
        
        /// <summary>
        /// ATTENTION: <see cref="ToJson{T}"/> is only for debugging purposes! NOT for PRODUCTION code! 
        /// <br/>
        /// It allocates high amount of resources 
        /// </summary>
        public static string ToJson<T>(T value, bool pretty) {
            if (!_loggedWarning) {
                Console.WriteLine("warning: JsonDebug.ToJson() must be called only for debugging purposes.");
                _loggedWarning = true;
            }
            using (var writer = new  ObjectWriter(DebugTypeStore)) {
                writer.Pretty = pretty;
                return writer.Write(value);
            }
        }
    }
}
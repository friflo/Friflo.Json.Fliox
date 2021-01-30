// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public partial class JsonWriter : IDisposable
    {
        /// <summary>Caches type mata data per thread and provide stats to the cache utilization</summary>
        public readonly     TypeCache   typeCache;
        public              Bytes       bytes;
        /// <summary>Can be used for custom mappers append a number while creating the JSON payload</summary>
        public              ValueFormat format;
        /// <summary>Can be used for custom mappers to create a temporary "string"
        /// without creating a string on the heap.</summary>
        public              Bytes       strBuf;

        internal            Bytes       @null = new Bytes("null");
        internal            Bytes       discriminator = new Bytes("\"$type\":\"");

        public          ref Bytes Output => ref bytes;

        internal            int         level;
        public              int         Level => level;
        public              int         maxDepth;
        

        public JsonWriter(TypeStore typeStore) {
            typeCache = new TypeCache(typeStore);
            maxDepth = 100;
            useIL = typeStore.typeResolver.GetConfig().useIL;
        }
        
        public void Dispose() {
            typeCache.Dispose();
            @null.Dispose();
            discriminator.Dispose();
            format.Dispose();
            strBuf.Dispose();
            bytes.Dispose();
            DisposeMirrorStack();
        }

        public void WriteObject(object value) { 
            TypeMapper stubType = typeCache.GetTypeMapper(value.GetType());
            WriteStart(stubType, value);
        }
        
        public void Write<T>(T value) {
            var mapper = (TypeMapper<T>)typeCache.GetTypeMapper(typeof(T));
            
            WriteStart(mapper, value);
        }
        
        private void WriteStart(TypeMapper mapper, object value) {
            bytes.  InitBytes(128);
            strBuf. InitBytes(128);
            format. InitTokenFormat();
            bytes.Clear();
            level = 0;
            if (value == null) {
                WriteUtils.AppendNull(this);
            } else {
                try {
                    mapper.WriteObject(this, value);
                }
                finally { ClearMirrorStack(); }
            }

            if (level != 0)
                throw new InvalidOperationException($"Unexpected level after JsonWriter.Write(). Expect 0, Found: {level}");
        }
        
        private void WriteStart<T>(TypeMapper<T> mapper, T value) {
            bytes.  InitBytes(128);
            strBuf. InitBytes(128);
            format. InitTokenFormat();
            bytes.Clear();
            level = 0;
            if (EqualityComparer<T>.Default.Equals(value, default)) {
                WriteUtils.AppendNull(this);
            } else {
                try {
                    mapper.Write(this, value);
                }
                finally { ClearMirrorStack(); }
            }

            if (level != 0)
                throw new InvalidOperationException($"Unexpected level after JsonWriter.Write(). Expect 0, Found: {level}");
        }
    }
}

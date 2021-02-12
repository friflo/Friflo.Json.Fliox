// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.MapIL.Obj;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper
{
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public partial struct Writer : IDisposable
    {
        /// <summary>Caches type mata data per thread and provide stats to the cache utilization</summary>
        public readonly     TypeCache           typeCache;
        public              Bytes               bytes;
        /// <summary>Can be used for custom mappers append a number while creating the JSON payload</summary>
        public              ValueFormat         format;
        /// <summary>Can be used for custom mappers to create a temporary "string"
        /// without creating a string on the heap.</summary>
        public              Bytes               strBuf;

        internal            Bytes               @null;
        internal            Bytes               discriminator;
        internal            int                 level;
        public              int                 maxDepth;
#if !UNITY_5_3_OR_NEWER
        private             int                 classLevel;
        private  readonly   List<ClassMirror>   mirrorStack;
#endif

        public Writer(TypeStore typeStore) {
            bytes           = new Bytes(128);
            strBuf          = new Bytes(128);
            format          = new ValueFormat();
            format. InitTokenFormat();
            @null           = new Bytes("null");
            discriminator   = new Bytes($"\"{typeStore.config.discriminator}\":\"");
            typeCache       = new TypeCache(typeStore);
            level           = 0;
            maxDepth        = 100;
#if !UNITY_5_3_OR_NEWER
            classLevel      = 0;
            mirrorStack     = new List<ClassMirror>(16);
#endif
        }
        
        public void Dispose() {
            typeCache.Dispose();
            discriminator.Dispose();
            @null.Dispose();
            format.Dispose();
            strBuf.Dispose();
            bytes.Dispose();
            DisposeMirrorStack();
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class JsonWriter : IDisposable
    {
        private     Writer      intern;
        public  ref Bytes       Output => ref intern.bytes;

        public      int         Level => intern.level;
        public      int         MaxDepth {
            get => intern.maxDepth;
            set => intern.maxDepth = value;
        }

        public JsonWriter(TypeStore typeStore) {
            intern = new Writer(typeStore);
        }
        
        public void Dispose() {
            intern.Dispose();
        }

        private void InitJsonWriter() {
            intern.bytes.Clear();
            intern.level = 0;
            intern.InitMirrorStack();
        }
        
        // --------------- Bytes ---------------  todo
        public void Write<T>(T value) {
            WriteStart(value);
        }

        public void WriteObject(object value) {
            WriteStart(value);
        }

        // --------------------------------------- private --------------------------------------- 
        private void WriteStart(object value) {
            if (value == null) {
                WriteUtils.AppendNull(ref intern);
                return;
            }
            TypeMapper mapper = intern.typeCache.GetTypeMapper(value.GetType());
            InitJsonWriter();
            try {
                mapper.WriteObject(ref intern, value);
            }
            finally { intern.ClearMirrorStack(); }

            if (intern.level != 0)
                throw new InvalidOperationException($"Unexpected level after JsonWriter.Write(). Expect 0, Found: {intern.level}");
        }
        
        private void WriteStart<T>(T value) {
            var mapper = (TypeMapper<T>)intern.typeCache.GetTypeMapper(typeof(T));
            InitJsonWriter();
            try {
                if (mapper.IsNull(ref value))
                    WriteUtils.AppendNull(ref intern);
                else
                    mapper.Write(ref intern, value);
            }
            finally { intern.ClearMirrorStack(); }
            

            if (intern.level != 0)
                throw new InvalidOperationException($"Unexpected level after JsonWriter.Write(). Expect 0, Found: {intern.level}");
        }
    }
}

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Mapper.MapIL.Obj;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map
{
    enum OutputType {
        ByteList,
        ByteWriter,
    }
    
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

        internal            OutputType          outputType;
#if JSON_BURST
        // private          int                 writerHandle;
#else
        public              IBytesWriter        bytesWriter;
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
            outputType      = OutputType.ByteList;
#if !JSON_BURST
            bytesWriter     = null;
#endif
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
}

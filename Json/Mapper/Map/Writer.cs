using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Mapper.Map.Utils;
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
        internal            Bytes               @null;
        internal            Bytes               discriminator;
        internal            int                 level;
        public              int                 maxDepth;
        public              bool                pretty;
#if !UNITY_5_3_OR_NEWER
        private             int                 classLevel;
        private  readonly   List<ClassMirror>   mirrorStack;
#endif

        internal            OutputType          outputType;
#if JSON_BURST
        public              int                 writerHandle;
#else
        public              IBytesWriter        bytesWriter;
#endif

        public Writer(TypeStore typeStore) {
            bytes           = new Bytes(128);
            format          = new ValueFormat();
            format. InitTokenFormat();
            @null           = new Bytes("null");
            discriminator   = new Bytes($"\"{typeStore.config.discriminator}\":\"");
            typeCache       = new TypeCache(typeStore);
            level           = 0;
            maxDepth        = JsonParser.DefaultMaxDepth;
            outputType      = OutputType.ByteList;
            pretty          = false;
#if JSON_BURST
            writerHandle    = -1;
#else
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
            bytes.Dispose();
            DisposeMirrorStack();
        }
        
        // --- WriteUtils
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteString(String str) {
            JsonSerializer.AppendEscString(ref bytes, in str);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendNull() {
            bytes.AppendBytes(ref @null);
            WriteUtils.FlushFilledBuffer(ref this);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IncLevel() {
            if (level++ < maxDepth)
                return level;
            throw new InvalidOperationException($"JsonParser: maxDepth exceeded. maxDepth: {maxDepth}");
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DecLevel(int expectedLevel) {
            if (level-- != expectedLevel)
                throw new InvalidOperationException($"Unexpected level in Write() end. Expect {expectedLevel}, Found: {level + 1}");
        }
    }
}

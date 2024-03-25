// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;

namespace Friflo.Json.Fliox.Pools
{
    /// <summary>
    /// A pool for class instances of all types defined in a <see cref="TypeStore"/>.<br/>
    /// By assigning to <see cref="ObjectReader.ReaderPool"/> pooled instances are reused when deserializing JSON
    /// with <see cref="ObjectReader"/> <b>Read()</b> methods.<br/>
    /// The pool is not utilized when using the <see cref="ObjectReader"/> <b>ReadTo()</b> methods.
    /// </summary>
    /// <remarks> <see cref="ReaderPool"/> is not thread safe </remarks>
    public sealed class ReaderPool : IDisposable
    {
        private             PoolIntern<object>[]    pools;
        private             int                     poolCount;
        private             int                     version;
        private             byte[]                  buffer;
        private             int                     bufferPos;
#if DEBUG
        private readonly    TypeStore               typeStore;
#endif
        private const       int                     BufferMax = 16 * 1024;

        public   override   string                  ToString() => GetString();
        
        public ReaderPool(TypeStore typeStore) {
            pools           = Array.Empty<PoolIntern<object>>();
            buffer          = new byte[128];
#if DEBUG
            this.typeStore  = typeStore;
#endif
        }

        public void Dispose() { }

        /// <summary>
        /// Make pooled class instances available for reuse.<br/>
        /// These instances were created when using the <see cref="ReaderPool"/> in a previous <see cref="Reuse"/> cycle.
        /// </summary>
        public ReaderPool Reuse() {
            version++;
            bufferPos = 0;
            return this;
        }
        
        public T Create<T>(TypeMapper<T> mapper) {
            return (T)CreateObject(mapper);
        }
        
        public object CreateObject(TypeMapper mapper)
        {
#if DEBUG
            if (typeStore != mapper.typeStore)
                throw new InvalidOperationException($"used TypeMapper from a different TypeStore.Type {mapper.type}");
#endif
            var classId = mapper.classId;
            if (classId < poolCount) {
                ref var pool    = ref pools[classId];
                var objects     = pool.objects;
                if (objects != null) {
                    if (pool.version != version) {
                        pool.version = version;
                        if (pool.count > 0) {
                            pool.used = 1;
                            return objects[0];
                        }
                        return pool.Create(mapper.NewInstance);
                    }
                    int used = pool.used;
                    if (used < pool.count) {
                        pool.used++;
                        return objects[used];
                    }
                    return pool.Create(mapper.NewInstance);
                }
            }
            return CreateInstancePool(mapper);
        }
        
        private object CreateInstancePool(TypeMapper mapper) {
            var count           = poolCount;
            var classId         = mapper.classId;
            poolCount           = Math.Max(classId + 1, count);
            var newPool         = new PoolIntern<object>(new object[4]) { version = version };
            var instance        = newPool.Create(mapper.NewInstance);
            var newPools        = new PoolIntern<object>[poolCount];
            for (int n = 0; n < count; n++) {
                newPools[n] = pools[n];
            }
            pools           = newPools;
            pools[classId]  = newPool;
            return instance;
        }

        
        internal JsonValue CreateJsonValue(in Bytes value) {
            var len         = value.end - value.start;
            if (len > BufferMax) {
                return new JsonValue(value.AsArray());
            }
            var srcArray    = value.buffer;
            var remaining   = buffer.Length - bufferPos;
            if (len <= remaining) {
                var start    = bufferPos;
                bufferPos   += len;
                Buffer.BlockCopy(srcArray, 0, buffer, start, len);
                return new JsonValue(buffer, start, len);
            }
            var newBufferLen    = Math.Max(2 * buffer.Length, len); 
            newBufferLen        = Math.Min(newBufferLen, BufferMax);
            buffer              = new byte[newBufferLen];
            bufferPos           = len;
            Buffer.BlockCopy(srcArray, 0, buffer, 0, len);
            return new JsonValue(buffer, 0, len);
        }
        
        private string GetString() {
            var used        = 0;
            var count       = 0;
            var typeCount   = 0;
            for (int n = 0; n < poolCount; n++) {
                ref var pool = ref pools[n];
                count       += pool.count;
                if (pool.objects == null)
                    continue;
                typeCount++;
                if (version == pool.version) {
                    used    += pool.used;
                }
            }
            return $"count: {count}, used: {used}, types: {typeCount}, version: {version}";
        }
    }
}
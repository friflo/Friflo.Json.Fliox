// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper.Map;

namespace Friflo.Json.Fliox.Mapper
{
    /// <summary>
    /// A pool for class instances of all types defined in a <see cref="TypeStore"/>.<br/>
    /// Pooled instances are reused when deserializing JSON using an <see cref="ObjectReader"/>
    /// </summary>
    public class InstancePool
    {
        private             ClassPool[]     pools;
        private readonly    TypeStore       typeStore;
        private             int             poolCount;
        private             int             version;
        
        public   override   string          ToString() => GetString();
        
        public InstancePool(TypeStore typeStore) {
            pools           = Array.Empty<ClassPool>();
            this.typeStore  = typeStore;
        }
        
        public void Reuse() {
            version++;
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
            var id = mapper.id;
            if (id < poolCount) {
                ref var pool    = ref pools[id];
                var objects     = pool.objects;
                if (objects != null) {
                    if (pool.version != version) {
                        pool.version = version;
                        if (pool.count > 0) {
                            pool.used = 1;
                            return objects[0];
                        }
                        return pool.Create(mapper);
                    }
                    int used = pool.used;
                    if (used < pool.count) {
                        pool.used++;
                        return objects[used];
                    }
                    return pool.Create(mapper);
                }
            }
            return CreateInstancePool(mapper);
        }
        
        private object CreateInstancePool(TypeMapper mapper) {
            var count           = poolCount;
            var id              = mapper.id;
            poolCount           = Math.Max(id + 1, count);
            var newPool         = new ClassPool( new List<object>() ) { version = version };
            var instance        = newPool.Create(mapper);
            var newPools        = new ClassPool[poolCount];
            for (int n = 0; n < count; n++) {
                newPools[n] = pools[n];
            }
            pools       = newPools;
            pools[id]   = newPool;
            return instance;
        }
        
        private string GetString() {
            var used        = 0;
            var count       = 0;
            var typeCount   = 0;
            for (int n = 0; n < poolCount; n++) {
                ref var pool = ref pools[n];
                used        += pool.used;
                count       += pool.count;
                typeCount   += pool.objects != null ? 1 : 0;
            }
            return $"count: {count} used: {used} types: {typeCount} version: {version}";
        }
    }
    
    /// <summary> Contain pooled instances of a specific type </summary>
    internal struct ClassPool
    {
        internal readonly   List<object>    objects;
        internal            int             used;
        internal            int             count;
        internal            int             version;

        public   override   string          ToString() => GetString();

        internal ClassPool(List<object> objects) {
            this.objects    = objects;
            used            =  0;
            count           =  0;
            version         = -1;
        }
        
        internal object Create(TypeMapper mapper) {
            used++;
            count++;
            var instance = mapper.NewInstance();
            objects.Add(instance);
            return instance;               
        }
        
        private string GetString() {
            if (objects == null)
                return "";
            var type        = objects[0].GetType();
            var typeName    = VarType.GetTypeName(type);
            return $"count: {count} used: {used} - {typeName}";
        }
    }
}
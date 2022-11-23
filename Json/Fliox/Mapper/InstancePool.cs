// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Mapper.Map;

namespace Friflo.Json.Fliox.Mapper
{
    /// <summary>
    /// A pool for class instances of all types defined in a <see cref="TypeStore"/>.<br/>
    /// Pooled instances are reused when deserializing JSON using an <see cref="ObjectReader"/>
    /// </summary>
    public class InstancePool
    {
        private             ClassPoolIntern<object>[]   pools;
        private             int                         poolCount;
        private             int                         version;
#if DEBUG
        private readonly    TypeStore                   typeStore;
#endif
        
        public   override   string                      ToString() => GetString();
        
        public InstancePool(TypeStore typeStore) {
            pools           = Array.Empty<ClassPoolIntern<object>>();
#if DEBUG
            this.typeStore  = typeStore;
#endif
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
            var newPool         = new ClassPoolIntern<object>(new object[4]) { version = version };
            var instance        = newPool.Create(mapper.NewInstance);
            var newPools        = new ClassPoolIntern<object>[poolCount];
            for (int n = 0; n < count; n++) {
                newPools[n] = pools[n];
            }
            pools           = newPools;
            pools[classId]  = newPool;
            return instance;
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
            return $"count: {count} used: {used} types: {typeCount} version: {version}";
        }
    }
    

    

}
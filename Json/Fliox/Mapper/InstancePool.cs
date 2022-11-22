// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper.Map;

namespace Friflo.Json.Fliox.Mapper
{
    public class InstancePool
    {
        private Instances[] instancePools       = Array.Empty<Instances>();
        private int         instancePoolsCount;
        private int         version;
        
        public void Reuse() {
            version++;
        }
        
        public object Create(TypeMapper mapper)
        {
            var id = mapper.id;
            if (id < instancePoolsCount) {
                ref var instancePool    = ref instancePools[id];
                var instances           = instancePool.instances;
                if (instances != null) {
                    if (instancePool.version != version) {
                        instancePool.version = version;
                        if (instancePool.count > 0) {
                            instancePool.used = 1;
                            return instances[0];
                        }
                        return instancePool.Create(mapper);
                    }
                    int used = instancePool.used;
                    if (used < instancePool.count) {
                        instancePool.used++;
                        return instances[used];
                    }
                    return instancePool.Create(mapper);
                }
            }
            return CreateInstancePool(mapper);
        }
        
        private object CreateInstancePool(TypeMapper mapper) {
            var count           = instancePoolsCount;
            var id              = mapper.id;
            instancePoolsCount  = Math.Max(id + 1, count);
            var newPool         = new Instances( new List<object>() ) { version = version };
            var instance        = newPool.Create(mapper);
            var newPools        = new Instances[instancePoolsCount];
            for (int n = 0; n < count; n++) {
                newPools[n] = instancePools[n];
            }
            instancePools       = newPools;
            instancePools[id]   = newPool;
            return instance;
        }
    }
    
    internal struct Instances
    {
        internal readonly   List<object>    instances;
        internal            int             used;
        internal            int             count;
        internal            int             version;
        
        internal Instances(List<object> instances) {
            this.instances  = instances;
            used            =  0;
            count           =  0;
            version         = -1;
        }
        
        internal object Create(TypeMapper mapper) {
            used++;
            var instance = mapper.CreateInstance();
            count++;
            instances.Add(instance);
            return instance;               
        }
    }
}
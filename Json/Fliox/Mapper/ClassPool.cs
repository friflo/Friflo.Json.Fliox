// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Mapper
{
    /// <summary>
    /// A pool for class instances of reference types.
    /// </summary>
    public class ClassPools
    {
        internal            int             version;
        //  internal readonly   List<ClassPool> pools = new List<ClassPool>();

        public void Reuse() {
            version++;
        }
    }
    
    public class ClassPool { } 

    /// <summary> Contain pooled instances of a specific type </summary>
    public class ClassPool<T> : ClassPool where T : new()
    {
        private  readonly   ClassPools          pools;
        private             ClassPoolIntern<T>  pool;
        
        public ClassPool(ClassPools pools) {
            this.pools  = pools;
            // pools.pools.Add(this);
            pool      = new ClassPoolIntern<T>(new T[4]);
        }
        
        private static T NewInstance() => new T();

        public T Create()
        {
            var version = pools.version;
            var objects = pool.objects;
            if (pool.version != version) {
                pool.version = version;
                if (pool.count > 0) {
                    pool.used = 1;
                    return objects[0];
                }
                return pool.Create(NewInstance);
            }
            int used = pool.used;
            if (used < pool.count) {
                pool.used++;
                return objects[used];
            }
            return pool.Create(NewInstance);
        }
    }
}
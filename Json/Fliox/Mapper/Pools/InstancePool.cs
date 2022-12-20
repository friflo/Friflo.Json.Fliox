// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper.Map;

namespace Friflo.Json.Fliox.Mapper.Pools
{
    /// <summary>
    /// A pool for class instances of reference types.
    /// </summary>
    public sealed class ClassPools
    {
        internal            int             version;
        internal  readonly  TypeStore       typeStore;
        internal  readonly  List<InstancePool> pools;
        public    override  string          ToString() => GetString();

        public ClassPools(TypeStore typeStore) {
            this.typeStore  = typeStore;
            pools           = new List<InstancePool>();
        }

        public void Reuse() {
            version++;
        }
        
        private string GetString() {
            var used        = 0;
            var count       = 0;
            var typeCount   = 0;
            foreach (var pool in pools) {
                count       += pool.Count;
                typeCount++;
                if (version == pool.Version) {
                    used    += pool.Used;
                }
            }
            return $"count: {count}, used: {used}, types: {typeCount}, version: {version}";
        }
    }
    
    public abstract class InstancePool
    {
        internal  abstract  int     Used    { get; }
        internal  abstract  int     Version { get; }
        internal  abstract  int     Count   { get; }
    } 

    /// <summary> Contain pooled instances of a specific type </summary>
    public sealed class InstancePool<T> : InstancePool where T : class, new() // constraint class is not necessary but improves new T() calls.
    {
        private  readonly   ClassPools          pools;
        private  readonly   TypeMapper<T>       mapper;
        private             ClassPoolIntern<T>  pool;
        
        internal  override  int                 Used        => pool.used;
        internal  override  int                 Version     => pool.version;
        internal  override  int                 Count       => pool.count;
        public    override  string              ToString()  => pool.GetString();
        
        public InstancePool(ClassPools pools) {
            this.pools  = pools;
            pools.pools.Add(this);
            mapper      = pools.typeStore.GetTypeMapper<T>();
            pool        = new ClassPoolIntern<T>(new T[4]);
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
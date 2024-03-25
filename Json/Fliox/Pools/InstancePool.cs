// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.


namespace Friflo.Json.Fliox.Pools
{
    public abstract class InstancePool
    {
        internal  abstract  int     Used    { get; }
        internal  abstract  int     Version { get; }
        internal  abstract  int     Count   { get; }
    } 

    /// <summary> Contain pooled instances of a specific type </summary>
    /// <remarks> <see cref="InstancePool{T}"/> is not thread safe </remarks>
    public sealed class InstancePool<T> : InstancePool where T : class, new() // constraint class is not necessary but improves new T() calls.
    {
        private  readonly   InstancePools   pools;
        private             PoolIntern<T>   pool;
        
        internal  override  int             Used        => pool.used;
        internal  override  int             Version     => pool.version;
        internal  override  int             Count       => pool.count;
        public    override  string          ToString()  => pool.GetString();
        
        public InstancePool(InstancePools pools) {
            this.pools  = pools;
            pools.pools.Add(this);
            pool        = new PoolIntern<T>(new T[4]);
        }
        
        private static T NewInstance() => new T();

        public T Create()
        {
            var curVersion  = pools.version;
            var objects     = pool.objects;
            if (pool.version != curVersion) {
                pool.version = curVersion;
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
// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Concurrent;

namespace Friflo.Json.Flow.Database
{
    public readonly struct Pooled<T> : IDisposable where T : IDisposable
    {
        public  readonly T              value;
        private readonly ObjectPool<T>  pool;
        
        internal Pooled(ObjectPool<T> pool, T value) {
            this.value  = value;
            this.pool   = pool;
        }

        public void Dispose() {
            pool.Return(value);
        }
    }
    
    public class ObjectPool<T> : IDisposable where T : IDisposable
    {
        private readonly ConcurrentQueue<T> queue = new ConcurrentQueue<T>();
        private readonly Func<T>            factory;

        public ObjectPool(Func<T> factory) {
            this.factory = factory;
        }

        public Pooled<T> Get() {
            if (!queue.TryDequeue(out T obj)) {
                obj = factory();
            }
            return new Pooled<T>(this, obj);
        }
        
        internal void Return(T obj) {
            queue.Enqueue(obj);
        }

        public void Dispose() {
            foreach (var obj in queue) {
                obj.Dispose();
            }
        }
    }
}
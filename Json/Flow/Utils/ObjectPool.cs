// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;

namespace Friflo.Json.Flow.Utils
{
    public readonly struct Pooled<T> : IDisposable where T : IDisposable
    {
        public  readonly T              instance;
        private readonly ObjectPool<T>  pool;
        
        internal Pooled(ObjectPool<T> pool, T instance) {
            this.instance   = instance;
            this.pool       = pool;
        }

        public void Dispose() {
            pool.Return(instance);
        }
    }
    
    public abstract class ObjectPool<T> where T : IDisposable
    {
        internal abstract T     Get();
        internal abstract void  Return(T obj);
        public   abstract void  Dispose();
        public   abstract void  AssertNoLeaks();
        
        public Pooled<T>     GetPooled() {
            return new Pooled<T>(this, Get());
        }
    }
    
    public class SharedPool<T> : ObjectPool<T> where T : IDisposable
    {
        private readonly    ConcurrentQueue<T>  queue = new ConcurrentQueue<T>();
        private readonly    Func<T>             factory;
        
        public              int                 Count => queue.Count;
        public  override    string              ToString() => $"Count: {queue.Count}";

        public SharedPool(Func<T> factory) {
            this.factory = factory;
        }

        public override void Dispose() {
            foreach (var obj in queue) {
                obj.Dispose();
            }
            queue.Clear();
        }
        
        internal override T Get() {
            if (!queue.TryDequeue(out T obj)) {
                obj = factory();
            }
            return obj;
        }
        
        internal override void Return(T obj) {
            queue.Enqueue(obj);
        }

        public override void AssertNoLeaks() {
            throw new InvalidOperationException("Dont expect calling SharedPool<>.AssertNoLeaks()");
        }
    }
    
    public class LocalPool<T> : ObjectPool<T> where T : IDisposable
    {
        private readonly    ObjectPool<T>   pool;
        private readonly    string          field;
        private             int             count;
        
        public  override    string          ToString() => pool.ToString();

        public LocalPool(ObjectPool<T> pool, string field) {
            this.pool   = pool;
            this.field  = field;
        }

        public override void Dispose() { }
        
        internal override T Get() {
            count++;
            return pool.Get();
        }
        
        internal override void Return(T obj) {
            count--;
            pool.Return(obj);
        }

        public override void AssertNoLeaks() {
            if (count != 0)
                throw new InvalidOperationException($"ObjectPool<{typeof(T).Name}> leak detected. Count: {count}, field: {field}");
        }
    }
}
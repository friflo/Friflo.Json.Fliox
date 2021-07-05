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
        internal abstract T     GetInstance();
        internal abstract void  Return(T instance);
        public   abstract void  Dispose();
        public   abstract void  AssertNoLeaks();
        
        public Pooled<T>        Get() {
            return new Pooled<T>(this, GetInstance());
        }
    }
    
    public class SharedPool<T> : ObjectPool<T> where T : IDisposable
    {
        private readonly    ConcurrentStack<T>  stack       = new ConcurrentStack<T>();
        private readonly    ConcurrentBag<T>    instances   = new ConcurrentBag<T>();
        private readonly    Func<T>             factory;
        
        public              int                 Count => stack.Count;
        public  override    string              ToString() => $"Count: {stack.Count}";

        public SharedPool(Func<T> factory) {
            this.factory = factory;
        }

        public override void Dispose() {
            foreach (var instance in instances) {
                instance.Dispose();
            }
            stack.Clear();
            instances.Clear();
        }
        
        internal override T GetInstance() {
            if (!stack.TryPop(out T instance)) {
                instance = factory();
                instances.Add(instance);
            }
            return instance;
        }
        
        internal override void Return(T instance) {
            stack.Push(instance);
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
        
        internal override T GetInstance() {
            count++;
            return pool.GetInstance();
        }
        
        internal override void Return(T instance) {
            count--;
            pool.Return(instance);
        }

        public override void AssertNoLeaks() {
            if (count != 0)
                throw new InvalidOperationException($"ObjectPool<{typeof(T).Name}> leak detected. Count: {count}, field: {field}");
        }
    }
}
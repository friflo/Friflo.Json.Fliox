// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;

namespace Friflo.Json.Fliox.Utils
{
    public readonly struct Pooled<T> : IDisposable where T : IDisposable
    {
        /// <summary>
        /// Provide a cached <see cref="instance"/> of Type <see cref="T"/>.
        /// Access to the <see cref="instance"/> is thread safe in the surrounding using scope.
        /// Safe access to <see cref="instance"/> is intended by calling <see cref="ObjectPool{T}.Get"/> in a using scope.
        /// E.g.:
        /// <code>
        ///     using (var pooled = messageContext.pool.ObjectMapper.Get()) {
        ///         ObjectMapper mapper  = pooled.instance;
        ///         ...
        ///     }
        /// </code>
        /// <b>Note</b>: Caching the <see cref="instance"/> reference and accessing it after leaving the using scope
        /// leads to a race condition and must not be done.
        /// </summary>
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
    
    public abstract class ObjectPool<T> : IDisposable where T : IDisposable
    {
        internal    readonly    Action<T>   init;
        
        internal    abstract    T           GetInstance();
        internal    abstract    void        Return(T instance);
        public      abstract    void        Dispose();
        public      abstract    int         Usage { get; }
        
        public                  Pooled<T>   Get() {
            var instance = GetInstance();
            // ReSharper disable once UseNullPropagation
            if (init != null) {
                init(instance);
            }
            return new Pooled<T>(this, instance);
        }
        
        protected ObjectPool(Action<T> init) {
            this.init = init;
        }
    }
    
    public sealed class SharedPool<T> : ObjectPool<T> where T : IDisposable
    {
        private readonly    ConcurrentStack<T>  stack       = new ConcurrentStack<T>();
        private readonly    ConcurrentStack<T>  instances   = new ConcurrentStack<T>();
        private readonly    Func<T>             factory;
        
        public              int                 Count       => stack.Count;
        public  override    int                 Usage       => 0;
        public  override    string              ToString()  => $"Count: {stack.Count}";

        public SharedPool(Func<T> factory, Action<T> init = null) : base(init){
            this.factory    = factory;
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
                instances.Push(instance);
            }
            return instance;
        }
        
        internal override void Return(T instance) {
            if (instance is IResetable resetable) {
                resetable.Reset();
            }
            stack.Push(instance);
        }
    }
    
    public sealed class LocalPool<T> : ObjectPool<T> where T : IDisposable
    {
        private readonly    ObjectPool<T>   pool;
        private             int             count;
        
        public  override    int             Usage       => count;
        public  override    string          ToString()  => pool.ToString();

        public LocalPool(ObjectPool<T> pool) : base (pool.init) {
            this.pool   = pool;
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
    }
}
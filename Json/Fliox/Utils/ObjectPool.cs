// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;

namespace Friflo.Json.Fliox.Utils
{
    public readonly struct Pooled<T> : IDisposable where T : IDisposable
    {
        /// <summary>
        /// Provide a cached <see cref="instance"/> of Type <typeparamref name="T"/>.
        /// Access to the <see cref="instance"/> is thread safe in the surrounding using scope.
        /// Safe access to <see cref="instance"/> is intended by calling <see cref="ObjectPool{T}.Get"/> in a using scope.
        /// E.g.:
        /// <code>
        ///     using (var pooled = syncContext.pool.ObjectMapper.Get()) {
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
    
    public readonly struct ObjectPool<T> : IDisposable where T : IDisposable
    {
        private readonly    Action<T>           init;
        private readonly    ConcurrentStack<T>  stack;
        private readonly    ConcurrentStack<T>  instances;
        private readonly    Func<T>             factory;
        
        public              int                 Count       => stack.Count;
        public  override    string              ToString()  => $"Count: {stack.Count}";
        
        public ObjectPool(Func<T> factory, Action<T> init = null) {
            stack           = new ConcurrentStack<T>();
            instances       = new ConcurrentStack<T>();
            this.factory    = factory;
            this.init       = init;
        }

        public              Pooled<T>           Get() {
            var instance = GetInstance();
            // ReSharper disable once UseNullPropagation
            if (init != null) {
                init(instance);
            }
            return new Pooled<T>(this, instance);
        }

        public void Dispose() {
            foreach (var instance in instances) {
                instance.Dispose();
            }
            stack.Clear();
            instances.Clear();
        }
        
        private T GetInstance() {
            if (stack.TryPop(out T instance)) {
                return instance;
            }
            instance = factory();
            instances.Push(instance);
            return instance;
        }
        
        public void Return(T instance) {
            if (instance is IResetable resetable) {
                resetable.Reset();
            }
            stack.Push(instance);
        }
    }
}
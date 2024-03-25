// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

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
    
    // Could be a readonly struct but don't see a real advantage.
    // Instances of this are intended to be created rarely as they are used as long lived pools.
    public sealed class ObjectPool<T> : IDisposable where T : IDisposable
    {
        private readonly    Action<T>   init;
        // Used Stack<> instead of ConcurrentStack<> as ConcurrentStack<>.Push() allocate a 32 Node on the heap
        // Even if ConcurrentStack<> is slightly faster.
        // Test 100_000_000  Get() / Return() cycles:   Stack<> 3.7 sec      ConcurrentStack<> 2.2 sec
        private readonly    Stack<T>    stack;
        private readonly    Func<T>     factory;
        
        public              int         Count       => stack.Count;
        public  override    string      ToString()  => $"Count: {stack.Count}";
        
        public ObjectPool(Func<T> factory, Action<T> init = null) {
            stack           = new Stack<T>();
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
            T[] instances; 
            lock (stack) {
                instances = stack.ToArray();
                stack.Clear();
            }
            foreach (var instance in instances) {
                instance.Dispose();
            }
        }
        
        private T GetInstance() {
            lock (stack) {
                if (stack.TryPop(out T instance)) {
                    return instance;
                }
            }
            return factory();
        }
        
        public void Return(T instance) {
            if (instance is IResetable resetable) {
                resetable.Reset();
            }
            lock (stack) {
                stack.Push(instance);
            }
        }
    }
}
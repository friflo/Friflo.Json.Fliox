// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Validation;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Utils;

namespace Friflo.Json.Fliox.Hub.Host
{
    public sealed class Pool : IPool
    {
        private   readonly  SharedEnv                       sharedEnv;
        private   readonly  IPool                           sharedPool;
        private   readonly  Dictionary<Type, IDisposable>   poolMap = new Dictionary<Type, IDisposable>(); // object = SharedPool<T>

        public  ObjectPool<JsonPatcher>     JsonPatcher     { get; }
        public  ObjectPool<ScalarSelector>  ScalarSelector  { get; }
        public  ObjectPool<JsonEvaluator>   JsonEvaluator   { get; }
        public  ObjectPool<ObjectMapper>    ObjectMapper    { get; }
        public  ObjectPool<EntityProcessor> EntityProcessor { get; }
        public  ObjectPool<TypeValidator>   TypeValidator   { get; }
        
        public  ObjectPool<T>               Type<T>         (Func<T> factory) where T : IDisposable {
            if (poolMap.TryGetValue(typeof(T), out var pooled)) {
                return (ObjectPool<T>)pooled;
            }
            ObjectPool<T> pool;
            if (sharedPool != null) {
                pool = new LocalPool<T>(sharedPool.Type(factory));
            } else {
                pool = new SharedPool<T>(factory);
            }
            poolMap[typeof(T)] = pool;
            return pool;
        }

        internal Pool(SharedEnv sharedEnv) {
            this.sharedEnv  = sharedEnv;
            JsonPatcher     = new SharedPool<JsonPatcher>       (() => new JsonPatcher());
            ScalarSelector  = new SharedPool<ScalarSelector>    (() => new ScalarSelector());
            JsonEvaluator   = new SharedPool<JsonEvaluator>     (() => new JsonEvaluator());
            ObjectMapper    = new SharedPool<ObjectMapper>      (() => new ObjectMapper(sharedEnv.TypeStore),  m => m.ErrorHandler = ObjectReader.NoThrow);
            EntityProcessor = new SharedPool<EntityProcessor>   (() => new EntityProcessor());
            TypeValidator   = new SharedPool<TypeValidator>     (() => new TypeValidator());
        }
        
        internal Pool(Pool sharedPool) {
            sharedEnv       = sharedPool.sharedEnv;
            this.sharedPool = sharedPool;
            JsonPatcher     = new LocalPool<JsonPatcher>        (sharedPool.JsonPatcher);
            ScalarSelector  = new LocalPool<ScalarSelector>     (sharedPool.ScalarSelector);
            JsonEvaluator   = new LocalPool<JsonEvaluator>      (sharedPool.JsonEvaluator);
            ObjectMapper    = new LocalPool<ObjectMapper>       (sharedPool.ObjectMapper);
            EntityProcessor = new LocalPool<EntityProcessor>    (sharedPool.EntityProcessor);
            TypeValidator   = new LocalPool<TypeValidator>      (sharedPool.TypeValidator);
        }
        
        public void Dispose() {
            JsonPatcher.    Dispose();
            ScalarSelector. Dispose();
            JsonEvaluator.  Dispose();
            ObjectMapper.   Dispose();
            EntityProcessor.Dispose();
            TypeValidator.  Dispose();
            foreach (var pair in poolMap) {
                var pool = pair.Value;
                pool.Dispose();
            }
            poolMap.Clear();
        }

        public PoolUsage PoolUsage { get {
            var usage = new PoolUsage {
                patcherCount            = JsonPatcher       .Usage,
                selectorCount           = ScalarSelector    .Usage,
                evaluatorCount          = JsonEvaluator     .Usage,
                objectMapperCount       = ObjectMapper      .Usage,
                entityProcessorCount    = EntityProcessor   .Usage,
                typeValidatorCount      = TypeValidator     .Usage
            };
            return usage;
        } }
    }
    
    public abstract class SharedEnv : IDisposable
    {
        public abstract TypeStore   TypeStore   { get; }
        public abstract Pool        Pool        { get; }
        
        public abstract void        Dispose();
    }
    
    public sealed class Shared : SharedEnv
    {
        private readonly    TypeStore   typeStore;
        private readonly    Pool        pool;
        
        public  override    TypeStore   TypeStore   => typeStore;
        public  override    Pool        Pool        => pool;
        
        public Shared() {
            typeStore   = new TypeStore();
            pool        = new Pool(this);
        }

        public override void Dispose () {
            pool.Dispose();
            typeStore.Dispose();
        }
    }
    
    public sealed class SharedHostEnv : SharedEnv
    {
        private readonly    Pool        pool;
        
        public  override    TypeStore   TypeStore   => HostTypeStore.Get();
        public  override    Pool        Pool        => pool;
        
        public static readonly SharedHostEnv Instance = new SharedHostEnv();
        
        private SharedHostEnv() {
            pool = new Pool(this);
        }
        
        public override void Dispose () {
            pool.Dispose();
        }
    }
}
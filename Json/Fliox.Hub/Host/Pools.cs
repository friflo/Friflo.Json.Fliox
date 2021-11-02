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
    public class Pools : IPools
    {
        private readonly    IPools                          sharedPools;
        private readonly    Func<TypeStore>                 factory;
        private readonly    Dictionary<Type, IDisposable>   poolMap = new Dictionary<Type, IDisposable>(); // object = SharedPool<T>
        
        public  TypeStore                   TypeStore       => factory();
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
            if (sharedPools != null) {
                pool = new LocalPool<T>(sharedPools.Type(factory));
            } else {
                pool = new SharedPool<T>(factory);
            }
            poolMap[typeof(T)] = pool;
            return pool;
        }
        
        // ReSharper disable once UnusedParameter.Local - keep for code navigation
        public Pools(Func<TypeStore> factory) {
            this.factory    = factory;
            JsonPatcher     = new SharedPool<JsonPatcher>       (() => new JsonPatcher());
            ScalarSelector  = new SharedPool<ScalarSelector>    (() => new ScalarSelector());
            JsonEvaluator   = new SharedPool<JsonEvaluator>     (() => new JsonEvaluator());
            ObjectMapper    = new SharedPool<ObjectMapper>      (() => new ObjectMapper(factory()),  m => m.ErrorHandler = ObjectReader.NoThrow);
            EntityProcessor = new SharedPool<EntityProcessor>   (() => new EntityProcessor());
            TypeValidator   = new SharedPool<TypeValidator>     (() => new TypeValidator());
        }
        
        internal Pools(Pools sharedPools) {
            factory         = sharedPools.factory;
            this.sharedPools = sharedPools;
            JsonPatcher     = new LocalPool<JsonPatcher>        (sharedPools.JsonPatcher);
            ScalarSelector  = new LocalPool<ScalarSelector>     (sharedPools.ScalarSelector);
            JsonEvaluator   = new LocalPool<JsonEvaluator>      (sharedPools.JsonEvaluator);
            ObjectMapper    = new LocalPool<ObjectMapper>       (sharedPools.ObjectMapper);
            EntityProcessor = new LocalPool<EntityProcessor>    (sharedPools.EntityProcessor);
            TypeValidator   = new LocalPool<TypeValidator>      (sharedPools.TypeValidator);
        }
        
        public virtual void Dispose() {
            JsonPatcher.    Dispose();
            ScalarSelector. Dispose();
            JsonEvaluator.  Dispose();
            ObjectMapper.   Dispose();
            EntityProcessor.Dispose();
            TypeValidator.  Dispose();
            foreach (var pool in poolMap) {
                var sharedPool = pool.Value;
                sharedPool.Dispose();
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
        
        public static Pools Create() {
            var typeStore = new TypeStore();
            return new TypeStorePool(typeStore);
        }
    }
    
    internal class TypeStorePool : Pools
    {
        private readonly TypeStore typeStore;
        
        internal TypeStorePool(TypeStore typeStore) : base(() => typeStore) {
            this.typeStore = typeStore;
        }
        
        public override void Dispose () {
            base.Dispose();
            typeStore.Dispose();
        }
    }
    
    public static class HostGlobal
    {
        public   static readonly    Pools   Pool = new Pools(HostTypeStore.Get);
    }
}
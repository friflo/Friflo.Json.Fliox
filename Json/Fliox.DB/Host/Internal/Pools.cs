// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Validation;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Utils;

namespace Friflo.Json.Fliox.DB.Host.Internal
{
    public struct PoolUsage {
        internal int    patcherCount;
        internal int    selectorCount;
        internal int    evaluatorCount;
        internal int    objectMapperCount;
        internal int    entityProcessorCount;
        internal int    typeValidatorCount;
        
        public void AssertEqual(in PoolUsage other) {
            if (patcherCount            != other.patcherCount)          throw new InvalidOperationException("detect JsonPatcher leak");
            if (selectorCount           != other.selectorCount)         throw new InvalidOperationException("detect ScalarSelector leak");
            if (evaluatorCount          != other.evaluatorCount)        throw new InvalidOperationException("detect JsonEvaluator leak");
            if (objectMapperCount       != other.objectMapperCount)     throw new InvalidOperationException("detect ObjectMapper leak");
            if (entityProcessorCount    != other.entityProcessorCount)  throw new InvalidOperationException("detect EntityProcessor leak");
            if (typeValidatorCount      != other.typeValidatorCount)    throw new InvalidOperationException("detect TypeValidator leak");
        }
    }

    internal sealed class Pools : IPools
    {
        private readonly  Dictionary<Type, IDisposable>    poolMap = new Dictionary<Type, IDisposable>(); // object = SharedPool<T>
        
        public  ObjectPool<JsonPatcher>     JsonPatcher     { get; }
        public  ObjectPool<ScalarSelector>  ScalarSelector  { get; }
        public  ObjectPool<JsonEvaluator>   JsonEvaluator   { get; }
        public  ObjectPool<ObjectMapper>    ObjectMapper    { get; }
        public  ObjectPool<EntityProcessor> EntityProcessor { get; }
        public  ObjectPool<TypeValidator>   TypeValidator   { get; }
        
        public  ObjectPool<T>               Pool<T>         (Func<T> factory) where T : IDisposable {
            if (poolMap.TryGetValue(typeof(T), out var pooled)) {
                return (ObjectPool<T>)pooled;
            }
            var sharedPooled = new SharedPool<T>(factory);
            poolMap[typeof(T)] = sharedPooled;
            return sharedPooled;
        }
        
        // ReSharper disable once UnusedParameter.Local - keep for code navigation
        internal Pools(Default _) {
            JsonPatcher     = new SharedPool<JsonPatcher>       (() => new JsonPatcher());
            ScalarSelector  = new SharedPool<ScalarSelector>    (() => new ScalarSelector());
            JsonEvaluator   = new SharedPool<JsonEvaluator>     (() => new JsonEvaluator());
            ObjectMapper    = new SharedPool<ObjectMapper>      (HostTypeStore.CreateObjectMapper);
            EntityProcessor = new SharedPool<EntityProcessor>   (() => new EntityProcessor());
            TypeValidator   = new SharedPool<TypeValidator>     (() => new TypeValidator());
        }
        
        internal Pools(IPools sharedPools) {
            JsonPatcher     = new LocalPool<JsonPatcher>        (sharedPools.JsonPatcher,       "JsonPatcher");
            ScalarSelector  = new LocalPool<ScalarSelector>     (sharedPools.ScalarSelector,    "ScalarSelector");
            JsonEvaluator   = new LocalPool<JsonEvaluator>      (sharedPools.JsonEvaluator,     "JsonEvaluator");
            ObjectMapper    = new LocalPool<ObjectMapper>       (sharedPools.ObjectMapper,      "ObjectMapper");
            EntityProcessor = new LocalPool<EntityProcessor>    (sharedPools.EntityProcessor,   "EntityProcessor");
            TypeValidator   = new LocalPool<TypeValidator>      (sharedPools.TypeValidator,     "TypeValidator");
        }

        public void Dispose() {
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
}
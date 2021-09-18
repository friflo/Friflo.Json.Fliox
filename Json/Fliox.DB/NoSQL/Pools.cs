// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.DB.Sync;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Validation;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Utils;

namespace Friflo.Json.Fliox.DB.NoSQL
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
    
    /// <summary>
    /// <see cref="IPools"/> is a set of pooled instances of various <see cref="Type"/>'s.
    /// To enable pooling instances of a specific class it needs to implement <see cref="IDisposable"/>.
    /// Pools for classes used commonly within <see cref="NoSQL"/> are directly available. E.g. <see cref="ObjectMapper"/>.
    /// Custom classes can also be managed by <see cref="IPools"/> by using <see cref="Pool{T}"/>.
    /// Its typical use case is pooling of a domain specific Store extending <see cref="Graph.EntityStore"/>. 
    /// </summary>
    public interface IPools
    {
        ObjectPool<JsonPatcher>     JsonPatcher     { get; }
        ObjectPool<ScalarSelector>  ScalarSelector  { get; }
        ObjectPool<JsonEvaluator>   JsonEvaluator   { get; }
        /// <summary> Returned <see cref="Mapper.ObjectMapper"/> doesnt throw Read() exceptions. To handle errors its
        /// <see cref="Mapper.ObjectMapper.reader"/> -> <see cref="ObjectReader.Error"/> need to be checked. </summary>
        ObjectPool<ObjectMapper>    ObjectMapper    { get; }
        ObjectPool<EntityProcessor> EntityProcessor { get; }
        ObjectPool<TypeValidator>   TypeValidator   { get; }
        /// <summary>
        /// Enable pooling instances of the given Type <see cref="T"/>. In case no cached instance of <see cref="T"/>
        /// is available the <see cref="factory"/> method is called to create a new instance.
        /// After returning a pooled instance to its pool with <see cref="ObjectPool{T}.Return"/> it is cached and
        /// will be reused when calling <see cref="ObjectPool{T}.Get"/> anytime later.
        /// To ensure pooled instances are not leaking use the using directive. E.g.
        /// <code>
        /// using (var pooledMapper = messageContext.pools.ObjectMapper.Get()) {
        ///     ...
        /// }
        /// </code>
        /// </summary>
        ObjectPool<T>               Pool<T>         (Func<T> factory) where T : IDisposable;
        
        PoolUsage                   PoolUsage       { get; }
    }
    
    public class Pools : IPools, IDisposable
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
        
        public   static readonly    Pools   SharedPools = new Pools(Default.Constructor);
        
        // constructor present for code navigation
        private Pools(Default _) {
            JsonPatcher     = new SharedPool<JsonPatcher>       (() => new JsonPatcher());
            ScalarSelector  = new SharedPool<ScalarSelector>    (() => new ScalarSelector());
            JsonEvaluator   = new SharedPool<JsonEvaluator>     (() => new JsonEvaluator());
            ObjectMapper    = new SharedPool<ObjectMapper>      (SyncTypeStore.CreateObjectMapper);
            EntityProcessor = new SharedPool<EntityProcessor>   (() => new EntityProcessor());
            TypeValidator   = new SharedPool<TypeValidator>     (() => new TypeValidator());
        }
        
        internal Pools(Pools sharedPools) {
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
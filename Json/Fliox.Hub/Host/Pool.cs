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
    /// <summary>
    /// <see cref="IPool"/> is a set of pooled instances of various <see cref="Type"/>'s.
    /// To enable pooling instances of a specific class it needs to implement <see cref="IDisposable"/>.
    /// Pool for classes used commonly within <see cref="Host"/> are directly available. E.g. <see cref="ObjectMapper"/>.
    /// Custom types can also be managed by <see cref="IPool"/> by using <see cref="Type{T}"/>.
    /// Its typical use case is pooling a domain specific <see cref="Client.FlioxClient"/> implementation. 
    /// </summary>
    public interface IPool : IDisposable
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
        /// Enable pooling instances of the given Type <typeparamref name="T"/>. In case no cached instance of <typeparamref name="T"/>
        /// is available the <paramref name="factory"/> method is called to create a new instance.
        /// After returning a pooled instance to its pool with <see cref="ObjectPool{T}.Return"/> it is cached and
        /// will be reused when calling <see cref="ObjectPool{T}.Get"/> anytime later.
        /// To ensure pooled instances are not leaking use the using directive. E.g.
        /// <code>
        /// using (var pooledMapper = messageContext.pool.ObjectMapper.Get()) {
        ///     ...
        /// }
        /// </code>
        /// </summary>
        ObjectPool<T>               Type<T>         (Func<T> factory) where T : IDisposable;
        
        PoolUsage                   PoolUsage       { get; }
    }
    
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
    
    
    public sealed class Pool : IPool
    {
        // Note: Pool does not expose sharedEnv.TypeStore by intention to avoid side effects by unexpected usage. 
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
}
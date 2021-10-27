// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Validation;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Utils;

namespace Friflo.Json.Fliox.Hub.Host
{
    /// <summary>
    /// <see cref="IPools"/> is a set of pooled instances of various <see cref="Type"/>'s.
    /// To enable pooling instances of a specific class it needs to implement <see cref="IDisposable"/>.
    /// Pools for classes used commonly within <see cref="Host"/> are directly available. E.g. <see cref="ObjectMapper"/>.
    /// Custom classes can also be managed by <see cref="IPools"/> by using <see cref="Pool{T}"/>.
    /// Its typical use case is pooling a domain specific <see cref="Client.FlioxClient"/> implementation. 
    /// </summary>
    public interface IPools : IDisposable
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
}
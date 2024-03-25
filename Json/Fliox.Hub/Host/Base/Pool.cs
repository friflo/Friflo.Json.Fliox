// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Pools;
using Friflo.Json.Fliox.Schema.Validation;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Tree;
using Friflo.Json.Fliox.Utils;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host
{
    /// <summary>
    /// <see cref="Pool"/> is a set of pooled instances of various <see cref="Type"/>'s.
    /// To enable pooling instances of a specific class it needs to implement <see cref="IDisposable"/>.
    /// Pool for classes used commonly within <see cref="Host"/> are directly available. E.g. <see cref="ObjectMapper"/>.
    /// Custom types can also be managed by <see cref="Pool"/> by using <see cref="Type{T}"/>.
    /// Its typical use case is pooling a domain specific <see cref="Client.FlioxClient"/> implementation. 
    /// </summary>
    internal sealed class Pool
    {
        // Note: Pool does not expose sharedEnv.TypeStore by intention to avoid side effects by unexpected usage. 
        private   readonly  ConcurrentDictionary<Type, IDisposable>   poolMap = new ConcurrentDictionary<Type, IDisposable>(); // object = SharedPool<T>

        internal    ObjectPool<JsonPatcher>         JsonPatcher         { get; }
        internal    ObjectPool<JsonMerger>          JsonMerger          { get; }
        internal    ObjectPool<ScalarSelector>      ScalarSelector      { get; }
        internal    ObjectPool<JsonEvaluator>       JsonEvaluator       { get; }
        /// <summary> Returned <see cref="Mapper.ObjectMapper"/> doesn't throw Read() exceptions. To handle errors its
        /// <see cref="Mapper.ObjectMapper.reader"/> -> <see cref="ObjectReader.Error"/> need to be checked. </summary>
        internal    ObjectPool<ObjectMapper>        ObjectMapper        { get; }
        internal    ObjectPool<ReaderPool>          ReaderPool          { get; }
        internal    ObjectPool<EntityProcessor>     EntityProcessor     { get; }
        internal    ObjectPool<TypeValidator>       TypeValidator       { get; }
        internal    ObjectPool<MemoryBuffer>        MemoryBuffer        { get; }
        internal    ObjectPool<Json2SQL>            Json2SQL            { get; }
        internal    ObjectPool<SQL2Json>            SQL2Json            { get; }
        /// <summary>
        /// Enable pooling instances of the given Type <typeparamref name="T"/>. In case no cached instance of <typeparamref name="T"/>
        /// is available the <paramref name="factory"/> method is called to create a new instance.
        /// After returning a pooled instance to its pool with <see cref="ObjectPool{T}.Return"/> it is cached and
        /// will be reused when calling <see cref="ObjectPool{T}.Get"/> anytime later.
        /// To ensure pooled instances are not leaking use the using directive. E.g.
        /// <code>
        /// using (var pooledMapper = syncContext.pool.ObjectMapper.Get()) {
        ///     ...
        /// }
        /// </code>
        /// </summary>
        internal    ObjectPool<T>               Type<T>         (Func<T> factory) where T : IDisposable {
            if (poolMap.TryGetValue(typeof(T), out var pooled)) {
                return (ObjectPool<T>)pooled;
            }
            var pool = new ObjectPool<T>(factory);
            poolMap[typeof(T)] = pool;
            return pool;
        }

        internal Pool(TypeStore typeStore) {
            JsonPatcher         = new ObjectPool<JsonPatcher>       (() => new JsonPatcher());
            JsonMerger          = new ObjectPool<JsonMerger>        (() => new JsonMerger());
            ScalarSelector      = new ObjectPool<ScalarSelector>    (() => new ScalarSelector());
            JsonEvaluator       = new ObjectPool<JsonEvaluator>     (() => new JsonEvaluator());
            ObjectMapper        = new ObjectPool<ObjectMapper>      (() => new ObjectMapper(typeStore),  m => m.ErrorHandler = ObjectReader.NoThrow);
            ReaderPool          = new ObjectPool<ReaderPool>        (() => new ReaderPool(typeStore));
            EntityProcessor     = new ObjectPool<EntityProcessor>   (() => new EntityProcessor());
            Json2SQL            = new ObjectPool<Json2SQL>          (() => new Json2SQL());
            SQL2Json            = new ObjectPool<SQL2Json>          (() => new SQL2Json());
            TypeValidator       = new ObjectPool<TypeValidator>     (() => new TypeValidator());
            MemoryBuffer        = new ObjectPool<MemoryBuffer>      (() => new MemoryBuffer(4 * 1024));
        }
        
        internal void Dispose() {
            JsonPatcher.        Dispose();
            JsonMerger.         Dispose();
            ScalarSelector.     Dispose();
            JsonEvaluator.      Dispose();
            ObjectMapper.       Dispose();
            ReaderPool.         Dispose();
            EntityProcessor.    Dispose();
            Json2SQL.           Dispose();
            SQL2Json.           Dispose();
            TypeValidator.      Dispose();
            MemoryBuffer.       Dispose();
            foreach (var pair in poolMap) {
                var pool = pair.Value;
                pool.Dispose();
            }
            poolMap.Clear();
        }

        internal PoolUsage PoolUsage => new PoolUsage {
            jsonPatcher     = JsonPatcher       .Count,
            jsonMerger      = JsonMerger        .Count,
            scalarSelector  = ScalarSelector    .Count,
            jsonEvaluator   = JsonEvaluator     .Count,
            objectMapper    = ObjectMapper      .Count,
            entityProcessor = EntityProcessor   .Count,
            typeValidator   = TypeValidator     .Count,
            memoryBuffer    = MemoryBuffer      .Count
        };
    }
    
    internal struct PoolUsage {
        internal  int     jsonPatcher;
        internal  int     jsonMerger;
        internal  int     scalarSelector;
        internal  int     jsonEvaluator;
        internal  int     objectMapper;
        internal  int     entityProcessor;
        internal  int     typeValidator;
        internal  int     memoryBuffer;

        public override string ToString() =>
            $"jsonPatcher: {jsonPatcher}, jsonMerger: {jsonMerger}, scalarSelector: {scalarSelector}, jsonEvaluator: {jsonEvaluator}, " +
            $"objectMapper: {objectMapper}, entityProcessor: {entityProcessor}, typeValidator: {typeValidator}, memoryBuffer: {memoryBuffer}";
    }
}
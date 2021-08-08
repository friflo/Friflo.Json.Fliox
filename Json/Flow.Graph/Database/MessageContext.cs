// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Flow.Auth;
using Friflo.Json.Flow.Database.Event;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Schema.Validation;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.Transform;
using Friflo.Json.Flow.Utils;

#if UNITY_5_3_OR_NEWER
    using BoolTask = System.Threading.Tasks.Task<bool>;
#else
    using BoolTask = System.Threading.Tasks.ValueTask<bool>;
#endif

namespace Friflo.Json.Flow.Database
{
    // ------------------------------------ MessageContext ------------------------------------
    /// <summary>
    /// One <see cref="MessageContext"/> is created per <see cref="SyncRequest"/> instance to enable
    /// multi threaded and concurrent request handling.
    /// <br></br>
    /// Note: In case of adding transaction support in future transaction data/state will be stored here.
    /// </summary>
    public class MessageContext
    {
        /// <summary>Is set for clients requests only. In other words - from the initiator of a <see cref="DatabaseRequest"/></summary>
        public                  string          clientId;
        public  readonly        IPools          pools;
        public  readonly        IEventTarget    eventTarget;
        public                  AuthState       authState;
        
        private                 PoolUsage       startUsage;
        public                  Action          canceler = () => {};
        public override         string          ToString() => $"clientId: {clientId}, auth: {authState}";

        public MessageContext (IEventTarget eventTarget) {
            pools               = new Pools(Pools.SharedPools);
            startUsage          = pools.PoolUsage;
            this.eventTarget    = eventTarget;
        }
        
        public MessageContext (IEventTarget eventTarget, string clientId) {
            pools               = new Pools(Pools.SharedPools);
            startUsage          = pools.PoolUsage;
            this.eventTarget    = eventTarget;
            this.clientId       = clientId;
        }
        
        public void Cancel() {
            canceler.Invoke();
        }
        
        public bool Authenticated() {
            return authState.Authenticated;
        }
        
        public bool Authorize(DatabaseTask task, MessageContext messageContext) {
            return authState.Authorizer.Authorize(task, messageContext);
        }

        
        public void Release() {
            startUsage.AssertEqual(pools.PoolUsage);
        }
    }
    
    public struct PoolUsage {
        internal int    patcherCount;
        internal int    selectorCount;
        internal int    evaluatorCount;
        internal int    objectMapperCount;
        internal int    entityValidatorCount;
        internal int    typeValidatorCount;
        
        public void AssertEqual(in PoolUsage other) {
            if (patcherCount            != other.patcherCount)          throw new InvalidOperationException("detect patcher leak");
            if (selectorCount           != other.selectorCount)         throw new InvalidOperationException("detect selector leak");
            if (evaluatorCount          != other.evaluatorCount)        throw new InvalidOperationException("detect evaluator leak");
            if (objectMapperCount       != other.objectMapperCount)     throw new InvalidOperationException("detect objectMapper leak");
            if (entityValidatorCount    != other.entityValidatorCount)  throw new InvalidOperationException("detect entityValidator leak");
            if (typeValidatorCount      != other.typeValidatorCount)    throw new InvalidOperationException("detect typeValidator leak");
        }
    }
    
    public interface IPools
    {
        ObjectPool<JsonPatcher>     JsonPatcher     { get; }
        ObjectPool<ScalarSelector>  ScalarSelector  { get; }
        ObjectPool<JsonEvaluator>   JsonEvaluator   { get; }
        /// <summary> Returned <see cref="Mapper.ObjectMapper"/> doesnt throw Read() exceptions. To handle errors its
        /// <see cref="Mapper.ObjectMapper.reader"/> -> <see cref="ObjectReader.Error"/> need to be checked. </summary>
        ObjectPool<ObjectMapper>    ObjectMapper    { get; }
        ObjectPool<EntityValidator> EntityValidator { get; }
        ObjectPool<TypeValidator>   TypeValidator   { get; }
        
        PoolUsage                   PoolUsage       { get; }
    }
    
    public class Pools : IPools, IDisposable
    {
        public  ObjectPool<JsonPatcher>     JsonPatcher     { get; }
        public  ObjectPool<ScalarSelector>  ScalarSelector  { get; }
        public  ObjectPool<JsonEvaluator>   JsonEvaluator   { get; }
        public  ObjectPool<ObjectMapper>    ObjectMapper    { get; }
        public  ObjectPool<EntityValidator> EntityValidator { get; }
        public  ObjectPool<TypeValidator>   TypeValidator   { get; }
        
        public   static readonly    Pools   SharedPools = new Pools(Default.Constructor);
        
        // constructor present for code navigation
        private Pools(Default _) {
            JsonPatcher     = new SharedPool<JsonPatcher>       (() => new JsonPatcher());
            ScalarSelector  = new SharedPool<ScalarSelector>    (() => new ScalarSelector());
            JsonEvaluator   = new SharedPool<JsonEvaluator>     (() => new JsonEvaluator());
            ObjectMapper    = new SharedPool<ObjectMapper>      (SyncTypeStore.CreateObjectMapper);
            EntityValidator = new SharedPool<EntityValidator>   (() => new EntityValidator());
            TypeValidator   = new SharedPool<TypeValidator>     (() => new TypeValidator());
        }
        
        internal Pools(Pools sharedPools) {
            JsonPatcher     = new LocalPool<JsonPatcher>        (sharedPools.JsonPatcher,       "JsonPatcher");
            ScalarSelector  = new LocalPool<ScalarSelector>     (sharedPools.ScalarSelector,    "ScalarSelector");
            JsonEvaluator   = new LocalPool<JsonEvaluator>      (sharedPools.JsonEvaluator,     "JsonEvaluator");
            ObjectMapper    = new LocalPool<ObjectMapper>       (sharedPools.ObjectMapper,      "ObjectMapper");
            EntityValidator = new LocalPool<EntityValidator>    (sharedPools.EntityValidator,   "EntityValidator");
            TypeValidator   = new LocalPool<TypeValidator>      (sharedPools.TypeValidator,     "TypeValidator");
        }

        public void Dispose() {
            JsonPatcher.    Dispose();
            ScalarSelector. Dispose();
            JsonEvaluator.  Dispose();
            ObjectMapper.   Dispose();
            EntityValidator.Dispose();
            TypeValidator.  Dispose();
        }

        public PoolUsage PoolUsage { get {
            var usage = new PoolUsage {
                patcherCount            = JsonPatcher       .Usage,
                selectorCount           = ScalarSelector    .Usage,
                evaluatorCount          = JsonEvaluator     .Usage,
                objectMapperCount       = ObjectMapper      .Usage,
                entityValidatorCount    = EntityValidator   .Usage,
                typeValidatorCount      = TypeValidator     .Usage
            };
            return usage;
        } }
    }
}
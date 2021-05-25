// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Database.Remote;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Database
{
    // ------------------------------------ SyncContext ------------------------------------
    /// <summary>
    /// One <see cref="SyncContext"/> is created per <see cref="EntityContainer"/> to enable multi threaded
    /// request handling for different <see cref="EntityContainer"/> instances.
    ///
    /// The <see cref="SyncContext"/> for a specific <see cref="EntityContainer"/> must not be used
    /// multi threaded.
    ///
    /// E.g. Reading key/values of a database can be executed multi threaded, but serializing for them
    /// for a <see cref="SyncResponse"/> in <see cref="DatabaseTask.Execute"/> need to be single threaded. 
    /// </summary>
    public class SyncContext
    {
        public  readonly        Pools  pools;
        
        public SyncContext (Pools pools) {
            this.pools = pools;
        }
    }
    
    public class ContextPools : IDisposable {
        internal readonly           Pools  pools;
        private  readonly           Pools  ownedPools;
        
        public   static             bool   useSharedPools = true;
        private  static readonly    Pools  SharedPools = new Pools();
        
        public ContextPools () {
            if (useSharedPools) {
                pools = SharedPools;
            } else {
                pools = ownedPools = new Pools();
            }
        }

        public void Dispose() {
            if (ownedPools != null) {
                ownedPools.AssertSingleUsage();
                ownedPools.Dispose();
            }
        }
    }

    public class Pools : IDisposable
    {
        public readonly  ObjectPool<JsonPatcher>    jsonPatcher     = new ObjectPool<JsonPatcher>   (() => new JsonPatcher());
        public readonly  ObjectPool<ScalarSelector> scalarSelector  = new ObjectPool<ScalarSelector>(() => new ScalarSelector());
        public readonly  ObjectPool<JsonEvaluator>  jsonEvaluator   = new ObjectPool<JsonEvaluator> (() => new JsonEvaluator());
        public readonly  ObjectPool<ObjectMapper>   objectMapper    = new ObjectPool<ObjectMapper>  (() => new ObjectMapper(SyncTypeStore.Get()));

        public void Dispose() {
            jsonPatcher.Dispose();
            scalarSelector.Dispose();
            jsonEvaluator.Dispose();
            objectMapper.Dispose();
        }
        
        internal void AssertSingleUsage() {
            jsonPatcher.    AssertSingleUsage("jsonPatcher");
            scalarSelector. AssertSingleUsage("scalarSelector");
            jsonEvaluator.  AssertSingleUsage("jsonEvaluator");
            objectMapper.   AssertSingleUsage("objectMapper");
        }
    }
}
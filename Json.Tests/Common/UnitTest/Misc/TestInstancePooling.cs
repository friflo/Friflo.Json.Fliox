using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Misc
{
    public class TestInstancePooling
    {
        [Test]
        public void TestPoolReferenceParallel()
        {
            Parallel.For(0, 4, i => TestPoolReference());
        }
        
        [Test]
        public void TestPoolReference()
        {
            var objects = new List<object>();
            for (int n = 0; n < 2_000_000; n++) {
                var syncRequest = new SyncRequest();
                var tasks       = new List<SyncRequestTask>();
                var upsert1     = new UpsertEntities();
                var upsert2     = new UpsertEntities();
                var entities    = new List<JsonEntity>(1);
                
                objects.Add(syncRequest);
                objects.Add(tasks);
                objects.Add(upsert1);
                objects.Add(upsert2);
                objects.Add(entities);
                objects.Clear();
            }
        }
        
        [Test]
        public void TestPoolParallel()
        {
            Parallel.For(0, 4, i => TestPool());
        }
        
        [Test]
        public void TestPool()
        {
            var pool                    = new Pool();
            var syncRequestMapper       = new Mapper<SyncRequest>           (0, () => new SyncRequest());
            var syncRequestTasksMapper  = new Mapper<List<SyncRequestTask>> (1, () => new List<SyncRequestTask>());
            var upsertMapper            = new Mapper<UpsertEntities>        (2, () => new UpsertEntities());
            var entitiesMapper          = new Mapper<List<JsonEntity>>      (3, () => new List<JsonEntity>());
            
            var start = GC.GetAllocatedBytesForCurrentThread();
            var objects = new List<object>();
            for (int n = 0; n < 2_000_000; n++) {
                var syncRequest = pool.Create(syncRequestMapper);
                var tasks       = pool.Create(syncRequestTasksMapper);
                var upsert1     = pool.Create(upsertMapper);
                var upsert2     = pool.Create(upsertMapper);
                var entities    = pool.Create(entitiesMapper);
                
                objects.Add(syncRequest);
                objects.Add(tasks);
                objects.Add(upsert1);
                objects.Add(upsert2);
                objects.Add(entities);
                objects.Clear();
                pool.Reuse();
            }
            var dif = GC.GetAllocatedBytesForCurrentThread() - start;
            Console.WriteLine(dif);
        }
        
        class Mapper
        {
            internal readonly int index;
            
            internal Mapper(int index) {
                this.index = index;
            }
        }
        
        class Mapper<T> : Mapper
        {
            internal readonly Func<T> factory;
            
            internal Mapper(int index, Func<T> factory) : base(index) {
                this.factory = factory;
            }
        }
        
        class Pool
        {
            private InstancePool[]  instancePools       = Array.Empty<InstancePool>();
            private int             instancePoolsCount;
            private int             version;
            
            internal T Create<T>(Mapper<T> mapper)
            {
                var index = mapper.index;
                if (index < instancePoolsCount) {
                    ref var instancePool = ref instancePools[index];
                    if (instancePool.version != version) {
                        instancePool.version = version;
                        if (instancePool.count > 0) {
                            instancePool.used = 1;
                            return (T)instancePool.instances[0];
                        }
                    } else {
                        int used = instancePool.used;
                        if (used < instancePool.count) {
                            instancePool.used++;
                            return (T)instancePool.instances[used];
                        }
                    }
                    return (T)instancePool.Create(mapper);
                }
                return CreateInstancePool(mapper);
            }
            
            private T CreateInstancePool<T>(Mapper<T> mapper) {
                var count           = instancePoolsCount;
                var index           = mapper.index;
                instancePoolsCount  = index + 1;
                var newPool         = new InstancePool( new List<object>() ) { version = version };
                var instance        = (T)newPool.Create(mapper);
                var newPools        = new InstancePool[instancePoolsCount];
                for (int n = 0; n < count; n++) {
                    newPools[n] = instancePools[n];
                }
                instancePools           = newPools;
                instancePools[index]    = newPool;
                return instance;
            }
            
            internal void Reuse() {
                version++;
            }
        }
        
        struct InstancePool
        {
            internal readonly   List<object>    instances;
            internal            int             used;
            internal            int             count;
            internal            int             version;
            
            internal InstancePool(List<object> instances) {
                this.instances  = instances;
                used            =  0;
                count           =  0;
                version         = -1;
            }
            
            internal object Create<T>(Mapper<T> mapper) {
                used++;
                var instance = mapper.factory();
                count++;
                instances.Add(instance);
                return instance;               
            }
        }
    }
}

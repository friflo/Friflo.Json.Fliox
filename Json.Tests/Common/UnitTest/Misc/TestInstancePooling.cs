using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Misc
{
    public static class TestInstancePooling
    {
        private const int Count = 2000; // 2_000_000;
        
        [Test]
        public static void TestPoolReferenceParallel()
        {
            Parallel.For(0, 4, i => TestPoolReference());
        }
        
        [Test]
        public static void TestPoolReference()
        {
            var objects = new List<object>();
            for (int n = 0; n < Count; n++) {
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
        public static void TestPoolParallel()
        {
            var typeStore = new TypeStore();
            Parallel.For(0, 4, i => TestPoolInternal(typeStore));
        }
        
        [Test]
        public static void TestPool() {
            var typeStore = new TypeStore();
            TestPoolInternal(typeStore);
        }
        
        private static void TestPoolInternal(TypeStore typeStore)
        {
            var pool                    = new InstancePool(typeStore);
            var syncRequestMapper       = typeStore.GetTypeMapper(typeof(SyncRequest));
            var syncRequestTasksMapper  = typeStore.GetTypeMapper(typeof(List<SyncRequestTask>));
            var upsertMapper            = typeStore.GetTypeMapper(typeof(UpsertEntities));
            var entitiesMapper          = typeStore.GetTypeMapper(typeof(List<JsonEntity>));
            
            var start = GC.GetAllocatedBytesForCurrentThread();
            var objects = new List<object>();
            for (int n = 0; n < Count; n++) {
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
    }
}

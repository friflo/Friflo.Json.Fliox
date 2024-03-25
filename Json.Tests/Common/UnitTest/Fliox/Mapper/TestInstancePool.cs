// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Pools;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
{
    public static class TestInstancePool
    {
        private const int Count         = 2000; // 2_000_000;
        private const int ParallelCount = 8;
        
        [Test]
        public static void TestPoolReferenceParallel()
        {
            Parallel.For(0, ParallelCount, i => TestPoolReference());
        }
        
        [Test]
        public static void TestPoolReference()
        {
            var objects = new List<object>();
            for (int n = 0; n < Count; n++) {
                objects.Clear();
                
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
            }
            AreEqual(5, objects.Count);
        }
        
        [Test]
        public static void TestPoolParallel()
        {
            var typeStore = new TypeStore();
            Parallel.For(0, ParallelCount, i => TestPoolInternal(typeStore));
        }
        
        [Test]
        public static void TestPool() {
            var typeStore = new TypeStore();
            TestPoolInternal(typeStore);
        }
        
        private static void TestPoolInternal(TypeStore typeStore)
        {
            var pool                    = new ReaderPool(typeStore);
            var syncRequestMapper       = typeStore.GetTypeMapper<SyncRequest>();
            var syncRequestTasksMapper  = typeStore.GetTypeMapper<List<SyncRequestTask>>();
            var upsertMapper            = typeStore.GetTypeMapper<UpsertEntities>();
            var entitiesMapper          = typeStore.GetTypeMapper<List<JsonEntity>>();
            
            long start = 0;
            var objects = new List<object>();
            for (int n = 0; n < Count; n++) {
                if (n == 1) start = Mem.GetAllocatedBytes();
                objects.Clear();
                
                var syncRequest = pool.CreateObject(syncRequestMapper);
                var tasks       = pool.CreateObject(syncRequestTasksMapper);
                var upsert1     = pool.CreateObject(upsertMapper);
                var upsert2     = pool.CreateObject(upsertMapper);
                var entities    = pool.CreateObject(entitiesMapper);
                
                objects.Add(syncRequest);
                objects.Add(tasks);
                objects.Add(upsert1);
                objects.Add(upsert2);
                objects.Add(entities);
                
                pool.Reuse();
            }
            var diff = Mem.GetAllocationDiff(start);
            AreEqual(5, objects.Count);
            Mem.NoAlloc(diff);
        }
        
        [Test]
        public static void TestClassPoolParallel()
        {
            var typeStore = new TypeStore();
            Parallel.For(0, ParallelCount, i => TestClassPoolInternal(typeStore));
        }
        
        [Test]
        public static void TestClassPool() {
            TestClassPoolInternal(new TypeStore());
        }
        
        private static void TestClassPoolInternal(TypeStore typeStore)
        {
            var pools                   = new InstancePools();
            var syncRequestMapper       = new InstancePool<SyncRequest>            (pools);
            var syncRequestTasksMapper  = new InstancePool<List<SyncRequestTask>>  (pools);
            var upsertMapper            = new InstancePool<UpsertEntities>         (pools);
            var entitiesMapper          = new InstancePool<List<JsonEntity>>       (pools);
            
            long start = 0;
            var objects = new List<object>();
            for (int n = 0; n < Count; n++) {
                if (n == 1) start = Mem.GetAllocatedBytes();
                objects.Clear();
                
                var syncRequest = syncRequestMapper.Create();
                var tasks       = syncRequestTasksMapper.Create();
                var upsert1     = upsertMapper.Create();
                var upsert2     = upsertMapper.Create();
                var entities    = entitiesMapper.Create();
                
                objects.Add(syncRequest);
                objects.Add(tasks);
                objects.Add(upsert1);
                objects.Add(upsert2);
                objects.Add(entities);
                
                pools.Reuse();
            }
            var diff = Mem.GetAllocationDiff(start);
            AreEqual(5, objects.Count);
            Mem.NoAlloc(diff);
        }
    }
}

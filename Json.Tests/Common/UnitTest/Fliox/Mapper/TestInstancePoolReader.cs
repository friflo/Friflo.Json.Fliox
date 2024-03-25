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
    public static class TestInstancePoolReader
    {
        private const int Count         = 200; // 200_000;
        private const int ParallelCount = 8;
        
        [Test]
        public static void TestInstancePoolReadParallel()
        {
            var typeStore = new TypeStore();
            Parallel.For(0, ParallelCount, i => TestInstancePoolReadInternal(typeStore));
        }
        
        [Test]
        public static void TestInstancePoolRead()
        {
            TestInstancePoolReadInternal(new TypeStore());
        }

        private static void TestInstancePoolReadInternal(TypeStore typeStore)
        {
            var mapper              = new ObjectMapper(typeStore);
            mapper.WriteNullMembers = false;
            
            var syncRequest = new SyncRequest {
                database    = new ShortString("db"),
                tasks       = new ListOne<SyncRequestTask> {
                    new UpsertEntities {
                        container   = new ShortString("test"),
                        entities    = new List<JsonEntity> {
                            new JsonEntity(new JsonKey(11), new JsonValue(@"{""id"":11}")),
                            new JsonEntity(new JsonKey(22), new JsonValue(@"{""id"":22}"))
                        }
                    }
                }
            };
            var json = mapper.WriteAsValue<ProtocolMessage>(syncRequest);
            
            var reader          = mapper.reader;
            var pool            = new ReaderPool(typeStore);
            reader.ReaderPool   = pool;
            long start = 0;
            for (int n = 0; n < Count; n++) {
                if (n == 1) {
                    start = Mem.GetAllocatedBytes();
                }
                pool.Reuse();
                reader.Read<ProtocolMessage>(json);
            }
            var diff = Mem.GetAllocationDiff(start);
            AreEqual($"count: 4, used: 4, types: 4, version: {Count}", pool.ToString());
            Mem.NoAlloc(diff);
        }
    }
}
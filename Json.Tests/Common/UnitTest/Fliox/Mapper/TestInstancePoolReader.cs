// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
{
    public static class TestInstancePoolReader
    {
        private const int Count         = 100; // 1_000_000;
        
        [Test]
        public static void TestInstancePoolRead()
        {
            var typeStore           = new TypeStore();
            var mapper              = new ObjectMapper(typeStore);
            mapper.WriteNullMembers = false;
            
            var syncRequest = new SyncRequest {
                // database    = "db",
                tasks       = new List<SyncRequestTask> {
                    new UpsertEntities {
                        // container   = "test",
                        entities    = new List<JsonEntity> {
                            new JsonEntity(new JsonKey(11), new JsonValue(@"{""id"":11}")),
                            new JsonEntity(new JsonKey(22), new JsonValue(@"{""id"":22}"))
                        }
                    }
                }
            };
            var json = mapper.Write<ProtocolMessage>(syncRequest);
            
            var reader          = mapper.reader;
            var pool            = new InstancePool(typeStore);
            reader.InstancePool = pool;
            long start = 0;
            for (int n = 0; n < Count; n++) {
                if (n == 1)    start = GC.GetAllocatedBytesForCurrentThread();
                pool.Reuse();
                reader.Read<ProtocolMessage>(json);
            }
            var dif = GC.GetAllocatedBytesForCurrentThread() - start;
            AreEqual($"count: 4, used: 1, types: 4, version: {Count}", pool.ToString());
            Console.WriteLine($"dif: {dif}");
            // AreEqual(13464, dif);
        }
    }
}
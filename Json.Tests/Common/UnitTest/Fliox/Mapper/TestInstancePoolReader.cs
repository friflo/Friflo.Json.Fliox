// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

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
       
        [Test]
        public static void TestPoolReferenceParallel()
        {
            var typeStore           = new TypeStore();
            var mapper              = new ObjectMapper(typeStore);
            mapper.WriteNullMembers = false;
            
            var syncRequest = new SyncRequest {
                database    = "db",
                tasks       = new List<SyncRequestTask> {
                    new UpsertEntities {
                        container   = "test",
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
            
            var result          = reader.Read<ProtocolMessage>(json);

            AreEqual("count: 4, used: 4, types: 4, version: 0", pool.ToString());

        }
    }
}
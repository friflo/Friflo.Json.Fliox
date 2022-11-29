// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Hub.Threading;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Utils;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client;
using NUnit.Framework;
using static NUnit.Framework.Assert;


namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Host
{
    
    public static class TestRemoteHost
    {
        [Test]
        public static void TestRemoteHostRequestAlloc() { SingleThreadSynchronizationContext.Run(AssertRemoteHostRequestAlloc); }

        private static async Task AssertRemoteHostRequestAlloc ()
        {
            using (var _            = SharedEnv.Default) { // for LeakTestsFixture
                var typeStore       = SharedEnv.Default.TypeStore;
                var database        = new MemoryDatabase("remote-memory");
                var testHub         = new FlioxHub(database);
                var remoteHost      = new RemoteHost(testHub, null);
                var memoryBuffer    = new MemoryBuffer (true, 4 * 1024);
                var mapper          = new ObjectMapper(typeStore);
                mapper.WriteNullMembers = false;

                // -- create request with upsert task
                var syncWrite = new SyncRequest {
                    database = "remote-memory",
                    tasks = new List<SyncRequestTask> {
                        new UpsertEntities { container = "test", entities = new List<JsonEntity> {
                            new JsonEntity(new JsonKey(1), new JsonValue(@"{""id"":1}"))
                        } }
                    }
                };
                var writeReq = mapper.WriteAsValue<ProtocolMessage>(syncWrite);
                
                // -- create request with read task
                var syncRead = new SyncRequest {
                    database = "remote-memory",
                    tasks = new List<SyncRequestTask> {
                        new ReadEntities { container = "test", ids = new List<JsonKey> { new JsonKey(1)} }
                    }
                };
                var readReq = mapper.WriteAsValue<ProtocolMessage>(syncRead);
                
                var contextWrite    = remoteHost.CreateSyncContext(memoryBuffer, null, default);          
                var writeResponse   = await remoteHost.ExecuteJsonRequest(mapper, writeReq, contextWrite);
                AreEqual(JsonResponseStatus.Ok, writeResponse.status);

                // GC.Collect();
                
                var instancePool = new InstancePool(typeStore);
                mapper.reader.InstancePool = instancePool;
                
                long dif = 0;
                for (int n = 0; n < 10; n++) {
                    long start      = GC.GetAllocatedBytesForCurrentThread();
                    instancePool.Reuse();
                    var contextRead = remoteHost.CreateSyncContext(memoryBuffer, null, default);            
                    var response    = await remoteHost.ExecuteJsonRequest(mapper, readReq, contextRead);
                    
                    dif = GC.GetAllocatedBytesForCurrentThread() - start;
                    if (response.status != JsonResponseStatus.Ok)   Fail("Expect OK");
                }
                var expect = TestUtils.IsDebug() ? 2360 : 1592;
                AreEqual(expect, dif);
            }
        }
    }
}
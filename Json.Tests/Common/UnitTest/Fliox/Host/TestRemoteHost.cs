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
        private class RemoteCx {
            // Add fields to avoid showing them in Rider > Debug > Variables. 
            // If listed Rider calls their ToString() methods causing object instantiations (e.g. of string)
            // which will be listed in Rider > Debug > Memory list
            internal    JsonValue       readReq;
            internal    JsonValue       writeReq;
            internal    SyncContext     contextRead;
            internal    RemoteHost      remoteHost;
            internal    MemoryBuffer    memoryBuffer;
            internal    ObjectMapper    mapper; 
        }
        
        [Test]
        public static  void TestRemoteHostReadRequest() {
            SingleThreadSynchronizationContext.Run(async () =>
            {
                using (var sharedEnv = SharedEnv.Default) { // for LeakTestsFixture
                    var cx = await PrepareRemoteHost(sharedEnv);
                    // GC.Collect();
                    long dif = 0;
                    for (int n = 0; n < 10; n++) {
                        long start      = GC.GetAllocatedBytesForCurrentThread();
                        cx.mapper.reader.InstancePool.Reuse();
                        cx.contextRead  = cx.remoteHost.CreateSyncContext(cx.memoryBuffer, null, default);            
                        var response    = await cx.remoteHost.ExecuteJsonRequest(cx.mapper, cx.readReq, cx.contextRead);
                        
                        dif = GC.GetAllocatedBytesForCurrentThread() - start;
                        if (response.status != JsonResponseStatus.Ok)   Fail("Expect OK");
                    }
                    var expect = TestUtils.IsDebug() ? 2312 : 1552;
                    AreEqual(expect, dif);
                }
            });
        }
        
        [Test]
        public static  void TestRemoteHostWriteRequest() {
            SingleThreadSynchronizationContext.Run(async () =>
            {
                using (var sharedEnv = SharedEnv.Default) { // for LeakTestsFixture
                    var cx = await PrepareRemoteHost(sharedEnv);
                    
                    long dif = 0;
                    for (int n = 0; n < 10; n++) {
                        long start      = GC.GetAllocatedBytesForCurrentThread();
                        cx.mapper.reader.InstancePool.Reuse();
                        cx.contextRead  = cx.remoteHost.CreateSyncContext(cx.memoryBuffer, null, default);            
                        var response    = await cx.remoteHost.ExecuteJsonRequest(cx.mapper, cx.writeReq, cx.contextRead);
                        
                        dif = GC.GetAllocatedBytesForCurrentThread() - start;
                        if (response.status != JsonResponseStatus.Ok)   Fail("Expect OK");
                    }
                    var expect = TestUtils.IsDebug() ? 1592 : 872;
                    AreEqual(expect, dif);
                }
            });
        }

        private static async Task<RemoteCx> PrepareRemoteHost (SharedEnv sharedEnv)
        {
            var cx = new RemoteCx();
            var typeStore       = sharedEnv.TypeStore;
            var database        = new MemoryDatabase("remote-memory");
            var testHub         = new FlioxHub(database);
            cx.remoteHost       = new RemoteHost(testHub, null);
            cx.memoryBuffer     = new MemoryBuffer (true, 4 * 1024);
            cx.mapper           = new ObjectMapper(typeStore);
            cx.mapper.WriteNullMembers = false;
            cx.mapper.reader.InstancePool = new InstancePool(typeStore);;

            // -- create request with upsert task
            var syncWrite = new SyncRequest {
                database = "remote-memory",
                tasks = new List<SyncRequestTask> {
                    new UpsertEntities { container = "test", entities = new List<JsonEntity> {
                        new JsonEntity(new JsonKey(1), new JsonValue(@"{""id"":1}"))
                    } }
                }
            };
            cx.writeReq = cx.mapper.WriteAsValue<ProtocolMessage>(syncWrite);
            
            // -- create request with read task
            var syncRead = new SyncRequest {
                database = "remote-memory",
                tasks = new List<SyncRequestTask> {
                    new ReadEntities { container = "test", ids = new List<JsonKey> { new JsonKey(1)} }
                }
            };
            cx.readReq = cx.mapper.WriteAsValue<ProtocolMessage>(syncRead);
            
            var contextWrite    = cx.remoteHost.CreateSyncContext(cx.memoryBuffer, null, default);          
            var writeResponse   = await cx.remoteHost.ExecuteJsonRequest(cx.mapper, cx.writeReq, contextWrite);
            AreEqual(JsonResponseStatus.Ok, writeResponse.status);
            return cx;
        }
    }
}
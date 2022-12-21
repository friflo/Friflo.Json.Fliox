// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Pools;
using Friflo.Json.Fliox.Utils;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client;
using NUnit.Framework;
using static NUnit.Framework.Assert;


namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Host
{
    public static class TestRemoteHost
    {
        internal class RemoteCx {
            // Add fields to avoid showing them in Rider > Debug > Variables. 
            // If listed Rider calls their ToString() methods causing object instantiations (e.g. of string)
            // which will be listed in Rider > Debug > Memory list
            internal            JsonValue               readReq;
            internal            JsonValue               writeReq;
            internal            SyncContext             contextRead;
            internal            FlioxHub                hub;
            internal            RemoteHost              remoteHost;
            internal            MemoryBuffer            memoryBuffer;
            internal            ObjectMapper            mapper;
            internal            SyncPools               syncPools;
            private readonly    List<SyncRequestTask>   eventTasks = new List<SyncRequestTask>();
            private readonly    List<JsonValue>         jsonTasks  = new List<JsonValue>();
            
            internal SyncContext CreateSyncContext() {
                return new SyncContext (remoteHost.sharedEnv, null, new SyncBuffers(eventTasks, jsonTasks), syncPools);
            }
        }
        
        [Test]
        public static  void TestRemoteHostReadRequest() {
            using (var sharedEnv = SharedEnv.Default) { // for LeakTestsFixture
                var cx          = PrepareRemoteHost(sharedEnv);
                cx.contextRead  = cx.CreateSyncContext();   // reused context
                long dif = 0;
                for (int n = 0; n < 10; n++) {
                    long start      = GC.GetAllocatedBytesForCurrentThread();
                    cx.contextRead.Init();
                    cx.contextRead.SetMemoryBuffer(cx.memoryBuffer);
                    cx.mapper.reader.InstancePool.Reuse();
                    var response    = cx.remoteHost.ExecuteJsonRequest(cx.mapper, cx.readReq, cx.contextRead);
                    
                    dif = GC.GetAllocatedBytesForCurrentThread() - start;
                    if (response.status != JsonResponseStatus.Ok)   Fail("Expect OK");
                }
                AreEqual(776, dif);
            }
        }
        
        [Test]
        public static  void TestRemoteHostWriteRequest() {
            using (var sharedEnv = SharedEnv.Default) { // for LeakTestsFixture
                var cx          = PrepareRemoteHost(sharedEnv);
                cx.contextRead  = cx.CreateSyncContext();   // reused context
                
                long dif = 0;
                for (int n = 0; n < 10; n++) {
                    long start      = GC.GetAllocatedBytesForCurrentThread();
                    cx.contextRead.Init();
                    cx.contextRead.SetMemoryBuffer(cx.memoryBuffer);
                    cx.mapper.reader.InstancePool.Reuse();
                    var response    = cx.remoteHost.ExecuteJsonRequest(cx.mapper, cx.writeReq, cx.contextRead);
                    
                    dif = GC.GetAllocatedBytesForCurrentThread() - start;
                    if (response.status != JsonResponseStatus.Ok)   Fail("Expect OK");
                }
                AreEqual(80, dif);
            }
        }
        
        [Test]
        public static  void TestRemoteHostWriteSubscribe() {
            using (var sharedEnv = SharedEnv.Default) { // for LeakTestsFixture
                var cx          = PrepareRemoteHost(sharedEnv);
                cx.contextRead  = cx.CreateSyncContext();   // reused context
                cx.hub.EventDispatcher = new EventDispatcher(EventDispatching.Queue, sharedEnv);

                for (int n = 0; n < 2; n++) {
                    var client  = new GameClient(cx.hub) { ClientId = $"client-{n}" };
                    client.players.SubscribeChanges(Change.All, (changes, context) =>  {} );
                    client.SyncTasksSynchronous();
                }

                long dif = 0;
                for (int n = 0; n < 10; n++) {
                    long start      = GC.GetAllocatedBytesForCurrentThread();
                    cx.contextRead.Init();
                    cx.contextRead.SetMemoryBuffer(cx.memoryBuffer);
                    cx.mapper.reader.InstancePool.Reuse();
                    var response    = cx.remoteHost.ExecuteJsonRequest(cx.mapper, cx.writeReq, cx.contextRead);
                    cx.hub.EventDispatcher?.SendQueuedEvents();
                    
                    dif = GC.GetAllocatedBytesForCurrentThread() - start;
                    if (response.status != JsonResponseStatus.Ok)   Fail("Expect OK");
                }
                AreEqual(80, dif);
            }
        }

        private static RemoteCx PrepareRemoteHost (SharedEnv sharedEnv)
        {
            var cx = new RemoteCx();
            var typeStore       = sharedEnv.TypeStore;
            var database        = new MemoryDatabase("remote-memory", smallValueSize: 1024);
            cx.hub              = new FlioxHub(database, sharedEnv);
            cx.remoteHost       = new RemoteHost(cx.hub, sharedEnv);
            cx.memoryBuffer     = new MemoryBuffer (4 * 1024);
            cx.mapper           = new ObjectMapper(typeStore);
            cx.mapper.WriteNullMembers      = false;
            cx.mapper.reader.InstancePool   = new ReaderInstancePool(typeStore);
            cx.syncPools        = new SyncPools(typeStore);

            // -- create request with upsert task
            var syncWrite = new SyncRequest {
                database = "remote-memory",
                tasks = new List<SyncRequestTask> {
                    new UpsertEntities { container = "players", entities = new List<JsonEntity> {
                        new JsonEntity(new JsonKey(1), new JsonValue(@"{""id"":1}"))
                    } }
                }
            };
            cx.writeReq = cx.mapper.WriteAsValue<ProtocolMessage>(syncWrite);
            
            // -- create request with read task
            var syncRead = new SyncRequest {
                database = "remote-memory",
                tasks = new List<SyncRequestTask> {
                    new ReadEntities { container = "players", ids = new List<JsonKey> { new JsonKey(1)} }
                }
            };
            cx.readReq = cx.mapper.WriteAsValue<ProtocolMessage>(syncRead);
            
            var contextWrite    = cx.CreateSyncContext();
            contextWrite.SetMemoryBuffer(cx.memoryBuffer);
            
            var writeResponse   = cx.remoteHost.ExecuteJsonRequest(cx.mapper, cx.writeReq, contextWrite);
            AreEqual(JsonResponseStatus.Ok, writeResponse.status);
            return cx;
        }
    }
}
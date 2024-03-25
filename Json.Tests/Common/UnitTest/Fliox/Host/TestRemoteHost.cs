// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Remote.Tools;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Pools;
using Friflo.Json.Fliox.Utils;
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
            internal            MemoryBuffer            memoryBuffer;
            internal            ObjectMapper            mapper;
            internal            SyncPools               syncPools;
            private readonly    List<SyncRequestTask>   syncTasks  = new List<SyncRequestTask>();
            private readonly    List<SyncRequestTask>   eventTasks = new List<SyncRequestTask>();
            private readonly    List<JsonValue>         jsonTasks  = new List<JsonValue>();
            
            internal SyncContext CreateSyncContext() {
                return new SyncContext (hub.sharedEnv, null, new SyncBuffers(syncTasks, eventTasks, jsonTasks), syncPools);
            }
        }
        
        [Test]
        public static  void TestRemoteHostReadRequest() {
            using (var sharedEnv = SharedEnv.Default) { // for LeakTestsFixture
                var cx          = PrepareRemoteHost(sharedEnv);
                cx.contextRead  = cx.CreateSyncContext();   // reused context
                long diff = 0;
                for (int n = 0; n < 10; n++) {
                    long start      = Mem.GetAllocatedBytes();
                    cx.contextRead.Init();
                    cx.contextRead.SetMemoryBuffer(cx.memoryBuffer);
                    cx.mapper.reader.ReaderPool.Reuse();
                    var response    = RemoteHostUtils.ExecuteJsonRequest(cx.hub, cx.mapper, cx.readReq, cx.contextRead);
                    
                    diff = Mem.GetAllocationDiff(start);
                    if (response.status != JsonResponseStatus.Ok)   Fail("Expect OK");
                }
                Mem.AreEqual(184, diff);
            }
        }
        
        [Test]
        public static  void TestRemoteHostWriteRequest() {
            using (var sharedEnv = SharedEnv.Default) { // for LeakTestsFixture
                var cx          = PrepareRemoteHost(sharedEnv);
                cx.contextRead  = cx.CreateSyncContext();   // reused context
                
                long diff = 0;
                for (int n = 0; n < 10; n++) {
                    long start      = Mem.GetAllocatedBytes();
                    cx.contextRead.Init();
                    cx.contextRead.SetMemoryBuffer(cx.memoryBuffer);
                    cx.mapper.reader.ReaderPool.Reuse();
                    var response    = RemoteHostUtils.ExecuteJsonRequest(cx.hub, cx.mapper, cx.writeReq, cx.contextRead);
                    
                    diff = Mem.GetAllocationDiff(start);
                    if (response.status != JsonResponseStatus.Ok)   Fail("Expect OK");
                }
                Mem.AreEqual(72, diff);
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

                long diff = 0;
                for (int n = 0; n < 10; n++) {
                    long start      = Mem.GetAllocatedBytes();
                    cx.contextRead.Init();
                    cx.contextRead.SetMemoryBuffer(cx.memoryBuffer);
                    cx.mapper.reader.ReaderPool.Reuse();
                    var response    = RemoteHostUtils.ExecuteJsonRequest(cx.hub, cx.mapper, cx.writeReq, cx.contextRead);
                    cx.hub.EventDispatcher?.SendQueuedEvents();
                    
                    diff = Mem.GetAllocationDiff(start);
                    if (response.status != JsonResponseStatus.Ok)   Fail("Expect OK");
                }
                Mem.AreEqual(72, diff);
            }
        }

        private static RemoteCx PrepareRemoteHost (SharedEnv sharedEnv)
        {
            var cx = new RemoteCx();
            var typeStore       = sharedEnv.TypeStore;
            var database        = new MemoryDatabase("remote-memory") { SmallValueSize = 1024 };
            cx.hub              = new FlioxHub(database, sharedEnv);
            cx.memoryBuffer     = new MemoryBuffer (4 * 1024);
            cx.mapper           = new ObjectMapper(typeStore);
            cx.mapper.Pretty            = true;
            cx.mapper.WriteNullMembers  = false;
            cx.mapper.reader.ReaderPool = new ReaderPool(typeStore);
            cx.syncPools        = new SyncPools();

            // -- create request with upsert task
            var syncWrite = new SyncRequest {
                database    = new ShortString("remote-memory"),
                tasks       = new ListOne<SyncRequestTask> {
                    new UpsertEntities { container = new ShortString("players"), entities = new List<JsonEntity> {
                        new JsonEntity(new JsonKey(1), new JsonValue(@"{""id"":1}"))
                    } }
                }
            };
            cx.writeReq = cx.mapper.WriteAsValue<ProtocolMessage>(syncWrite);
            
            // -- create request with read task
            var syncRead = new SyncRequest {
                database    = new ShortString("remote-memory"),
                tasks       = new ListOne<SyncRequestTask> {
                    new ReadEntities { container = new ShortString("players"), ids = new ListOne<JsonKey> { new JsonKey(1)} }
                }
            };
            cx.readReq = cx.mapper.WriteAsValue<ProtocolMessage>(syncRead);
            
            var contextWrite    = cx.CreateSyncContext();
            contextWrite.SetMemoryBuffer(cx.memoryBuffer);
            
            var writeResponse   = RemoteHostUtils.ExecuteJsonRequest(cx.hub, cx.mapper, cx.writeReq, contextWrite);
            AreEqual(JsonResponseStatus.Ok, writeResponse.status);
            return cx;
        }
    }
}
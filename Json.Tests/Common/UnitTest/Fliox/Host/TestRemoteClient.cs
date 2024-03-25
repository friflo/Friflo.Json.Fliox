// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client;
using NUnit.Framework;

// ReSharper disable UseObjectOrCollectionInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Host
{
    public static class TestRemoteClient
    {
        private class ClientCx
        {
            // Add fields to avoid showing them in Rider > Debug > Variables. 
            // If listed Rider calls their ToString() methods causing object instantiations (e.g. of string)
            // which will be listed in Rider > Debug > Memory list
            internal GameClient     client;
            internal EntityDatabase database;
            internal FlioxHub       hub;
            internal SyncResult     result;
        }
        
        [Test]
        public static  void TestRemoteClient_UpsertMemoryParallel() {
            // reusing sync instances give a 50% performance enhancement in Unity when using 8 threads
            Parallel.For(0, 8, i => {
                UpsertMemory();
            });
        }
        
        [Test]
        public static  void TestRemoteClient_UpsertMemory() {
            var dif = UpsertMemory();
            Mem.AreEqual(208, dif);
        }
        
        private static long UpsertMemory() {
            var sharedEnv = SharedEnv.Default;
            var cx = new ClientCx();
            cx.database = new MemoryDatabase("test") { SmallValueSize = 1024, ContainerType = MemoryType.NonConcurrent };
            cx.hub      = new FlioxHub(cx.database, sharedEnv);
            cx.client   = new GameClient(cx.hub);
            
            var player  = new Player { id = 1 };
            var players = new List<Player>{player}; 

            long start = 0;
            for (int n = 0; n < 10; n++) {
                start = Mem.GetAllocatedBytes();
                cx.client.players.UpsertRange(players);
                cx.result = cx.client.SyncTasksSynchronous();
                
                cx.result.Reuse(cx.client);
            }
            return Mem.GetAllocationDiff(start);
        }
        
        [Test]
        public static  void TestRemoteClient_ReadMemoryParallel() {
            Parallel.For(0, 8, i => {
                ReadMemory();
            });
        }
        
        [Test]
        public static  void TestRemoteClient_ReadMemory() {
            var dif         = ReadMemory();
            var expected    = TestUtils.IsDebug() ? 368 : 344;  // Test Debug & Release
            Mem.AreEqual(expected, dif);
        }
        
        private static  long ReadMemory() {
            var sharedEnv = SharedEnv.Default;
            var cx = new ClientCx();
            cx.database = new MemoryDatabase("test") { SmallValueSize = 1024, ContainerType = MemoryType.NonConcurrent };
            cx.hub      = new FlioxHub(cx.database, sharedEnv);
            cx.client   = new GameClient(cx.hub);
            
            var player = new Player { id = 1 };
            cx.client.players.Upsert(player);
            cx.client.SyncTasksSynchronous();

            long start = 0;
            for (int n = 0; n < 10; n++) {
                start = Mem.GetAllocatedBytes();
                cx.client.players.Read().Find(1);
                cx.result = cx.client.SyncTasksSynchronous();
                
                cx.result.Reuse(cx.client);
            }
            return Mem.GetAllocationDiff(start);
        }
    }
}
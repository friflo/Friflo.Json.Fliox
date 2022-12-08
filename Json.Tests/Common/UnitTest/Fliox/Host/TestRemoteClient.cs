// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client;
using NUnit.Framework;
using static NUnit.Framework.Assert;

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
                TestRemoteClient_UpsertMemory();
            });
        }
        
        [Test]
        public static  void TestRemoteClient_UpsertMemory() {
            var sharedEnv = SharedEnv.Default;
            var cx = new ClientCx();
            cx.database = new MemoryDatabase("test", smallValueSize: 1024, type: MemoryType.NonConcurrent);
            cx.hub      = new FlioxHub(cx.database, sharedEnv);
            cx.client   = new GameClient(cx.hub);
            
            var player = new Player { id = 1 };

            long start = 0;
            for (int n = 0; n < 10; n++) {
                start = GC.GetAllocatedBytesForCurrentThread();
                cx.client.players.Upsert(player);
                cx.result = cx.client.SyncTasksSynchronous();
                
                cx.result.Reuse(cx.client);
            }
            var dif = GC.GetAllocatedBytesForCurrentThread() - start;
            
            var expected    = TestUtils.IsDebug() ? 288 : 288;  // Test Debug & Release
            AreEqual(expected, dif);
        }
        
        [Test]
        public static  void TestRemoteClient_ReadMemoryParallel() {
            Parallel.For(0, 8, i => {
                TestRemoteClient_ReadMemory();
            });
        }
        
        [Test]
        public static  void TestRemoteClient_ReadMemory() {
            var sharedEnv = SharedEnv.Default;
            var cx = new ClientCx();
            cx.database = new MemoryDatabase("test", smallValueSize: 1024, type: MemoryType.NonConcurrent);
            cx.hub      = new FlioxHub(cx.database, sharedEnv);
            cx.client   = new GameClient(cx.hub);
            
            var player = new Player { id = 1 };
            cx.client.players.Upsert(player);
            cx.client.SyncTasksSynchronous();

            long start = 0;
            for (int n = 0; n < 10; n++) {
                start = GC.GetAllocatedBytesForCurrentThread();
                cx.client.players.Read().Find(1);
                cx.result = cx.client.SyncTasksSynchronous();
                
                cx.result.Reuse(cx.client);
            }
            var dif = GC.GetAllocatedBytesForCurrentThread() - start;
            
            var expected    = TestUtils.IsDebug() ? 1360 : 1336;  // Test Debug & Release
            AreEqual(expected, dif);
        }
    }
}
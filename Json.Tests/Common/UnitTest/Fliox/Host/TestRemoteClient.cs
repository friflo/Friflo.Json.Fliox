// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
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
        }
        
        [Test]
        public static  void TestRemoteClient_UpsertMemory() {
            using (var sharedEnv = SharedEnv.Default) {
                var cx = new ClientCx();
                cx.database = new MemoryDatabase("test", smallValueSize: 1024);
                cx.hub      = new FlioxHub(cx.database, sharedEnv);
                cx.client   = new GameClient(cx.hub);
                
                var player = new Player { id = 1 };

                long start = 0;
                for (int n = 0; n < 10; n++) {
                    start = GC.GetAllocatedBytesForCurrentThread();
                    cx.client.players.Upsert(player);
                    var result = cx.client.SyncTasksSynchronous();
                    
                    result.ReUse(cx.client);
                }
                var dif = GC.GetAllocatedBytesForCurrentThread() - start;
                
                var expected    = TestUtils.IsDebug() ? 1064 : 1024;  // Test Debug & Release
                AreEqual(expected, dif);
            }
        }
        
        [Test]
        public static  void TestRemoteClient_ReadMemory() {
            using (var sharedEnv = SharedEnv.Default) {
                var cx = new ClientCx();
                cx.database = new MemoryDatabase("test", smallValueSize: 1024);
                cx.hub      = new FlioxHub(cx.database, sharedEnv);
                cx.client   = new GameClient(cx.hub);
                
                var player = new Player { id = 1 };
                cx.client.players.Upsert(player);
                cx.client.SyncTasksSynchronous();

                long start = 0;
                for (int n = 0; n < 10; n++) {
                    start = GC.GetAllocatedBytesForCurrentThread();
                    cx.client.players.Read().Find(1);
                    var result = cx.client.SyncTasksSynchronous();
                    
                    result.ReUse(cx.client);
                }
                var dif = GC.GetAllocatedBytesForCurrentThread() - start;
                
                var expected    = TestUtils.IsDebug() ? 2832 : 2808;  // Test Debug & Release
                AreEqual(expected, dif);
            }
        }
    }
}
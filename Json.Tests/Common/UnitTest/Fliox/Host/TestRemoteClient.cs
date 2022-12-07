// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Host
{
    public static class TestRemoteClient
    {
        [Test]
        public static  void TestRemoteHostReadRequest() {
            using (var sharedEnv = SharedEnv.Default) { 
                var cx = TestRemoteHost.PrepareRemoteHost(sharedEnv);
                
                var player = new Player();

                long start = 0;
                for (int n = 0; n < 10; n++) {
                    start = GC.GetAllocatedBytesForCurrentThread();
                    cx.client.players.Upsert(player);
                    var result = cx.client.SyncTasksSynchronous();
                    
                    result.ReUse();
                }
                var dif = GC.GetAllocatedBytesForCurrentThread() - start;
                
                var expected    = TestUtils.IsDebug() ? 1672 : 1592;  // Test Debug & Release
                AreEqual(expected, dif);
            }
        }
    }
}
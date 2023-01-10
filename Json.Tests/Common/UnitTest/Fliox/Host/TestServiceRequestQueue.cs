// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Host
{
    public static class TestServiceRequestQueue
    {
        [Test]
        public static async Task TestServiceRequestQueueRun() {
            var service     = new DatabaseService(true);
            var database    = new MemoryDatabase("test", service);
            var hub         = new FlioxHub(database);
            var client      = new FlioxClient(hub);
            
            for (int n = 0; n < 10; n++) {
                var echoResult = client.std.Echo("hello");
                var task = client.SyncTasks();
                
                await service.ExecuteQueuedRequestsAsync();
                
                if (!task.IsCompleted) {
                    Fail("Expect task completed");
                }
                if (echoResult.Result != "hello") {
                    Fail("Expect hello");
                }
            }
        }
    }
}
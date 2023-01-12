// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Threading;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Host
{
    internal class QueueingService : DatabaseService
    {
        /// <summary>
        /// using queueRequests == true enables handlers methods like <see cref="Test"/> are called / executed
        /// sequentially from a single thread using <see cref="DatabaseService.ExecuteQueuedRequestsAsync"/>
        /// </summary>
        internal QueueingService() : base (true) {
            AddMessageHandlers(this, null);
        }
        
        private static string Test(Param<string> param, MessageContext command)
        {
            param.Get(out var result, out _);
            return result;
        }
    }
    
    public static class TestServiceRequestQueue
    {
        [Test]
        public static async Task TestServiceRequestQueueRun() {
            var service     = new QueueingService();
            var database    = new MemoryDatabase("test", service);
            var hub         = new FlioxHub(database);
            var client      = new FlioxClient(hub);
            
            for (int n = 0; n < 10; n++) {
                var commandTask = client.SendCommand<string,string>("Test", "foo");
                var task = client.SyncTasks();
                
                await service.ExecuteQueuedRequestsAsync();
                
                if (!task.IsCompleted) {
                    Fail("Expect task completed");
                }
                if (commandTask.Result != "foo") {
                    Fail("Expect foo");
                }
            }
        }
        
        [Test]
        public static void TestServiceRequestQueueThreaded() {
            var service     = new QueueingService();
            var database    = new MemoryDatabase("test", service);
            var hub         = new FlioxHub(database);
            var count       = 10;
            var finished    = 0;
            
            SingleThreadSynchronizationContext.Run(async () =>
            {
                var clients = new List<FlioxClient>();
                for (int n = 0; n < count; n++) {
                    clients.Add(new FlioxClient(hub) { ClientId = $"{n}" });
                }
                var ignore = Parallel.ForEachAsync(clients, async (client, token) =>
                {
                    for (int n = 0; n < 2; n++) {
                        var commandTask = client.SendCommand<string,string>("Test", client.ClientId);
                        await client.SyncTasks();
                        
                        if (commandTask.Result !=  client.ClientId) {
                            Fail($"Expect {client.ClientId}");
                        }
                    }
                    Interlocked.Increment(ref finished);
                });
                
                // the single service will be accessed from multiple clients running on various ThreadPool worker threads 
                while (finished < count) {

                    var executed = await service.ExecuteQueuedRequestsAsync();
                    Console.WriteLine($"executed: {executed}");
                }
            });
        }
    }
}
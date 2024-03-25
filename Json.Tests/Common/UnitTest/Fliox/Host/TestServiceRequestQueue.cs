// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Hub.Threading;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Host
{
    internal class QueueingService : DatabaseService
    {
        /// <summary>
        /// using queueRequests == true enables handlers methods like <see cref="Test"/> are called / executed
        /// sequentially from a single thread using <see cref="DatabaseService.queue"/>
        /// </summary>
        internal QueueingService() : base (new DatabaseServiceQueue()) {
        }
        
        [CommandHandler]
        private static Result<string> Test(Param<string> param, MessageContext context)
        {
            return param.Value;
        }
    }
    
    public static class TestServiceRequestQueue
    {
#if !UNITY_5_3_OR_NEWER
        [Test]
        public static async Task TestServiceRequestQueueRun() {
            var service     = new QueueingService();
            var database    = new MemoryDatabase("test", null, service);
            var hub         = new FlioxHub(database);
            var client      = new FlioxClient(hub);
            
            for (int n = 0; n < 10; n++) {
                var commandTask = client.SendCommand<string,string>("Test", "foo");
                var task = client.SyncTasks();
                
                await service.queue.ExecuteQueuedRequestsAsync();
                
                if (!task.IsCompleted) {
                    Fail("Expect task completed");
                }
                if (commandTask.Result != "foo") {
                    Fail("Expect foo");
                }
            }
        }
#endif
        
        [Test]
        public static void TestServiceRequestQueueThreaded() {
            var service     = new QueueingService();
            var database    = new MemoryDatabase("test", null, service);
            var hub         = new FlioxHub(database);
            var count       = 10;
            var finished    = 0;
            
            SingleThreadSynchronizationContext.Run(async () =>
            {
                async Task RunClient (FlioxClient client ) {
                    for (int n = 0; n < 2; n++) {
                        var commandTask = client.SendCommand<string,string>("Test", client.ClientId);
                        await client.SyncTasks();
                        
                        if (commandTask.Result !=  client.ClientId) {
                            Fail($"Expect {client.ClientId}");
                        }
                    }
                    Interlocked.Increment(ref finished);
                };
                for (int n = 0; n < count; n++) {
                    var client = new FlioxClient(hub) { ClientId = $"{n}" };
                    var ignore = Task.Run(async () =>
                    {
                        await RunClient(client);    
                    });
                }

                // the single service will be accessed from multiple clients running on various ThreadPool worker threads 
                while (finished < count) {
                    await Task.Delay(1);
                    var executed = await service.queue.ExecuteQueuedRequestsAsync();
                    Console.WriteLine($"executed: {executed}");
                }
            });
        }
        
        // [Test]
        public static void TestWebSocket() {
            var hub     = new WebSocketClientHub("main_db", "ws://localhost:8010/fliox/");
            var client  = new FlioxClient(hub) { UserId = "admin", Token = "admin" };

            SingleThreadSynchronizationContext.Run(async () =>
            {
                var tasks = new List<Task>();
                for (int n = 0; n < 10000; n++) {
                    client.std.Echo("Test");
                    var task = client.SyncTasks();
                    tasks.Add(task);
                }
                await Task.WhenAll(tasks);
                await hub.Close();
            });
        }
    }
}
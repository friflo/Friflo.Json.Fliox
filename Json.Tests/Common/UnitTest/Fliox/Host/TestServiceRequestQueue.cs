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
        /// using queueRequests == true enables handlers methods like <see cref="Test"/> are called from
        /// a single thread using <see cref="DatabaseService.ExecuteQueuedRequestsAsync"/>
        /// </summary>
        internal QueueingService() : base (true) {
            AddMessageHandlers(this, null);
        }
        
        private static int Test(Param<int> param, MessageContext command)
        {
            param.Get(out int result, out _);
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
                var commandTask = client.SendCommand<int,int>("Test", 42);
                var task = client.SyncTasks();
                
                await service.ExecuteQueuedRequestsAsync();
                
                if (!task.IsCompleted) {
                    Fail("Expect task completed");
                }
                if (commandTask.Result != 42) {
                    Fail("Expect hello");
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
            
            void RunClient(int param) {
                SingleThreadSynchronizationContext.Run(async () => {
                    var client = new FlioxClient(hub);
                    for (int n = 0; n < 2; n++) {
                        var commandTask = client.SendCommand<int,int>("Test", param);
                        await client.SyncTasks();

                        if (commandTask.Result != param) {
                            Fail("Expect hello");
                        }
                    }
                    Interlocked.Increment(ref finished);
                });
            }

            SingleThreadSynchronizationContext.Run(async () =>
            {
                var threads = new List<Thread>();
                for (int n = 0; n < count; n++) {
                    var param = n;
                    threads.Add(new Thread(() => {
                        RunClient(param);
                    }));
                }
                // a single service will be accessed from multiple clients each using its own thread 
                foreach (var thread in threads) {
                    thread.Start();
                }
                
                while (finished < count) {
                    await Task.Delay(1);
                    var executed = await service.ExecuteQueuedRequestsAsync();
                    Console.WriteLine($"executed: {executed}");
                }
            });
        }
    }
}
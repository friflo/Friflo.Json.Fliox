// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Threading;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Hubs;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public partial class TestHappy
    {
        /// <summary>
        /// Test multiple calls of <see cref="FlioxClient.SyncTasks"/> without await-ing each call individually.
        /// This enables "pipelining" of scheduling <see cref="FlioxClient.SyncTasks"/> calls.
        /// <br></br>
        /// Doing this erode the performance benefits handling multiple <see cref="SyncTask"/>'s via a single
        /// <see cref="FlioxClient.SyncTasks"/> but the behavior of <see cref="FlioxClient.SyncTasks"/> needs to be same -
        /// if its awaited or not.
        /// <br></br>
        /// Note:
        /// In a game loop it can happen that multiple calls to <see cref="FlioxClient.SyncTasks"/> are pending because
        /// processing them is slower than creating new <see cref="FlioxClient.SyncTasks"/> requests.
        /// <br></br>
        /// Motivation for "pipelining": <br></br>
        /// SyncTasks() calls are executed in order but overall execution time is improved in scenarios with high latency to a
        /// remote host because RTT is added only once instead of n times for n awaited SyncTasks() calls. 
        /// </summary>
        [Test] public static void TestSyncConcurrency () {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            {
                SingleThreadSynchronizationContext.Run(async () => {
                    using (var memoryDb = new MemoryDatabase(TestGlobals.DB))
                    using (var hub      = new AsyncDatabaseHub(memoryDb, TestGlobals.Shared))
                    using (var store    = new PocStore(hub) { UserId = "store"}) {
                        await SyncConcurrencyInit(store);
                    }
                });
            }
        }
        
        private static async Task SyncConcurrencyInit(PocStore store) {
            var peter   = new Customer{ id = "customer-peter",  name = "Peter"};
            var paul    = new Customer{ id = "customer-paul",   name = "Paul"};
            for (int n = 0; n < 1; n++) {
                await SyncConcurrency(store, peter, paul);
            }
        }
        
        private static async Task SyncConcurrency(PocStore store, Customer peter, Customer paul) {
            var customers = store.customers;
            {
                customers.Upsert(peter);
                AreEqual(1, store.Tasks.Count);
                var sync1 = store.SyncTasks();
                AreEqual(0, store.Tasks.Count); // assert Tasks are cleared without awaiting SyncTasks()
                
                store.SendMessage("Some message"); // add additional task to second SyncTasks() to identify the result by task.Count
                customers.Upsert(paul);
                AreEqual(2, store.Tasks.Count);
                var sync2 = store.SyncTasks();
                AreEqual(0, store.Tasks.Count); // assert Tasks are cleared without awaiting SyncTasks()
                
                // by using AsyncDatabase SyncTasks() response handling is executed at WhenAll() instead synchronously in SyncTasks()
                await Task.WhenAll(sync1, sync2);  // ------ sync point
                AreEqual(1, sync1.Result.Tasks.Count);
                AreEqual(2, sync2.Result.Tasks.Count);
            } {
                var readCustomers1 = customers.Read();
                var findPeter = readCustomers1.Find("customer-peter");
                var sync1 = store.SyncTasks();
                
                store.SendMessage("Some message"); // add additional task to second SyncTasks() to identify the result by task.Count
                var readCustomers2 = customers.Read();
                IsFalse(readCustomers1 == readCustomers2);
                var findPaul = readCustomers2.Find("customer-paul");
                var sync2 = store.SyncTasks();
                
                // by using AsyncDatabase SyncTasks() response handling is executed at WhenAll() instead synchronously in SyncTasks()
                await Task.WhenAll(sync1, sync2);  // ------ sync point
                AreEqual(1, sync1.Result.Tasks.Count);
                AreEqual(2, sync2.Result.Tasks.Count);

                AreEqual("Peter", findPeter.Result.name);
                AreEqual("Paul",  findPaul.Result.name);
            }
        }
    }
}

// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Client;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Remote;
using Friflo.Json.Fliox.DB.Threading;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public partial class TestStore
    {
        /// <summary>
        /// Test multiple calls of <see cref="EntityStore.SynchronizeAsync"/> without await-ing each call individually.
        /// This enables "pipelining" of scheduling <see cref="EntityStore.SynchronizeAsync"/> calls.
        /// <br></br>
        /// Doing this erode the performance benefits handling multiple <see cref="SyncTask"/>'s via a single
        /// <see cref="EntityStore.SynchronizeAsync"/> but the behavior of <see cref="EntityStore.SynchronizeAsync"/> needs to be same -
        /// if its awaited or not.
        /// <br></br>
        /// Note:
        /// In a game loop it can happen that multiple calls to <see cref="EntityStore.SynchronizeAsync"/> are pending because
        /// processing them is slower than creating new <see cref="EntityStore.SynchronizeAsync"/> requests.
        /// <br></br>
        /// Motivation for "pipelining": <br></br>
        /// SynchronizeAsync() calls are executed in order but overall execution time is improved in scenarios with high latency to a
        /// <see cref="RemoteHostDatabase"/> because RTT is added only once instead of n times for n awaited SynchronizeAsync() calls. 
        /// </summary>
        [Test] public static void TestSyncConcurrency () {
            using (var _                = UtilsInternal.SharedPools) // for LeakTestsFixture
            {
                SingleThreadSynchronizationContext.Run(async () => {
                    using (var memoryDb = new MemoryDatabase())
                    using (var database = new AsyncDatabase(memoryDb))
                    using (var store  = new PocStore(database, "store")) {
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
                var sync1 = store.SynchronizeAsync();
                AreEqual(0, store.Tasks.Count); // assert Tasks are cleared without awaiting SynchronizeAsync()
                
                store.SendMessage("Some message"); // add additional task to second SynchronizeAsync() to identify the result by task.Count
                customers.Upsert(paul);
                AreEqual(2, store.Tasks.Count);
                var sync2 = store.SynchronizeAsync();
                AreEqual(0, store.Tasks.Count); // assert Tasks are cleared without awaiting SynchronizeAsync()
                
                // by using AsyncDatabase SynchronizeAsync() response handling is executed at WhenAll() instead synchronously in SynchronizeAsync()
                await Task.WhenAll(sync1, sync2);  // ------ sync point
                AreEqual(1, sync1.Result.tasks.Count);
                AreEqual(2, sync2.Result.tasks.Count);
            } {
                var readCustomers1 = customers.Read();
                var findPeter = readCustomers1.Find("customer-peter");
                var sync1 = store.SynchronizeAsync();
                
                store.SendMessage("Some message"); // add additional task to second SynchronizeAsync() to identify the result by task.Count
                var readCustomers2 = customers.Read();
                IsFalse(readCustomers1 == readCustomers2);
                var findPaul = readCustomers2.Find("customer-paul");
                var sync2 = store.SynchronizeAsync();
                
                // by using AsyncDatabase SynchronizeAsync() response handling is executed at WhenAll() instead synchronously in SynchronizeAsync()
                await Task.WhenAll(sync1, sync2);  // ------ sync point
                AreEqual(1, sync1.Result.tasks.Count);
                AreEqual(2, sync2.Result.tasks.Count);

                AreEqual("Peter", findPeter.Result.name);
                AreEqual("Paul",  findPaul.Result.name);
            }
        }
    }
}

// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Database.Remote;
using Friflo.Json.Flow.Database.Utils;
using Friflo.Json.Flow.Graph;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph.Happy
{
    public partial class TestStore
    {
        /// <summary>
        /// Test multiple calls of <see cref="EntityStore.Sync()"/> without await-ing each call individually.
        /// This enables "pipelining" of scheduling <see cref="EntityStore.Sync()"/> calls.
        /// <br></br>
        /// Motivation for "pipelining": <br></br>
        /// Sync() calls are executed in order but overall execution time is improved in scenarios with high latency to a
        /// <see cref="RemoteHostDatabase"/> because RTT is added only once instead of n times for n awaited Sync() calls. 
        /// </summary>
        [Test] public static void TestSyncConcurrency () {
            using (var _                = Pools.SharedPools) // for LeakTestsFixture
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
            {
                store.customers.Update(peter);
                AreEqual(1, store.Tasks.Count);
                var sync1 = store.Sync();
                AreEqual(0, store.Tasks.Count); // assert Tasks are cleared without awaiting Sync()
                
                store.SendMessage("Some message");
                store.customers.Update(paul);
                AreEqual(2, store.Tasks.Count);
                var sync2 = store.Sync();
                AreEqual(0, store.Tasks.Count); // assert Tasks are cleared without awaiting Sync()
                
                await Task.WhenAll(sync1, sync2);  // ------ sync point
                AreEqual(1, sync1.Result.tasks.Count);
                AreEqual(2, sync2.Result.tasks.Count);
            } {
                var readCustomers1 = store.customers.Read();
                var findPeter = readCustomers1.Find("customer-peter");
                var sync1 = store.Sync();
                
                store.SendMessage("Some message");
                var readCustomers2 = store.customers.Read();
                IsFalse(readCustomers1 == readCustomers2);
                var findPaul = readCustomers2.Find("customer-paul");
                var sync2 = store.Sync();
                
                await Task.WhenAll(sync1, sync2);  // ------ sync point
                AreEqual(1, sync1.Result.tasks.Count);
                AreEqual(2, sync2.Result.tasks.Count);

                AreEqual("Peter", findPeter.Result.name);
                AreEqual("Paul",  findPaul.Result.name);
            }
        }
    }
}

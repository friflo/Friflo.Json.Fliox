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
                    using (var database = new MemoryDatabase())
                    using (var createStore  = new PocStore(database, "createStore")) {
                        await SyncConcurrency(createStore);
                    }
                });
            }
        }
        
        private static async Task SyncConcurrency(PocStore store) {
            var customerPeter   = new Customer{ id = "customer-peter",  name = "Peter"};
            store.customers.Create(customerPeter);
            var sync1 = store.Sync();
            
            var customerPaul    = new Customer{ id = "customer-paul",   name = "Paul"};
            store.customers.Create(customerPaul);
            var sync2 = store.Sync();
            
            await Task.WhenAll(sync1, sync2);  // ------ sync point
            
            var readCustomers1 = store.customers.Read();
            var findPeter = readCustomers1.Find("customer-peter");
            sync1 = store.Sync();
            
            var readCustomers2 = store.customers.Read();
            var findPaul = readCustomers2.Find("customer-paul");
            sync2 = store.Sync();
            
            await Task.WhenAll(sync1, sync2);  // ------ sync point

            AreEqual("Peter", findPeter.Result.name);
            AreEqual("Paul", findPaul.Result.name);
        }
    }
}

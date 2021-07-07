// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Database.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph.Happy
{
    public partial class TestStore
    {
        /// <summary>
        /// Assert that the used <see cref="FileDatabase"/> support multi threaded access when reading and writing
        /// the same entity. By using <see cref="AsyncReaderWriterLock"/> a multi read / single write lock
        /// is used for each <see cref="FileContainer"/>. Without this lock it would result in:
        /// IOException: The process cannot access the file 'path' because it is being used by another process. 
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
            var createPeter = store.customers.Create(customerPeter);
            var sync1 = store.Sync();
            
            var customerPaul    = new Customer{ id = "customer-paul",   name = "Paul"};
            var createPaul = store.customers.Create(customerPaul);
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

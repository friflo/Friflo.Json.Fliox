// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Burst;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Database.Utils;
using Friflo.Json.Tests.Common.Utils;
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
        [Test] public static void TestConcurrentAccessSync () {
            SingleThreadSynchronizationContext.Run(async () => {
                using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/db")) {
                    await ConcurrentAccess(fileDatabase, 2, 2, 10);
                }
            });
        }
        
        public static async Task ConcurrentAccess(EntityDatabase database, int readerCount, int writerCount, int requestCount) {
            DebugUtils.StopLeakDetection();
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            {
                // prepare
                var             store       = new PocStore(database, "prepare");
                const string    id          = "concurrent-access";
                var             employee    = new Employee { id = id, firstName = "Concurrent accessed entity"};
                store.employees.Create(employee);
                await store.Sync();

                var readerStores = new List<PocStore>();
                var writerStores = new List<PocStore>();
                try {
                    for (int n = 0; n < readerCount; n++) {
                        readerStores.Add(new PocStore(database, $"reader-{n}"));
                    }
                    for (int n = 0; n < writerCount; n++) {
                        writerStores.Add(new PocStore(database, $"writer-{n}"));
                    }

                    // run readers and writers
                    var tasks = new List<Task>();

                    foreach (var readerStore in readerStores) {
                        tasks.Add(ReadLoop (readerStore, id, requestCount));
                    }
                    foreach (var writerStore in writerStores) {
                        tasks.Add(WriteLoop (writerStore, employee, requestCount));
                    }
                    var lastCount = 0;
                    var count = new Thread(() => {
                        while (true) {
                            try {
                                Thread.Sleep(1000);
                            } catch { break; }
                            lastCount = CountRequests(readerStores, writerStores, lastCount);
                        }
                    });
                    count.Start();

                    await Task.WhenAll(tasks);
                    CountRequests(readerStores, writerStores, lastCount);
                    count.Interrupt();
                }
                finally {
                    foreach (var readerStore in readerStores)
                        readerStore.Dispose();
                    foreach (var writerStore in writerStores) {
                        writerStore.Dispose();
                    }
                }
            }
        }

        private static Task ReadLoop (PocStore store, string id, int requestCount) {
            return Task.Run(async () => {
                for (int n= 0; n < requestCount; n++) {
                    var readEmployee = store.employees.Read();
                    readEmployee.Find(id);
                    await store.Sync();
                    AreEqual (1, readEmployee.Results.Count);
                }
            });
        }
        
        private static Task WriteLoop (PocStore store, Employee employee, int requestCount) {
            return Task.Run(async () => {
                for (int n= 0; n < requestCount; n++) {
                    store.employees.Create(employee);
                    await store.Sync();
                }
            });
        }
        
        private static int CountRequests (List<PocStore> readers, List<PocStore> writers, int lastCount) {
            int sum = 0;
            sum += readers.Sum(reader => reader.GetSyncCount());
            sum += writers.Sum(writer => writer.GetSyncCount());
            var requests = sum - lastCount;
            Console.WriteLine($"requests: {requests}, sum: {sum}");
            return sum;
        }
    }
}
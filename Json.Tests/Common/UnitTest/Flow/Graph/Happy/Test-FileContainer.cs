// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
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
        [Test] public static void TestConcurrentAccessSync      () { SingleThreadSynchronizationContext.Run(AssertConcurrentAccess);  }
        
        private static async Task AssertConcurrentAccess() {
            DebugUtils.StopLeakDetection();
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/db")) {
                const int readerCount = 2;
                const int writerCount = 2;
                
                var readerStores = new List<PocStore>();
                var writerStores = new List<PocStore>();
                try {
                    for (int n = 0; n < readerCount; n++) {
                        readerStores.Add(new PocStore(fileDatabase, $"reader-{n}"));
                    }
                    for (int n = 0; n < writerCount; n++) {
                        writerStores.Add(new PocStore(fileDatabase, $"writer-{n}"));
                    }

                    const string    id          = "concurrent-access";
                    var             employee    = new Employee { id = id, firstName = "Concurrent accessed entity"};
                    
                    var tasks = new List<Task>();

                    foreach (var readerStore in readerStores) {
                        tasks.Add(ReadLoop (readerStore, id));
                    }
                    foreach (var writerStore in writerStores) {
                        tasks.Add(WriteLoop (writerStore, employee));
                    }
                    await Task.WhenAll(tasks);
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

        private const int AccessCount = 10;

        private static Task ReadLoop (PocStore store, string id) {
            return Task.Run(async () => {
                for (int n= 0; n < AccessCount; n++) {
                    var readEmployee = store.employees.Read();
                    readEmployee.Find(id);
                    await store.Sync();
                    AreEqual (1, readEmployee.Results.Count);
                }
            });
        }
        
        private static Task WriteLoop (PocStore store, Employee employee) {
            return Task.Run(async () => {
                for (int n= 0; n < AccessCount; n++) {
                    store.employees.Create(employee);
                    await store.Sync();
                }
            });
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Tests.Provider.Client;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Provider.Env;

namespace Friflo.Json.Tests.Provider.Perf
{
    // ReSharper disable once InconsistentNaming
    public static class TestPerf
    {
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task Perf_Read_One(string db) {
            var client  = await GetClient(db);
            // warmup
            for (int n = 0; n < 1; n++) {
                client.testOps.Read().Find("a-1");
                await client.SyncTasks();
            }
            // measurement
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var count = 1; // 1_000;
            for (int n = 0; n < count; n++) {
                client.testOps.Read().Find("a-1");
                await client.SyncTasks();
            }
            var duration = stopWatch.ElapsedMilliseconds;
            Console.WriteLine($"Read. count: {count}, duration: {duration} ms");
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task Perf_QueryAll(string db) {
            var client  = await GetClient(db);
            // warmup
            for (int n = 0; n < 1; n++) {
                client.testOps.QueryAll();
                await client.SyncTasks();
            }
            // measurement
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var count = 1; // 1_000;

            for (int n = 0; n < count; n++) {
                client.testOps.QueryAll();
                await client.SyncTasks();
            }
            var duration = stopWatch.ElapsedMilliseconds;
            Console.WriteLine($"QueryAll. count: {count}, duration: {duration} ms");
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task Perf_MutationUpsert(string db) {
            var client      = await GetClient(db, false);
            var count       = 1; // 1_000_000; // memory_db & sqlite_db: 1_000_000 ~ 4 sec
            var entities    = new List<TestMutate>();
            for (int n = 0; n < count; n++) {
                var entity = new TestMutate { id = $"perf-{n}", val1 = n, val2 = n };
                entities.Add(entity);
            }
            var upsert = client.testMutate.UpsertRange(entities);
            await client.SyncTasks();
            IsTrue(upsert.Success);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task Perf_MutationDelete(string db) {
            var client      = await GetClient(db, false);
            var count       = 1; // 1_000_000; // memory_db & sqlite_db: 1_000_000 ~ 0.8 sec if already empty
            var ids         = new List<string>();
            for (int n = 0; n < count; n++) {
                ids.Add($"perf-{n}");
            }
            var upsert = client.testMutate.DeleteRange(ids);
            await client.SyncTasks();
            IsTrue(upsert.Success);
        }
    }
}
// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Tests.Common.Utils;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public static class TestSyncPerf
    {
        public static async Task ReadThroughput() {
            var database    = new MemoryDatabase("sync_perf_db");
            var hub         = new FlioxHub(database);
            
            var store       = new SimpleStore(hub) { ClientId = "sync_perf"};
            var entities    = new List<SimplyEntity>();

            for (int n = 0; n < 10; n++) {
                var entity = new SimplyEntity { id = n };
                entities.Add(entity);
            }
            store.entities.UpsertRange(entities);
            await store.SyncTasks();
            
            await ReadLoop(store, 50_000_000).ConfigureAwait(false);
        }

        private const int IntervalCount = 500_000; // 10, 500_000
        
        
        private static Task ReadLoop (SimpleStore store, int requestCount)
        {
            var perfContext = new PerfContext();

            for (int n= 0; n < requestCount; n++) {
                var readEntity = store.entities.Find(1);
                store.SyncTasksSynchronous();
                if (readEntity.Result == null) {
                    throw new TestException($"Expect entity not null");
                }
                var syncCount = store.GetSyncCount();
                if (syncCount % IntervalCount == 0) {
                    perfContext.Sample(IntervalCount); // 683.000 req/sec
                }
            }
            return Task.CompletedTask;
        }
    }
    
    internal class PerfContext
    {
        private             long        start;
        private readonly    Stopwatch   stopwatch;
        
        internal PerfContext() {
            stopwatch = new Stopwatch();
            stopwatch.Start();
        }
        
        internal void Sample(int count) {
            var now = stopwatch.ElapsedMilliseconds;
            var dif = now - start;
            var reqPerSec = dif == 0 ? -1 : 1000 * count / dif;
            Console.WriteLine($"---- {reqPerSec}");
            start = now;
        }
    }
}
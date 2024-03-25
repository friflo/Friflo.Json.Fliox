// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
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
            
            var store       = new SimpleStore(hub) { TrackEntities = false }; // { ClientId = "sync_perf" };  ClientId impact performance by AuthenticateNone
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
                    // req/sec: 1.500.000 - Mac Mini M2
                    //          1.030.000 - Intel(R) Core(TM) i7-4790K CPU 4.00 GHz
                    perfContext.Sample(IntervalCount);
                }
            }
            return Task.CompletedTask;
        }
    }
    
    internal class PerfContext
    {
        private             long            start;
        private             long            startMem;
        private readonly    Stopwatch       stopwatch;
        private readonly    StringBuilder   sb;
        private             int             countGen0;
        private             int             countGen1;
        private             int             countGen2;
        
        internal PerfContext() {
            stopwatch   = new Stopwatch();
            stopwatch.Start();
            startMem    = Mem.GetAllocatedBytes();
            sb          = new StringBuilder();
        }
        
        internal void Sample(int count) {
            var memDiff         = Mem.GetAllocationDiff(startMem);
            var now             = stopwatch.ElapsedMilliseconds;
            var dif             = now - start;
            var memPerSample    = memDiff / count;
            var reqPerSec       = dif == 0 ? -1 : 1000 * count / dif;
            
            int gen0 = GC.CollectionCount(0);
            int gen1 = GC.CollectionCount(1);
            int gen2 = GC.CollectionCount(2);

            sb.Clear();
            sb.Append("--- ");
            sb.AppendFormat("  {0,7}", reqPerSec);
            sb.AppendFormat("  {0,4}", memPerSample);
            sb.Append("  GC Gen:");
            sb.AppendFormat(" {0,3}", gen0 - countGen0);
            sb.AppendFormat(" {0,1}", gen1 - countGen1);
            sb.AppendFormat(" {0,1}", gen2 - countGen2);
            
            countGen0 = gen0;
            countGen1 = gen1;
            countGen2 = gen2;

            Console.WriteLine(sb);
            
            start               = stopwatch.ElapsedMilliseconds;
            startMem            = Mem.GetAllocatedBytes();
        }
    }
}
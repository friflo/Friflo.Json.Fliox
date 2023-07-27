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

    public static class TestPerf
    {
        internal const int  SeedCount   = 1; // 5000;
        internal const  int WarmupCount = 1; // 50_000;
        internal const  int ReadCount   = 1; // 1_000;
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task Perf_Read_One(string db) {
            await SeedPosts(db);
            
            var client  = await GetClient(db, false);

            // warmup
            for (int n = 0; n < WarmupCount; n++) {
                client.posts.Find(n % SeedCount);
                await client.SyncTasks();
            }

            // measurement
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var count = ReadCount;
            for (int n = 0; n < count; n++) {
                client.posts.Find(n % SeedCount);
                await client.SyncTasks();
            }
            var duration = stopWatch.Elapsed.TotalMilliseconds;
            Console.WriteLine($"Read. count: {count}, duration: {duration} ms");
        }
        
        // [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task Perf_Read_OneSynchronous(string db) {
            var supportSync = IsMemoryDB(db) || IsSQLServer(db);
            if (!supportSync) return;
            
            await SeedPosts(db);
            
            var client  = await GetClient(db, false);

            // warmup
            for (int n = 0; n < WarmupCount; n++) {
                client.posts.Read().Find(n % SeedCount);
                client.SyncTasksSynchronous();
            }

            // measurement
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var count = ReadCount;
            for (int n = 0; n < count; n++) {
                client.posts.Read().Find(n % SeedCount);
                client.SyncTasksSynchronous();
            }
            var duration = stopWatch.Elapsed.TotalMilliseconds;
            Console.WriteLine($"Read. count: {count}, duration: {duration} ms");
        }

        internal static async Task SeedPosts(string db) {
            var client  = await GetClient(db, false);
            
            var postCount = client.posts.CountAll();
            await client.SyncTasks();
            if (postCount.Result < SeedCount) {
                var chunkSize   = 1000;
                var text        = new string('x', 2000);
                var dateTime    = DateTime.Now;
                var posts       = new List<Post>();
                for (int n = 0; n < SeedCount; n++) {
                    var post = new Post { Id = n, Text = text, CreationDate = dateTime, LastChangeDate = dateTime };
                    posts.Add(post);
                    if (posts.Count % chunkSize == 0) {
                        client.posts.UpsertRange(posts);
                        await client.SyncTasks();
                        posts.Clear();
                    }
                }
                client.posts.UpsertRange(posts);
                await client.SyncTasks();
            }
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task Perf_QueryFilter(string db) {
            var client  = await GetClient(db);
            // warmup
            for (int n = 0; n < 1; n++) {
                client.testOps.Query(i => i.id == "a-1");
                await client.SyncTasks();
            }
            // measurement
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var count = 1; // 1_000;

            for (int n = 0; n < count; n++) {
                client.testOps.Query(i => i.id == "a-1");
                await client.SyncTasks();
            }
            var duration = stopWatch.ElapsedMilliseconds;
            Console.WriteLine($"QueryAll. count: {count}, duration: {duration} ms");
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
            // warmup
            var upsert = client.testMutate.UpsertRange(entities);
            await client.SyncTasks();
            IsTrue(upsert.Success);
            
            // measurement
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            upsert = client.testMutate.UpsertRange(entities);
            await client.SyncTasks();
            var duration = stopWatch.ElapsedMilliseconds;
            Console.WriteLine($"Upsert. count: {count}, duration: {duration} ms");
            
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
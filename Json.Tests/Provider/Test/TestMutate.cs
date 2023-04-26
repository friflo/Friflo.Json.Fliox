using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Tests.Provider.Client;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Provider.Env;

namespace Friflo.Json.Tests.Provider.Test
{
    public static class TestMutate
    {
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestUpsert(string db) {
            var client  = await GetClient(db);
            var entities = new List<TestWrite>();
            for (int n = 0; n < 3; n++) {
                var entity = new TestWrite {
                    id      = $"w-{n}",
                    val1    = n,
                    val2    = n,
                };
                entities.Add(entity);
            }
            var upsert = client.testWrite.UpsertRange(entities);
            await client.SyncTasks();
            IsTrue(upsert.Success);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestUpsertPerf(string db) {
            var client      = await GetClient(db);
            var count       = 1; // 1_000_000; // memory_db & sqlite_db: 1_000_000 ~ 4 sec
            var entities    = new List<TestWrite>();
            for (int n = 0; n < count; n++) {
                var entity = new TestWrite { id = $"perf-{n}", val1 = n, val2 = n };
                entities.Add(entity);
            }
            var upsert = client.testWrite.UpsertRange(entities);
            await client.SyncTasks();
            IsTrue(upsert.Success);
        }
    }
}
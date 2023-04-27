using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Tests.Provider.Client;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Provider.Env;

namespace Friflo.Json.Tests.Provider.Test
{
    // ReSharper disable once InconsistentNaming
    public static class Test_1_Mutation
    {
        // --- delete all
        [Order(1)]
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestMutation_1_DeleteAll(string db) {
            var client      = await GetClient(db);
            var upsert = client.testMutate.DeleteAll();
            await client.SyncTasks();
            IsTrue(upsert.Success);
        }
        
        [Order(2)]
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestMutation_2_DeleteAll_Check(string db) {
            var client      = await GetClient(db);
            var count       = client.testMutate.CountAll();
            await client.SyncTasks();
            AreEqual(0, count.Result);
        }
        
        // --- upsert
        [Order(3)]
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestMutation_3_Upsert(string db) {
            var client      = await GetClient(db);
            var entities    = new List<TestMutate>();
            for (int n = 0; n < 3; n++) {
                var entity = new TestMutate { id = $"w-{n}", val1 = n, val2    = n };
                entities.Add(entity);
            }
            var upsert = client.testMutate.UpsertRange(entities);
            await client.SyncTasks();
            IsTrue(upsert.Success);
        }
        
        [Order(4)]
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestMutation_4_Upsert_Check(string db) {
            var client      = await GetClient(db);
            var count       = client.testMutate.CountAll();
            await client.SyncTasks();
            AreEqual(3, count.Result);
        }
        
        // --- delete by id
        [Order(5)]
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestMutation_5_DeleteById(string db) {
            var client      = await GetClient(db);
            var upsert      = client.testMutate.Delete("w-1");
            await client.SyncTasks();
            IsTrue(upsert.Success);
        }
        
        [Order(6)]
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestMutation_6_DeleteById_Check(string db) {
            var client      = await GetClient(db);
            var find        = client.testMutate.Read().Find("w-1");
            await client.SyncTasks();
            IsNull(find.Result);
        }
    }
    
    public static class TestMutationPerf
    {
        [TestCase(memory_db, Category = memory_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestMutationUpsertPerf(string db) {
            var client      = await GetClient(db);
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
    }
}
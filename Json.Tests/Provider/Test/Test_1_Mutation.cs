using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Tests.Provider.Client;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Provider.Env;

namespace Friflo.Json.Tests.Provider.Test
{
    /// <summary>
    /// Used to implement and test <see cref="EntityContainer"/> methods required by subsequent test classes:
    /// <see cref="Test_2_Read"/>, <see cref="Test_3_1_QueryOps"/>, ...<br/>
    /// Methods to implement:
    /// <list type="bullet">
    ///   <item><see cref="EntityContainer.DeleteEntitiesAsync"/> to delete all container entities before executing mutations.</item>
    ///   <item><see cref="EntityContainer.AggregateEntitiesAsync"/> to count container entities after executing mutations.</item>
    ///   <item><see cref="EntityContainer.UpsertEntitiesAsync"/> to seed test records for subsequent test classes: <see cref="Test_2_Read"/>, ... .</item>
    /// </list>
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class Test_1_Mutation
    {
        // --- delete all
        // --- count all
        /// <summary>
        /// Requires basic implementation of:<br/>
        /// <see cref="EntityContainer.DeleteEntitiesAsync"/> reduced to delete all entities in a container.<br/> 
        /// <see cref="EntityContainer.AggregateEntitiesAsync"/> reduced to count all entities in a container.<br/>
        /// </summary>
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestMutation_1_DeleteAll(string db) {
            var client      = await GetClient(db, false);
            var deleteAll   = client.testMutate.DeleteAll();
            var countAll    = client.testMutate.CountAll();
            await client.SyncTasks();
            
            IsTrue(deleteAll.Success);
            AreEqual(0, countAll.Result);
        }
        
        // --- upsert
        /// <summary>
        /// Requires implementation of <see cref="EntityContainer.UpsertEntitiesAsync"/>
        /// </summary>
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestMutation_2_UpsertNew(string db) {
            var client      = await GetClient(db, false);
            var deleteAll   = client.testMutate.DeleteAll();
            var entities    = new List<TestMutate>();
            for (int n = 0; n < 3; n++) {
                var entity = new TestMutate { id = $"upsert-{n}", val1 = n, val2    = n };
                entities.Add(entity);
            }
            var upsert      = client.testMutate.UpsertRange(entities);
            var countAll    = client.testMutate.CountAll();
            await client.SyncTasks();
            
            IsTrue(upsert.Success);
            AreEqual(3, countAll.Result);
            IsTrue(deleteAll.Success);
        }
        
        /// <summary>
        /// Requires implementation of <see cref="EntityContainer.UpsertEntitiesAsync"/>
        /// </summary>
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestMutation_3_UpsertExists(string db) {
            var client      = await GetClient(db, false);
            var deleteAll   = client.testMutate.DeleteAll();
            var entities    = new List<TestMutate>();
            for (int n = 0; n < 3; n++) {
                var entity = new TestMutate { id = $"upsert-{n}", val1 = n, val2    = n };
                entities.Add(entity);
            }
            var upsert      = client.testMutate.UpsertRange(entities);
            await client.SyncTasks();
            IsTrue(upsert.Success);
                
            var upsert2     = client.testMutate.UpsertRange(entities);
            var countAll    = client.testMutate.CountAll();
            await client.SyncTasks();
            
            IsTrue(upsert2.Success);
            AreEqual(3, countAll.Result);
            IsTrue(deleteAll.Success);
        }
        
        /// <summary>
        /// Requires implementation of <see cref="EntityContainer.UpsertEntitiesAsync"/>
        /// </summary>
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestMutation_3_UpsertEscape(string db) {
            var client      = await GetClient(db, false);
            var deleteAll   = client.testMutate.DeleteAll();
            var entities    = new List<TestMutate> {
                new TestMutate { id = $"upsert-quote",  str = "quote-\'" },
                new TestMutate { id = $"upsert-escape", str = "escape-\\-\b-\f-\n-\r-\t-" }
            };
            var upsert      = client.testMutate.UpsertRange(entities);
            var countAll    = client.testMutate.CountAll();
            await client.SyncTasks();
            
            IsTrue(upsert.Success);
            AreEqual(2, countAll.Result);
            IsTrue(deleteAll.Success);
        }
        
        // --- create
        /// <summary>
        /// Requires implementation of <see cref="EntityContainer.CreateEntitiesAsync"/>
        /// </summary>
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestMutation_4_CreateNew(string db) {
            var client      = await GetClient(db, false);
            var deleteAll   = client.testMutate.DeleteAll();
            var entities    = new List<TestMutate>();
            for (int n = 0; n < 3; n++) {
                var entity = new TestMutate { id = $"create-{n}", val1 = n, val2    = n };
                entities.Add(entity);
            }
            var create      = client.testMutate.CreateRange(entities);
            var countAll    = client.testMutate.CountAll();
            await client.SyncTasks();
            
            IsTrue(create.Success);
            AreEqual(3, countAll.Result);
            IsTrue(deleteAll.Success);
        }
        
        /// <summary>
        /// Error handling in of <see cref="EntityContainer.CreateEntitiesAsync"/>
        /// </summary>
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestMutation_5_CreateExists(string db) {
            var client      = await GetClient(db, false);
            var entity      = new TestMutate { id = "create-new", val1 = 10, val2 = 10};
            {
                var deleteAll   = client.testMutate.DeleteAll();
                var create1     = client.testMutate.Create(entity);
                var countAll    = client.testMutate.CountAll();
                await client.SyncTasks();
                
                IsTrue (create1.Success);
                IsTrue(deleteAll.Success);
                AreEqual(1, countAll.Result);
            }
            {
                var create2     = client.testMutate.Create(entity);
                var countAll    = client.testMutate.CountAll();
                await client.TrySyncTasks();
                
                IsFalse(create2.Success);
                AreEqual(1, countAll.Result);
            }
        }
        
        // --- delete by id
        /// <summary>
        /// Requires implementation of <see cref="EntityContainer.DeleteEntitiesAsync"/>
        /// </summary>
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestMutation_6_DeleteById(string db) {
            var client      = await GetClient(db, false);
            var deleteAll   = client.testMutate.DeleteAll();
            var upsert      = client.testMutate.Upsert(new TestMutate { id = "delete-1", val1 = 1, val2 = 2} );
            var delete      = client.testMutate.Delete("delete-1");
            var countAll    = client.testMutate.CountAll();
            await client.SyncTasks();
            
            IsTrue(delete.Success);
            AreEqual(0, countAll.Result);
            IsTrue(deleteAll.Success);
            IsTrue(upsert.Success);
        }
    }
}
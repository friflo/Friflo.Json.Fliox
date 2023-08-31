using System.Threading.Tasks;
using Friflo.Json.Tests.Provider.Client;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Provider.Env;

namespace Friflo.Json.Tests.Provider.Test
{
    // ReSharper disable once InconsistentNaming
    public static class Test_7_MutateRead
    {
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task Test_7_FindDeleted(string db)
        {
            var client  = await GetClient(db, trackEntities: true);
            
            var testDelete = new TestMutate { id = "test-delete-1" };
            client.testMutate.DeleteAll();
            client.testMutate.Create(testDelete);
            await client.SyncTasksEnv();
            
            client.testMutate.Delete(testDelete);
            await client.SyncTasksEnv();
            IsFalse(client.testMutate.Local.ContainsKey(testDelete.id));
            
            var find = client.testMutate.Find(testDelete.id);
            await client.SyncTasksEnv();
            IsNull(find.Result);
            IsNull(client.testMutate.Local[testDelete.id]);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task Test_7_ReadDeleted(string db)
        {
            var client1  = await GetClient(db);
            var testDelete = new TestMutate { id = "test-delete-2" };
            client1.testMutate.DeleteAll();
            client1.testMutate.Create(testDelete);

            await client1.SyncTasksEnv();
            
            var client2  = await GetClient(db, trackEntities: true);
            client2.testMutate.Find(testDelete.id);
            await client2.SyncTasksEnv();
            NotNull(client2.testMutate.Local[testDelete.id]);
            
            client1.testMutate.Delete(testDelete);
            await client1.SyncTasksEnv();
            
            var find = client2.testMutate.Find(testDelete.id);
            await client2.SyncTasksEnv();
            IsNull(find.Result);
            // Ensure Local entry is null when deleted by another client
            IsNull(client2.testMutate.Local[testDelete.id]);
        }
    }
}
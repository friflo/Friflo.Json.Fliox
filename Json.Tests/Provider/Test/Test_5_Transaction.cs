using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Tests.Provider.Client;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Provider.Env;

// ReSharper disable CompareOfFloatsByEqualityOperator
namespace Friflo.Json.Tests.Provider.Test
{
    // ReSharper disable once InconsistentNaming
    public static class Test_5_Transaction
    {
        
        private static bool SupportTransaction(string db) => IsSQLite(db) || IsMySQL(db) || IsMariaDB(db) || IsPostgres(db) || IsSQLServer(db);

        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestTransaction_Commit_Implicit(string db) {
            var client  = await GetClient(db);
            client.testMutate.DeleteAll();
            await client.SyncTasksEnv();
            
            var begin = client.std.TransactionBegin();
            client.testMutate.Create(new TestMutate { id = "op-1", val1 = 1, val2 = 1 });
            client.testMutate.Create(new TestMutate { id = "op-2", val1 = 2, val2 = 2 });
            await client.SyncTasksEnv();
            AreEqual(TransactionCommand.Commit, begin.Result.executed);
            
            var count = client.testMutate.CountAll();
            await client.SyncTasksEnv();
            AreEqual(2, count.Result);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestTransaction_Commit_Explicit(string db) {
            var client  = await GetClient(db);
            client.testMutate.DeleteAll();
            await client.SyncTasksEnv();
            
            var begin = client.std.TransactionBegin();
            client.testMutate.Create(new TestMutate { id = "op-1", val1 = 1, val2 = 1 });
            client.testMutate.Create(new TestMutate { id = "op-2", val1 = 2, val2 = 2 });
            var end = client.std.TransactionCommit();
            await client.SyncTasksEnv();
            AreEqual(TransactionCommand.Commit, begin.Result.executed);
            AreEqual(TransactionCommand.Commit, end.Result.executed);
            
            var count = client.testMutate.CountAll();
            await client.SyncTasksEnv();
            AreEqual(2, count.Result);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestTransaction_Rollback(string db) {
            var client  = await GetClient(db);
            client.testMutate.DeleteAll();
            await client.SyncTasksEnv();
            
            var begin = client.std.TransactionBegin();
            client.testMutate.Create(new TestMutate { id = "op-1", val1 = 1, val2 = 1 });
            client.testMutate.Create(new TestMutate { id = "op-2", val1 = 2, val2 = 2 });
            var end = client.std.TransactionRollback();
            await client.SyncTasksEnv();
            AreEqual(TransactionCommand.Rollback, begin.Result.executed);
            AreEqual(TransactionCommand.Rollback, end.Result.executed);
            
            var count = client.testMutate.CountAll();
            await client.SyncTasksEnv();
            if (SupportTransaction(db)) {
                AreEqual(0, count.Result);
                return;
            }
            AreEqual(2, count.Result);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestTransaction_Commit_Rollback(string db) {
            var client  = await GetClient(db);
            client.testMutate.DeleteAll();
            client.testMutate.Create(new TestMutate { id = "op-1", val1 = 1, val2 = 1 });
            await client.SyncTasksEnv();
            
            client      = await GetClient(db);
            var begin   = client.std.TransactionBegin();
            var create  = client.testMutate.Create(new TestMutate { id = "op-1", val1 = 2, val2 = 2 });
            await client.TrySyncTasksEnv();

            IsFalse(create.Success);
            AreEqual(TransactionCommand.Rollback, begin.Result.executed);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestTransaction_Commit_Error(string db) {
            var client  = await GetClient(db);
            
            var end = client.std.TransactionCommit();
            await client.TrySyncTasksEnv();
            
            AreEqual("CommandError ~ Missing begin transaction", end.Error.Message);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestTransaction_Rollback_Error(string db) {
            var client  = await GetClient(db);
            
            var end = client.std.TransactionRollback();
            await client.TrySyncTasksEnv();
            
            AreEqual("CommandError ~ Missing begin transaction", end.Error.Message);
        }
        
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestTransaction_Commit_Nested_Error(string db) {
            var client  = await GetClient(db);
            
            var begin1 = client.std.TransactionBegin();
            var begin2 = client.std.TransactionBegin();
            await client.TrySyncTasksEnv();
            
            IsTrue(begin1.Success);
            AreEqual("CommandError ~ Transaction already started", begin2.Error.Message);
        }
    }
}

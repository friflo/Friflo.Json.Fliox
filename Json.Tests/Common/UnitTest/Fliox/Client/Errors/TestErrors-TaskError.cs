// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Client;
using Friflo.Json.Fliox.DB.Protocol;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Errors
{
    public partial class TestErrors
    {
        // ------ Test each topic individual - using a FileDatabase
        [Test] public async Task TestTaskError      () { await Test(async (store, database) => await AssertTaskError        (store, database)); }
        
        private static async Task AssertTaskError(PocStore store, TestDatabase testDatabase) {
            testDatabase.ClearErrors();
            TestContainer testCustomers = testDatabase.GetTestContainer(nameof(PocStore.customers));
            
            const string createTaskError        = "create-task-error";
            const string upsertTaskError        = "upsert-task-error";
            const string deleteTaskError        = "delete-task-error";
            const string readTaskError          = "read-task-error";
            
            testCustomers.writeTaskErrors.Add(createTaskError,  () => new CommandError("simulated create task error"));
            testCustomers.writeTaskErrors.Add(upsertTaskError,  () => new CommandError("simulated upsert task error"));
            testCustomers.writeTaskErrors.Add(deleteTaskError,  () => new CommandError("simulated delete task error"));
            testCustomers.readTaskErrors. Add(readTaskError,    () => new CommandError("simulated read task error"));
            // Query(c => c.id == "query-task-error")
            testCustomers.queryErrors.Add(".id == 'query-task-error'", () => new QueryEntitiesResult {Error = new CommandError("simulated query error")});
        
            var customers = store.customers;

            var readCustomers   = customers.Read()                                      .TaskName("readCustomers");
            var customerRead    = readCustomers.Find(readTaskError)                     .TaskName("customerRead");
            var customerQuery   = customers.Query(c => c.id == "query-task-error")      .TaskName("customerQuery");
            
            var createError     = customers.Create(new Customer{id = createTaskError})  .TaskName("createError");
            var upsertError     = customers.Upsert(new Customer{id = upsertTaskError})  .TaskName("upsertError");
            var deleteError     = customers.Delete(new Customer{id = deleteTaskError})  .TaskName("deleteError");
            
            AreEqual(5, store.Tasks.Count);
            var sync = await store.TrySync(); // -------- Sync --------
            
            AreEqual("tasks: 5, failed: 5", sync.ToString());
            AreEqual("tasks: 5, failed: 5", sync.ToString());
            AreEqual(@"Sync() failed with task errors. Count: 5
|- customerRead # DatabaseError ~ simulated read task error
|- customerQuery # DatabaseError ~ simulated query error
|- createError # DatabaseError ~ simulated create task error
|- upsertError # DatabaseError ~ simulated upsert task error
|- deleteError # DatabaseError ~ simulated delete task error", sync.Message);

            TaskResultException te;
            te = Throws<TaskResultException>(() => { var _ = customerRead.Result; });
            AreEqual("DatabaseError ~ simulated read task error", te.Message);
            AreEqual(TaskErrorType.DatabaseError, customerRead.Error.type);
            
            te = Throws<TaskResultException>(() => { var _ = customerQuery.Results; });
            AreEqual("DatabaseError ~ simulated query error", te.Message);
            AreEqual(TaskErrorType.DatabaseError, customerQuery.Error.type);
            
            IsFalse(createError.Success);
            AreEqual("DatabaseError ~ simulated create task error", createError.Error.Message);
            
            IsFalse(upsertError.Success);
            AreEqual("DatabaseError ~ simulated upsert task error", upsertError.Error.Message);
            
            IsFalse(deleteError.Success);
            AreEqual("DatabaseError ~ simulated delete task error", deleteError.Error.Message);
        }
    }
}
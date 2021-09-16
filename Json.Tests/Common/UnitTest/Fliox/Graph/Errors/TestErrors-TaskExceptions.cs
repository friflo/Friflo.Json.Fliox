// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Graph;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Graph.Errors
{
    public partial class TestErrors
    {
        // ------ Test each topic individual - using a FileDatabase
        [Test] public async Task TestTaskExceptions () { await Test(async (store, database) => await AssertTaskExceptions   (store, database)); }

        private static async Task AssertTaskExceptions(PocStore store, TestDatabase testDatabase) {
            testDatabase.ClearErrors();
            TestContainer testCustomers = testDatabase.GetTestContainer(nameof(PocStore.customers));
            const string readTaskException      = "read-task-exception"; // throws an exception also for a Query
            const string createTaskException    = "create-task-exception";
            const string upsertTaskException    = "upsert-task-exception";
            const string deleteTaskException    = "delete-task-exception";
            
            testCustomers.readTaskErrors. Add(readTaskException,    () => throw new SimulationException("simulated read task exception"));
            testCustomers.writeTaskErrors.Add(createTaskException,  () => throw new SimulationException("simulated create task exception"));
            testCustomers.writeTaskErrors.Add(upsertTaskException,  () => throw new SimulationException("simulated upsert task exception"));
            testCustomers.writeTaskErrors.Add(deleteTaskException,  () => throw new SimulationException("simulated delete task exception"));
            // Query(c => c.id == "query-task-exception")
            testCustomers.queryErrors.Add(".id == 'query-task-exception'", () => throw new SimulationException("simulated query exception"));

            
            var customers = store.customers;

            var readCustomers   = customers.Read()                                          .TaskName("readCustomers");
            var customerRead    = readCustomers.Find(readTaskException)                     .TaskName("customerRead");
            var customerQuery   = customers.Query(c => c.id == "query-task-exception")      .TaskName("customerQuery");

            var createError     = customers.Create(new Customer{id = createTaskException})  .TaskName("createError");
            var upsertError     = customers.Upsert(new Customer{id = upsertTaskException})  .TaskName("upsertError");
            var deleteError     = customers.Delete(new Customer{id = deleteTaskException})  .TaskName("deleteError");
            
            AreEqual("CreateTask<Customer> (#keys: 1)", createError.Details);
            AreEqual("UpsertTask<Customer> (#keys: 1)", upsertError.Details);
            AreEqual("DeleteTask<Customer> (#keys: 1)", deleteError.Details);
            
            Exception e;
            e = Throws<TaskNotSyncedException>(() => { var _ = createError.Success; });
            AreEqual("SyncTask.Success requires Sync(). createError", e.Message);
            e = Throws<TaskNotSyncedException>(() => { var _ = createError.Error; });
            AreEqual("SyncTask.Error requires Sync(). createError", e.Message);

            AreEqual(5, store.Tasks.Count);
            var sync = await store.TrySync(); // -------- Sync --------
            
            AreEqual("tasks: 5, failed: 5", sync.ToString());
            AreEqual(@"Sync() failed with task errors. Count: 5
|- customerRead # UnhandledException ~ SimulationException: simulated read task exception
|- customerQuery # UnhandledException ~ SimulationException: simulated query exception
|- createError # UnhandledException ~ SimulationException: simulated create task exception
|- upsertError # UnhandledException ~ SimulationException: simulated upsert task exception
|- deleteError # UnhandledException ~ SimulationException: simulated delete task exception", sync.Message);
            
            TaskResultException te;
            te = Throws<TaskResultException>(() => { var _ = customerRead.Result; });
            AreEqual("UnhandledException ~ SimulationException: simulated read task exception", te.Message); // No stacktrace by intention
            AreEqual("SimulationException: simulated read task exception", te.error.taskMessage);

            te = Throws<TaskResultException>(() => { var _ = customerQuery.Results; });
            AreEqual("UnhandledException ~ SimulationException: simulated query exception", te.error.Message);

            IsFalse(createError.Success);
            AreEqual("UnhandledException ~ SimulationException: simulated create task exception", createError.Error.Message);
            
            IsFalse(upsertError.Success);
            AreEqual("UnhandledException ~ SimulationException: simulated upsert task exception", upsertError.Error.Message);
            
            IsFalse(deleteError.Success);
            AreEqual("UnhandledException ~ SimulationException: simulated delete task exception", deleteError.Error.Message);
        }
    }
}
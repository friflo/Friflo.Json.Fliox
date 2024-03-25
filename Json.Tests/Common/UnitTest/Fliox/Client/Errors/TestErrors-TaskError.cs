// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Hubs;
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
        
        private static async Task AssertTaskError(PocStore store, TestDatabaseHub testHub) {
            testHub.ClearErrors();
            TestContainer testCustomers = testHub.GetTestContainer(nameof(PocStore.customers));
            
            const string createTaskError        = "create-task-error";
            const string upsertTaskError        = "upsert-task-error";
            const string deleteTaskError        = "delete-task-error";
            const string readTaskError          = "read-task-error";
            
            testCustomers.writeTaskErrors.Add(createTaskError,  () => new TaskExecuteError("simulated create task error"));
            testCustomers.writeTaskErrors.Add(upsertTaskError,  () => new TaskExecuteError("simulated upsert task error"));
            testCustomers.writeTaskErrors.Add(deleteTaskError,  () => new TaskExecuteError("simulated delete task error"));
            testCustomers.readTaskErrors. Add(readTaskError,    () => new TaskExecuteError("simulated read task error"));
            // Query(c => c.id == "query-task-error")
            testCustomers.queryErrors.Add("o => o.id == 'query-task-error'", () => new QueryEntitiesResult {Error = new TaskExecuteError("simulated query error")});
        
            var customers = store.customers;

            var readCustomers   = customers.Read()                                      .TaskName("readCustomers");
            var customerRead    = readCustomers.Find(readTaskError); //                 .TaskName("customerRead");
            var customerQuery   = customers.Query(o => o.id == "query-task-error")      .TaskName("customerQuery");
            
            var createError     = customers.Create(new Customer{id = createTaskError})  .TaskName("createError");
            var upsertError     = customers.Upsert(new Customer{id = upsertTaskError})  .TaskName("upsertError");
            var deleteError     = customers.Delete(new Customer{id = deleteTaskError})  .TaskName("deleteError");
            
            AreEqual(5, store.Tasks.Count);
            var sync = await store.TrySyncTasks(); // ----------------
            
            AreEqual("tasks: 5, failed: 5", sync.ToString());
            AreEqual(@"SyncTasks() failed with task errors. Count: 5
|- readCustomers # DatabaseError ~ simulated read task error
|- customerQuery # DatabaseError ~ simulated query error
|- createError # DatabaseError ~ simulated create task error
|- upsertError # DatabaseError ~ simulated upsert task error
|- deleteError # DatabaseError ~ simulated delete task error", sync.Message);

            TaskResultException te;
            te = Throws<TaskResultException>(() => { var _ = customerRead.Result; });
            AreEqual("DatabaseError ~ simulated read task error", te.Message);
            AreEqual(TaskErrorType.DatabaseError, customerRead.Error.type);
            
            te = Throws<TaskResultException>(() => { var _ = customerQuery.Result; });
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
// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Hubs;
using static Friflo.Json.Tests.Common.Utils.AssertUtils;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Errors
{
    public partial class TestErrors
    {
        // ------ Test each topic individual - using a FileDatabase
        [Test] public async Task TestPatchError() { await Test(async (store, database) => await AssertPatchError  (store, database)); }

        private static async Task AssertPatchError(PocStore store, TestDatabaseHub testHub) {
            testHub.ClearErrors();
            TestContainer testCustomers = testHub.GetTestContainer(nameof(PocStore.customers));
            var customers = store.customers;
            
            // --- prepare precondition for log changes
            const string writeError = "log-patch-entity-write-error";
            const string readError  = "log-patch-entity-read-error";
            var readCustomers = customers.Read();
            var customerWriteError  = readCustomers.Find(writeError);
            var customerReadError   = readCustomers.Find(readError);

            await store.SyncTasks();

            // --- setup simulation errors after preconditions are established
            {
                testCustomers.writeEntityErrors.Add(writeError, new SimWriteError());
                testCustomers.readEntityErrors. Add(readError,  new SimReadError());

                customerWriteError.Result.name  = "<change write 1>";
                customerReadError.Result.name   = "<change read 1>";
                var customerPatches = customers.DetectPatches();

                var sync = await store.TrySyncTasks(); // ----------------
                AreEqual("tasks: 1, failed: 1", sync.ToString());

                IsFalse(customerPatches.Success);
                AreEqual(TaskErrorType.EntityErrors, customerPatches.Error.type);
                AreEqual(@"EntityErrors ~ count: 2
| ReadError: customers [log-patch-entity-read-error], simulated read entity error
| WriteError: customers [log-patch-entity-write-error], simulated write entity error", customerPatches.Error.Message);
            } {
                testCustomers.readTaskErrors [readError]    = () => throw new SimulationException("simulated read task exception");
                customerReadError.Result.name   = "<change read 2>";
                var customerPatches = customers.DetectPatches();

                AreEqual(1, store.Tasks.Count);
                var sync = await store.TrySyncTasks(); // ----------------
                
                AreEqual("tasks: 1, failed: 1", sync.ToString());

                AreEqual(TaskErrorType.UnhandledException, customerPatches.Error.type);
                AreEqualTrimStack(@"UnhandledException ~ SimulationException: simulated read task exception", customerPatches.Error.Message);
            } {
                testCustomers.readTaskErrors[readError]    = () => new TaskExecuteError("simulated read task error");
                customerReadError.Result.name   = "<change read 3>";
                var customerPatches = customers.DetectPatches();

                var sync = await store.TrySyncTasks(); // ----------------
                AreEqual("tasks: 1, failed: 1", sync.ToString());

                AreEqual(TaskErrorType.DatabaseError, customerPatches.Error.type);
                AreEqual(@"DatabaseError ~ simulated read task error", customerPatches.Error.Message);
            } {
                testCustomers.readTaskErrors.Remove(readError);
                testCustomers.writeTaskErrors [writeError]    = () => throw new SimulationException("simulated write task exception");
                customerWriteError.Result.name   = "<change write 3>";
                customerReadError.Result.name   = "<change read 1>"; // restore original value
                var customerPatches = customers.DetectPatches();

                var sync = await store.TrySyncTasks(); // ----------------
                AreEqual("tasks: 1, failed: 1", sync.ToString());

                AreEqual(TaskErrorType.UnhandledException, customerPatches.Error.type);
                AreEqualTrimStack(@"UnhandledException ~ SimulationException: simulated write task exception", customerPatches.Error.Message);
            } {
                testCustomers.writeTaskErrors [writeError]    = () => new TaskExecuteError("simulated write task error");
                customerWriteError.Result.name   = "<change write 4>";
                var customerPatches = customers.DetectPatches();

                var sync = await store.TrySyncTasks(); // ----------------
                AreEqual("tasks: 1, failed: 1", sync.ToString());

                AreEqual(TaskErrorType.DatabaseError, customerPatches.Error.type);
                AreEqual(@"DatabaseError ~ simulated write task error", customerPatches.Error.Message);
                
                customerWriteError.Result.name   = "<change write 1>";  // restore original value
            }
        }
    }
}
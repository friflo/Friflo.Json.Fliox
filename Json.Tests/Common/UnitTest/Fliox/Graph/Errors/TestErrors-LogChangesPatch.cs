// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using Friflo.Json.Fliox.Graph;
using Friflo.Json.Fliox.Sync;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Graph.Errors
{
    public partial class TestErrors
    {
        // ------ Test each topic individual - using a FileDatabase
        [Test] public async Task TestLogChangesPatch() { await Test(async (store, database) => await AssertLogChangesPatch  (store, database)); }

        private static async Task AssertLogChangesPatch(PocStore store, TestDatabase testDatabase) {
            testDatabase.ClearErrors();
            TestContainer testCustomers = testDatabase.GetTestContainer<Customer>();
            var customers = store.customers;
            
            // --- prepare precondition for log changes
            const string writeError = "log-patch-entity-write-error";
            const string readError  = "log-patch-entity-read-error";
            var readCustomers = customers.Read();
            var customerWriteError  = readCustomers.Find(writeError);
            var customerReadError   = readCustomers.Find(readError);

            await store.Sync();

            // --- setup simulation errors after preconditions are established
            {
                testCustomers.writeEntityErrors.Add(writeError, () => testCustomers.WriteError(writeError));
                testCustomers.readEntityErrors. Add(readError,  (value) => value.SetError(testCustomers.ReadError(readError)));

                customerWriteError.Result.name  = "<change write 1>";
                customerReadError.Result.name   = "<change read 1>";
                var logChanges = customers.LogSetChanges();

                var sync = await store.TrySync(); // -------- Sync --------
                AreEqual("tasks: 1, failed: 1", sync.ToString());

                IsFalse(logChanges.Success);
                AreEqual(TaskErrorType.EntityErrors, logChanges.Error.type);
                AreEqual(@"EntityErrors ~ count: 2
| ReadError: Customer 'log-patch-entity-read-error', simulated read entity error
| WriteError: Customer 'log-patch-entity-write-error', simulated write entity error", logChanges.Error.Message);
            } {
                testCustomers.readTaskErrors [readError]    = () => throw new SimulationException("simulated read task exception");
                customerReadError.Result.name   = "<change read 2>";
                var logChanges = customers.LogSetChanges();

                AreEqual(1, store.Tasks.Count);
                var sync = await store.TrySync(); // -------- Sync --------
                
                AreEqual("tasks: 1, failed: 1", sync.ToString());

                AreEqual(TaskErrorType.EntityErrors, logChanges.Error.type);
                AreEqual(@"EntityErrors ~ count: 1
| PatchError: Customer 'log-patch-entity-read-error', UnhandledException - SimulationException: simulated read task exception", logChanges.Error.Message);
            } {
                testCustomers.readTaskErrors[readError]    = () => new CommandError{message = "simulated read task error"};
                customerReadError.Result.name   = "<change read 3>";
                var logChanges = customers.LogSetChanges();

                var sync = await store.TrySync(); // -------- Sync --------
                AreEqual("tasks: 1, failed: 1", sync.ToString());

                AreEqual(TaskErrorType.EntityErrors, logChanges.Error.type);
                AreEqual(@"EntityErrors ~ count: 1
| PatchError: Customer 'log-patch-entity-read-error', DatabaseError - simulated read task error", logChanges.Error.Message);
            } {
                testCustomers.readTaskErrors.Remove(readError);
                testCustomers.writeTaskErrors [writeError]    = () => throw new SimulationException("simulated write task exception");
                customerWriteError.Result.name   = "<change write 3>";
                customerReadError.Result.name   = "<change read 1>"; // restore original value
                var logChanges = customers.LogSetChanges();

                var sync = await store.TrySync(); // -------- Sync --------
                AreEqual("tasks: 1, failed: 1", sync.ToString());

                AreEqual(TaskErrorType.EntityErrors, logChanges.Error.type);
                AreEqual(@"EntityErrors ~ count: 1
| PatchError: Customer 'log-patch-entity-write-error', UnhandledException - SimulationException: simulated write task exception", logChanges.Error.Message);
            } {
                testCustomers.writeTaskErrors [writeError]    = () => new CommandError {message = "simulated write task error"} ;
                customerWriteError.Result.name   = "<change write 4>";
                var logChanges = customers.LogSetChanges();

                var sync = await store.TrySync(); // -------- Sync --------
                AreEqual("tasks: 1, failed: 1", sync.ToString());

                AreEqual(TaskErrorType.EntityErrors, logChanges.Error.type);
                AreEqual(@"EntityErrors ~ count: 1
| PatchError: Customer 'log-patch-entity-write-error', DatabaseError - simulated write task error", logChanges.Error.Message);
                
                customerWriteError.Result.name   = "<change write 1>";  // restore original value
            }
        }
    }
}
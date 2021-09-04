// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Graph;
using Friflo.Json.Fliox.DB.Sync;
using Friflo.Json.Fliox.Mapper;
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
        [Test] public async Task TestEntityPatch    () { await Test(async (store, database) => await AssertEntityPatch      (store, database)); }

        private static async Task AssertEntityPatch(PocStore store, TestDatabase testDatabase) {
            testDatabase.ClearErrors();
            TestContainer testCustomers = testDatabase.GetTestContainer<Customer>();
            const string patchReadEntityError   = "patch-read-entity-error";
            const string patchWriteEntityError  = "patch-write-entity-error";
            const string patchTaskException     = "patch-task-exception";
            const string patchTaskError         = "patch-task-error";
            const string readTaskError          = "read-task-error";
            const string readTaskException      = "read-task-exception";

            testCustomers.writeEntityErrors.Add(patchWriteEntityError,  () => testCustomers.WriteError(patchWriteEntityError));
            testCustomers.readEntityErrors. Add(patchReadEntityError,   (value) => value.SetError(testCustomers.ReadError(patchReadEntityError)));
            testCustomers.writeTaskErrors.  Add(patchTaskException,     () => throw new SimulationException("simulated patch task exception"));
            testCustomers.writeTaskErrors.  Add(patchTaskError,         () => new CommandError {message = "simulated patch task error"});
            testCustomers.readTaskErrors.   Add(readTaskError,          () => new CommandError{message = "simulated read task error"});
            testCustomers.readTaskErrors.   Add(readTaskException,      () => throw new SimulationException("simulated read task exception"));

            var customers = store.customers;
            const string unknownId = "unknown-id";
            
            var patchNotFound       = customers.Patch (new Customer{id = unknownId})            .TaskName("patchNotFound");
            
            var patchReadError      = customers.Patch (new Customer{id = patchReadEntityError}) .TaskName("patchReadError");
            
            var patchWriteError     = customers.Patch (new Customer{id = patchWriteEntityError}).TaskName("patchWriteError");
            
            AreEqual(3, store.Tasks.Count);
            var sync = await store.TrySync(); // -------- Sync --------
            
            AreEqual("tasks: 3, failed: 3", sync.ToString());
            AreEqual(@"Sync() failed with task errors. Count: 3
|- patchNotFound # EntityErrors ~ count: 1
|   PatchError: Customer 'unknown-id', patch target not found
|- patchReadError # EntityErrors ~ count: 1
|   ReadError: Customer 'patch-read-entity-error', simulated read entity error
|- patchWriteError # EntityErrors ~ count: 1
|   WriteError: Customer 'patch-write-entity-error', simulated write entity error", sync.Message);
            
            {
                IsFalse(patchNotFound.Success);
                AreEqual(TaskErrorType.EntityErrors, patchNotFound.Error.type);
                var patchErrors = patchNotFound.Error.entityErrors;
                AreEqual("PatchError: Customer 'unknown-id', patch target not found", patchErrors[new JsonKey(unknownId)].ToString());
            } {
                IsFalse(patchReadError.Success);
                AreEqual(TaskErrorType.EntityErrors, patchReadError.Error.type);
                var patchErrors = patchReadError.Error.entityErrors;
                AreEqual("ReadError: Customer 'patch-read-entity-error', simulated read entity error", patchErrors[new JsonKey(patchReadEntityError)].ToString());
            } {
                IsFalse(patchWriteError.Success);
                AreEqual(TaskErrorType.EntityErrors, patchWriteError.Error.type);
                var patchErrors = patchWriteError.Error.entityErrors;
                AreEqual("WriteError: Customer 'patch-write-entity-error', simulated write entity error", patchErrors[new JsonKey(patchWriteEntityError)].ToString());
            }
            
            // --- test read task error
            {
                var patchTaskReadError = customers.Patch(new Customer {id = readTaskError});

                sync = await store.TrySync(); // -------- Sync --------
                AreEqual("tasks: 1, failed: 1", sync.ToString());

                IsFalse(patchTaskReadError.Success);
                AreEqual("DatabaseError ~ simulated read task error", patchTaskReadError.Error.Message);
            }

            // --- test read task exception
            {
                var patchTaskReadException = customers.Patch(new Customer {id = readTaskException});

                sync = await store.TrySync(); // -------- Sync --------
                AreEqual("tasks: 1, failed: 1", sync.ToString());

                IsFalse(patchTaskReadException.Success);
                AreEqual("UnhandledException ~ SimulationException: simulated read task exception", patchTaskReadException.Error.Message);
            }
            
            // --- test write task error
            {
                var patchTaskWriteError = customers.Patch(new Customer {id = patchTaskError});

                sync = await store.TrySync(); // -------- Sync --------
                AreEqual("tasks: 1, failed: 1", sync.ToString());

                IsFalse(patchTaskWriteError.Success);
                AreEqual("DatabaseError ~ simulated patch task error", patchTaskWriteError.Error.Message);
            }
            
            // --- test write task exception
            {
                var patchTaskWriteException = customers.Patch(new Customer {id = patchTaskException});

                sync = await store.TrySync(); // -------- Sync --------
                AreEqual("tasks: 1, failed: 1", sync.ToString());

                IsFalse(patchTaskWriteException.Success);
                AreEqual("UnhandledException ~ SimulationException: simulated patch task exception", patchTaskWriteException.Error.Message);
            }
        }
    }
}
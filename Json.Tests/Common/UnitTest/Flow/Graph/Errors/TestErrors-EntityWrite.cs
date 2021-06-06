// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using Friflo.Json.Flow.Graph;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph.Errors
{
    public partial class TestErrors
    {
        // ------ Test each topic individual - using a FileDatabase
        [Test] public async Task TestEntityWrite    () { await Test(async (store, database) => await AssertEntityWrite      (store, database)); }

        private static async Task AssertEntityWrite(PocStore store, TestDatabase testDatabase) {
            testDatabase.ClearErrors();
            TestContainer testCustomers = testDatabase.GetTestContainer("Customer");
            
            const string deleteEntityError      = "delete-entity-error";
            const string createEntityError      = "create-entity-error";
            const string updateEntityError      = "update-entity-error";
            
            testCustomers.writeEntityErrors.Add(deleteEntityError,    () => testCustomers.WriteError(deleteEntityError));
            testCustomers.writeEntityErrors.Add(createEntityError,    () => testCustomers.WriteError(createEntityError));
            testCustomers.writeEntityErrors.Add(updateEntityError,    () => testCustomers.WriteError(updateEntityError));
            
            var customers = store.customers;
            
            var createError = customers.Create(new Customer{id = createEntityError})    .TaskName("createError");
            var updateError = customers.Update(new Customer{id = updateEntityError})    .TaskName("updateError");
            var deleteError = customers.Delete(new Customer{id = deleteEntityError})    .TaskName("deleteError");

            AreEqual(3, store.Tasks.Count);
            var sync = await store.TrySync(); // -------- Sync --------
            
            AreEqual("tasks: 3, failed: 3", sync.ToString());
            AreEqual(@"Sync() failed with task errors. Count: 3
|- createError # EntityErrors ~ count: 1
|   WriteError: Customer 'create-entity-error', simulated write entity error
|- updateError # EntityErrors ~ count: 1
|   WriteError: Customer 'update-entity-error', simulated write entity error
|- deleteError # EntityErrors ~ count: 1
|   WriteError: Customer 'delete-entity-error', simulated write entity error", sync.Message);

            IsFalse(deleteError.Success);
            var deleteErrors = deleteError.Error.entityErrors;
            AreEqual(1,        deleteErrors.Count);
            AreEqual("WriteError: Customer 'delete-entity-error', simulated write entity error", deleteErrors[deleteEntityError].ToString());
            
            IsFalse(createError.Success);
            var createErrors = createError.Error.entityErrors;
            AreEqual(1,        createErrors.Count);
            AreEqual("WriteError: Customer 'create-entity-error', simulated write entity error", createErrors[createEntityError].ToString());
            
            IsFalse(updateError.Success);
            var updateErrors = updateError.Error.entityErrors;
            AreEqual(1,        updateErrors.Count);
            AreEqual("WriteError: Customer 'update-entity-error', simulated write entity error", updateErrors[updateEntityError].ToString());
        }
    }
}
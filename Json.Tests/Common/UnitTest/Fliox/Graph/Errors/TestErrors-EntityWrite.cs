// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Graph;
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
        [Test] public async Task TestEntityWrite    () { await Test(async (store, database) => await AssertEntityWrite      (store, database)); }

        private static async Task AssertEntityWrite(PocStore store, TestDatabase testDatabase) {
            testDatabase.ClearErrors();
            TestContainer testCustomers = testDatabase.GetTestContainer(nameof(PocStore.customers));
            
            const string deleteEntityError      = "delete-entity-error";
            const string createEntityError      = "create-entity-error";
            const string upsertEntityError      = "upsert-entity-error";
            
            testCustomers.writeEntityErrors.Add(deleteEntityError,    () => testCustomers.WriteError(deleteEntityError));
            testCustomers.writeEntityErrors.Add(createEntityError,    () => testCustomers.WriteError(createEntityError));
            testCustomers.writeEntityErrors.Add(upsertEntityError,    () => testCustomers.WriteError(upsertEntityError));
            
            var customers = store.customers;
            
            var createError = customers.Create(new Customer{id = createEntityError})    .TaskName("createError");
            var upsertError = customers.Upsert(new Customer{id = upsertEntityError})    .TaskName("upsertError");
            var deleteError = customers.Delete(new Customer{id = deleteEntityError})    .TaskName("deleteError");

            AreEqual(3, store.Tasks.Count);
            var sync = await store.TrySync(); // -------- Sync --------
            
            AreEqual("tasks: 3, failed: 3", sync.ToString());
            AreEqual(@"Sync() failed with task errors. Count: 3
|- createError # EntityErrors ~ count: 1
|   WriteError: customers [create-entity-error], simulated write entity error
|- upsertError # EntityErrors ~ count: 1
|   WriteError: customers [upsert-entity-error], simulated write entity error
|- deleteError # EntityErrors ~ count: 1
|   WriteError: customers [delete-entity-error], simulated write entity error", sync.Message);

            IsFalse(deleteError.Success);
            var deleteErrors = deleteError.Error.entityErrors;
            AreEqual(1,        deleteErrors.Count);
            AreEqual("WriteError: customers [delete-entity-error], simulated write entity error", deleteErrors[new JsonKey(deleteEntityError)].ToString());
            
            IsFalse(createError.Success);
            var createErrors = createError.Error.entityErrors;
            AreEqual(1,        createErrors.Count);
            AreEqual("WriteError: customers [create-entity-error], simulated write entity error", createErrors[new JsonKey(createEntityError)].ToString());
            
            IsFalse(upsertError.Success);
            var upsertErrors = upsertError.Error.entityErrors;
            AreEqual(1,        upsertErrors.Count);
            AreEqual("WriteError: customers [upsert-entity-error], simulated write entity error", upsertErrors[new JsonKey(upsertEntityError)].ToString());
        }
    }
}
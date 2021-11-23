// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Schema.Native;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public partial class TestStore
    {
        [Test] public async Task TestCommands      () { await SetupCommandsDatabase(async (store) => await AssertCommands            (store)); }
        
        private static async Task SetupCommandsDatabase(Func<PocStore, Task> test) {
            using (var _            = SharedEnv.Default) // for LeakTestsFixture
            using (var database     = new FileDatabase(TestGlobals.PocStoreFolder, new PocHandler()))
            using (var hub          = new FlioxHub(database, TestGlobals.Shared))
            using (var createStore  = new PocStore(hub) { UserId = "createStore"})
            using (var nativeSchema = new NativeTypeSchema(typeof(PocStore)))
            using (database.Schema  = new DatabaseSchema(nativeSchema)) {
                await TestRelationPoC.CreateStore(createStore);
                await test(createStore);
            }
        }
        
        private static async Task AssertCommands(PocStore store) {
            var catalog         = store.Catalog();
            var catalogSchema   = store.CatalogSchema();
            var catalogList     = store.CatalogList();
            await store.SyncTasks();
            
            var catalogResult = catalog.Result;
            AreEqual(6,                 catalogResult.containers.Length);
            AreEqual("FileDatabase",    catalogResult.databaseType);
            
            var schemaResult = catalogSchema.Result;
            AreEqual(9,                 schemaResult.jsonSchemas.Count);
            AreEqual("PocStore",        schemaResult.schemaName);
            AreEqual("Friflo.Json.Tests.Common.UnitTest.Fliox.Client.json", schemaResult.schemaPath);
            
            var listResult = catalogList.Result;
            AreEqual(1,                 listResult.catalogs.Count);
            var catalog0 = listResult.catalogs[0];
            AreEqual(6,                 catalog0.containers.Length);
            AreEqual("FileDatabase",    catalog0.databaseType);
        }
    }
}
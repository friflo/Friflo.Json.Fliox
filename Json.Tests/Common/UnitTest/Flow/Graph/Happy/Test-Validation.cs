// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Schema.JSON;
using Friflo.Json.Flow.Schema.Native;
using Friflo.Json.Tests.Common.Utils;
using UnityEngine.TestTools;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph.Happy
{
    public partial class TestStore
    {
        [UnityTest] public IEnumerator ValidationByTypesCoroutine() { yield return RunAsync.Await(ValidationByTypes(), i => Logger.Info("--- " + i)); }
        [Test]      public async Task  ValidationByTypesAsync() { await ValidationByTypes(); }

        private static async Task ValidationByTypes() {
            using (var _                = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets/Graph/PocStore"))
            using (var createStore      = new PocStore(fileDatabase, "createStore"))
            using (var nativeSchema     = new NativeTypeSchema(TestGlobals.typeStore, typeof(PocStore)))
            using (fileDatabase.schema  = new DatabaseSchema(nativeSchema)) {
                await TestRelationPoC.CreateStore(createStore);
            }
        }
        
        [UnityTest] public IEnumerator ValidationByJsonSchemaCoroutine() { yield return RunAsync.Await(ValidationByJsonSchema(), i => Logger.Info("--- " + i)); }
        [Test]      public async Task  ValidationByJsonSchemaAsync() { await ValidationByJsonSchema(); }

        private static async Task ValidationByJsonSchema() {
            var jsonSchemaFolder    = CommonUtils.GetBasePath() + "assets/Schema/JSON/PocStore";
            var schemas             = JsonTypeSchema.ReadSchemas(jsonSchemaFolder);
            using (var _                = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets/Graph/PocStore"))
            using (var createStore      = new PocStore(fileDatabase, "createStore"))
            using (var jsonSchema       = new JsonTypeSchema(schemas, "./UnitTest.Flow.Graph.json#/definitions/PocStore"))
            using (fileDatabase.schema  = new DatabaseSchema(jsonSchema)) {
                await TestRelationPoC.CreateStore(createStore);
            }
        }
    }
}
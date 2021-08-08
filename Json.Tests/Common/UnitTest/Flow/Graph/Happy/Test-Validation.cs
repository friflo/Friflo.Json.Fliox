// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Schema.Native;
using Friflo.Json.Flow.Schema.Validation;
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
        [UnityTest] public IEnumerator FileValidationCoroutine() { yield return RunAsync.Await(FileValidation(), i => Logger.Info("--- " + i)); }
        [Test]      public async Task  FileValidationAsync() { await FileValidation(); }

        private static async Task FileValidation() {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/Graph/PocStore"))
            using (var createStore  = new PocStore(fileDatabase, "createStore"))
            using (var nativeSchema = new NativeTypeSchema(TestGlobals.typeStore))
            using (var validationSet= new ValidationSet(nativeSchema)) {
                var entityTypes     = nativeSchema.TypesAsValidationTypes(validationSet, EntityStore.GetEntityTypes<PocStore>());
                fileDatabase.schema = new DatabaseSchema(nativeSchema, entityTypes);
                await TestRelationPoC.CreateStore(createStore);
            }
        }
    }
}
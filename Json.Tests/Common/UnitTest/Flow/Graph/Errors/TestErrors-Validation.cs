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
                await AssertValidation(createStore);
            }
        }
        
        private static async Task AssertValidation(PocStore store) {
            var article = new Article  { id = "article-missing-name" };
            store.articles.Create(article);
            
            var sync = await store.TrySync();
            
            AreEqual(@"Sync() failed with task errors. Count: 1
|- CreateTask<Article> (#ids: 1) # EntityErrors ~ count: 1
|   WriteError: Article 'article-missing-name', Required property must not be null. at Article > name, pos: 40", sync.Message);
            
        }
    }
}
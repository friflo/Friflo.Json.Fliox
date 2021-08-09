// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Schema.Native;
using Friflo.Json.Flow.Schema.Validation;
using Friflo.Json.Flow.Sync;
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
            using (var _                = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets/Graph/PocStore"))
            using (var modifierDatabase = new WriteModifierDatabase(fileDatabase))
            using (var createStore      = new PocStore(modifierDatabase, "createStore"))
            using (var nativeSchema     = new NativeTypeSchema(TestGlobals.typeStore))
            using (var validationSet    = new ValidationSet(nativeSchema)) {
                var entityTypes         = nativeSchema.TypesAsValidationTypes(validationSet, EntityStore.GetEntityTypes<PocStore>());
                fileDatabase.schema     = new DatabaseSchema(nativeSchema, entityTypes);
                await AssertValidation(createStore, modifierDatabase);
            }
        }
        
        private static async Task AssertValidation(PocStore store, WriteModifierDatabase modifyDb) {
            modifyDb.ClearErrors();
            var articles = store.articles;
            
            var testCustomers = modifyDb.GetModifyContainer(nameof(Article));
            testCustomers.creates.Add("article-missing-id", entityValue => new EntityValue("{}"));

            var articleMissingName  = new Article { id = "article-missing-name" };
            var articleMissingId    = new Article { id = "article-missing-id" };
            articles.CreateRange(new [] { articleMissingName, articleMissingId});
            
            var sync = await store.TrySync(); // -------- Sync --------
            
            AreEqual(@"Sync() failed with task errors. Count: 1
|- CreateTask<Article> (#ids: 2) # EntityErrors ~ count: 2
|   WriteError: Article 'article-missing-id', Missing required fields: [id, name] at Article > (root), pos: 2
|   WriteError: Article 'article-missing-name', Required property must not be null. at Article > name, pos: 40", sync.Message);
            
        }
    }
}
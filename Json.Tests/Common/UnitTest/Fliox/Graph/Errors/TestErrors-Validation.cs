// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.NoSQL;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Tests.Common.Utils;
using UnityEngine.TestTools;
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
        [UnityTest] public IEnumerator FileValidationCoroutine() { yield return RunAsync.Await(FileValidation(), i => Logger.Info("--- " + i)); }
        [Test]      public async Task  FileValidationAsync() { await FileValidation(); }

        private static async Task FileValidation() {
            using (var _                = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets~/DB/PocStore"))
            using (var modifierDatabase = new WriteModifierDatabase(fileDatabase))
            using (var createStore      = new PocStore(modifierDatabase, "createStore"))
            using (var nativeSchema     = new NativeTypeSchema(TestGlobals.typeStore, typeof(PocStore)))
            using (fileDatabase.schema  = new DatabaseSchema(nativeSchema)) {
                await AssertValidation(createStore, modifierDatabase);
            }
        }
        
        private static async Task AssertValidation(PocStore store, WriteModifierDatabase modifyDb) {
            modifyDb.ClearErrors();
            var articles = store.articles;
            
            var articleModifier = modifyDb.GetWriteModifiers(nameof(PocStore.articles));
            articleModifier.writes.Add("article-missing-id",     val => new JsonValue("{\"id\": \"article-missing-id\" }"));
            articleModifier.writes.Add("article-incorrect-type", val => new JsonValue(val.json.AsString().Replace("\"xxx\"", "123")));

            var articleMissingName      = new Article { id = "article-missing-name" };
            var articleMissingId        = new Article { id = "article-missing-id" };
            var articleIncorrectType    = new Article { id = "article-incorrect-type", name = "xxx"};
            
            // --- test validation errors for creates
            var createTask = articles.CreateRange(new [] { articleMissingName, articleMissingId, articleIncorrectType});
            
            var sync = await store.TrySync(); // -------- Sync --------
            
            IsFalse(sync.Success);
            IsFalse(createTask.Success);
            var errors = createTask.Error.entityErrors;
            AreEqual("Required property must not be null. at Article > name, pos: 40", errors[new JsonKey("article-missing-name")].message);
            const string expectError = @"EntityErrors ~ count: 3
| WriteError: articles [article-incorrect-type], Incorrect type. was: 123, expect: string at Article > name, pos: 41
| WriteError: articles [article-missing-id], Missing required fields: [name] at Article > (root), pos: 29
| WriteError: articles [article-missing-name], Required property must not be null. at Article > name, pos: 40";
            AreEqual(expectError, createTask.Error.Message);
            
            // --- test validation errors for upserts
            var upsertTask = articles.UpsertRange(new [] { articleMissingName, articleMissingId, articleIncorrectType});
            
            sync = await store.TrySync(); // -------- Sync --------
            
            IsFalse(sync.Success);
            IsFalse(upsertTask.Success);
            errors = upsertTask.Error.entityErrors;
            AreEqual("Required property must not be null. at Article > name, pos: 40", errors[new JsonKey("article-missing-name")].message);
            AreEqual(expectError, upsertTask.Error.Message);
            
            
            // test validation errors for patches
            var patchModifier = modifyDb.GetPatchModifiers(nameof(PocStore.articles));
            patchModifier.patches.Add("article-2", patch => {
                var replace = (PatchReplace)patch.operations[0];
                replace.value.json = new JsonUtf8("123");
                return patch;
            });
            var articlePatch    = new Article { id = "article-2", name = "changed name"};
            var patchArticle    = articles.Patch(articlePatch);
            patchArticle.Member(a => a.name);
            
            sync = await store.TrySync(); // -------- Sync --------
            
            IsFalse(patchArticle.Success);
            AreEqual(@"EntityErrors ~ count: 1
| PatchError: articles [article-2], Incorrect type. was: 123, expect: string at Article > name, pos: 40", patchArticle.Error.Message);


            // --- test validation errors for invalid JSON
            articleModifier.writes.Add("invalid-json",       val => new JsonValue("X"));
            // articleModifier.writes.Add("empty-json",         val => new EntityValue(""));
            
            var invalidJson     = new Article { id = "invalid-json" };
            // var emptyJson       = new Article { id = "empty-json" };

            createTask = articles.CreateRange(new [] { invalidJson });
            
            await store.TrySync(); // -------- Sync --------
            
            IsFalse(sync.Success);
            IsFalse(createTask.Success);
            AreEqual("InvalidTask ~ error at entities[0]: entity value must be an object.", createTask.Error.Message);

        }
    }
}
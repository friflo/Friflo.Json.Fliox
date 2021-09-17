// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Graph;
using Friflo.Json.Fliox.DB.Sync;
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
        [Test] public async Task TestLogChangesCreate(){ await Test(async (store, database) => await AssertLogChangesCreate (store, database)); }

        private static async Task AssertLogChangesCreate(PocStore store, TestDatabase testDatabase) {
            testDatabase.ClearErrors();
            TestContainer testProducers = testDatabase.GetTestContainer(nameof(PocStore.producers));
            var articles = store.articles;

            // --- prepare precondition for log changes
            var readArticles = articles.Read();
            var patchArticle = readArticles.Find("log-create-read-error");
            await store.Sync();

            {
                var createError = "create-error";
                testProducers.writeTaskErrors.Add(createError, () => new CommandError("simulated create task error"));
                patchArticle.Result.producer = new Producer {id = createError};

                var logChanges = store.LogChanges();
                AreEqual("LogTask (patches: 1, creates: 1)", logChanges.ToString());

                var sync = await store.TrySync();

                AreEqual("tasks: 1, failed: 1", sync.ToString());
                AreEqual(TaskErrorType.EntityErrors, logChanges.Error.type);
                AreEqual(@"EntityErrors ~ count: 1
| WriteError: producers [create-error], DatabaseError - simulated create task error", logChanges.Error.Message);
            } {
                var createException = "create-exception";
                testProducers.writeTaskErrors.Add(createException, () => throw new SimulationException("simulated create task exception"));
                patchArticle.Result.producer = new Producer {id = createException};

                var logChanges = store.LogChanges();
                AreEqual("LogTask (patches: 1, creates: 1)", logChanges.ToString());

                AreEqual(1, store.Tasks.Count);
                var sync = await store.TrySync(); // -------- Sync --------

                AreEqual("tasks: 1, failed: 1", sync.ToString());
                AreEqual(TaskErrorType.EntityErrors, logChanges.Error.type);
                AreEqual(@"EntityErrors ~ count: 1
| WriteError: producers [create-exception], UnhandledException - SimulationException: simulated create task exception", logChanges.Error.Message);
            }

            /*  // not required as TestContainer as database doesnt mutate
                patchArticle.Result.producer = default; // restore precondition
                store.LogChanges();
                await store.Sync();
            */
        }
    }
}
// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Hubs;
using static Friflo.Json.Tests.Common.Utils.AssertUtils;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Errors
{
    public partial class TestErrors
    {
        // ------ Test each topic individual - using a FileDatabase
        [Test] public async Task TestCreateError(){ await Test(async (store, database) => await AssertCreateError (store, database)); }

        private static async Task AssertCreateError(PocStore store, TestDatabaseHub testHub) {
            testHub.ClearErrors();
            TestContainer testProducers = testHub.GetTestContainer(nameof(PocStore.producers));
            var articles = store.articles;

            // --- prepare precondition for log changes
            var readArticles = articles.Read();
            var patchArticle = readArticles.Find("log-create-read-error");
            await store.SyncTasks();

            {
                var createError = "create-error";
                testProducers.writeTaskErrors.Add(createError, () => new TaskExecuteError("simulated create task error"));
                var producer1 = new Producer {id = createError};
                var producerError = store.producers.Create(producer1);
                patchArticle.Result.producer = producer1.id;

                var storePatches = store.DetectAllPatches();
                AreEqual("DetectAllPatchesTask (patches: 1)", storePatches.ToString());

                var sync = await store.TrySyncTasks();

                AreEqual("tasks: 2, failed: 1", sync.ToString());
                AreEqual(TaskErrorType.DatabaseError, producerError.Error.type);
                AreEqual(@"DatabaseError ~ simulated create task error", producerError.Error.Message);
            } {
                var createException = "create-exception";
                testProducers.writeTaskErrors.Add(createException, () => throw new SimulationException("simulated create task exception"));
                var producer2 = new Producer {id = createException};
                var producerException = store.producers.Create(producer2);
                patchArticle.Result.producer = producer2.id;

                var storePatches = store.DetectAllPatches();
                AreEqual("DetectAllPatchesTask (patches: 1)", storePatches.ToString());

                AreEqual(2, store.Tasks.Count);
                var sync = await store.TrySyncTasks(); // ----------------

                AreEqual("tasks: 2, failed: 1", sync.ToString());
                AreEqual(TaskErrorType.UnhandledException, producerException.Error.type);
                AreEqualTrimStack(@"UnhandledException ~ SimulationException: simulated create task exception", producerException.Error.Message);
            }

            /*  // not required as TestContainer as database doesn't mutate
                patchArticle.Result.producer = default; // restore precondition
                store.LogChanges();
                await store.SyncTasks();
            */
        }
    }
}
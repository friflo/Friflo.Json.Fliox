// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Database.Remote;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using UnityEngine.TestTools;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph.Errors
{
    public partial class TestErrors : LeakTestsFixture
    {
        /// withdraw from allocation detection by <see cref="LeakTestsFixture"/> => init before tracking starts
        [NUnit.Framework.OneTimeSetUp]    public static void  Init()       { TestGlobals.Init(); }
        [NUnit.Framework.OneTimeTearDown] public static void  Dispose()    { TestGlobals.Dispose(); }

        [UnityTest] public IEnumerator FileUseCoroutine() { yield return RunAsync.Await(FileUse()); }
        [Test]      public async Task  FileUseAsync() { await FileUse(); }
        
        private async Task FileUse() {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/Graph/db"))
            using (var testDatabase = new TestDatabase(fileDatabase))
            using (var useStore     = new PocStore(testDatabase, "useStore")) {
                await TestStoresErrors(useStore, testDatabase);
            }
        }
        
        [UnityTest] public IEnumerator LoopbackUseCoroutine() { yield return RunAsync.Await(LoopbackUse()); }
        [Test]      public async Task  LoopbackUseAsync() { await LoopbackUse(); }
        
        private async Task LoopbackUse() {
            using (var _                = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets/Graph/db"))
            using (var testDatabase     = new TestDatabase(fileDatabase))
            using (var loopbackDatabase = new LoopbackDatabase(testDatabase))
            using (var useStore         = new PocStore(loopbackDatabase, "useStore")) {
                await TestStoresErrors(useStore, testDatabase);
            }
        }
        
        [UnityTest] public IEnumerator HttpUseCoroutine() { yield return RunAsync.Await(HttpUse()); }
        [Test]      public async Task  HttpUseAsync() { await HttpUse(); }
        
        private async Task HttpUse() {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/Graph/db"))
            using (var testDatabase = new TestDatabase(fileDatabase))
            using (var hostDatabase = new HttpHostDatabase(testDatabase, "http://+:8080/", null)) {
                await Happy.TestStore.RunRemoteHost(hostDatabase, async () => {
                    using (var remoteDatabase   = new HttpClientDatabase("http://localhost:8080/"))
                    using (var useStore         = new PocStore(remoteDatabase, "useStore")) {
                        await TestStoresErrors(useStore, testDatabase);
                    }
                });
            }
        }

        // ------ Test all topics on different EntityDatabase implementations
        private static async Task TestStoresErrors(PocStore useStore, TestDatabase testDatabase) {
            await AssertQueryTask       (useStore, testDatabase);
            await AssertReadTask        (useStore, testDatabase);
            await AssertTaskExceptions  (useStore, testDatabase);
            await AssertTaskError       (useStore, testDatabase);
            await AssertEntityWrite     (useStore, testDatabase);
            await AssertEntityPatch     (useStore, testDatabase);
            await AssertLogChangesPatch (useStore, testDatabase);
            await AssertLogChangesCreate(useStore, testDatabase);
            await AssertSyncErrors      (useStore, testDatabase);
        }
        
        private static async Task Test(Func<PocStore, TestDatabase, Task> test) {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/Graph/db"))
            using (var testDatabase = new TestDatabase(fileDatabase))
            using (var useStore     = new PocStore(testDatabase, "useStore")) {
                await test(useStore, testDatabase);
            }
        }
    }
}
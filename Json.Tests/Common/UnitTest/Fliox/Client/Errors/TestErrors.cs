// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Hubs;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using UnityEngine.TestTools;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Errors
{
    public partial class TestErrors : LeakTestsFixture
    {
        /// withdraw from allocation detection by <see cref="LeakTestsFixture"/> => init before tracking starts
        [NUnit.Framework.OneTimeSetUp]    public static void  Init()       { TestGlobals.Init(); }
        [NUnit.Framework.OneTimeTearDown] public static void  Dispose()    { TestGlobals.Dispose(); }

        [UnityTest] public IEnumerator FileUseCoroutine() { yield return RunAsync.Await(FileUse()); }
        [Test]      public async Task  FileUseAsync() { await FileUse(); }
        
        private async Task FileUse() {
            using (var _            = SharedHostEnv.Instance) // for LeakTestsFixture
            using (var fileDatabase = new FileDatabase(TestGlobals.PocStoreFolder))
            using (var testHub      = new TestDatabaseHub(fileDatabase))
            using (var useStore     = new PocStore(testHub) { UserId = "useStore"}) {
                await TestStoresErrors(useStore, testHub);
            }
        }
        
        [UnityTest] public IEnumerator LoopbackUseCoroutine() { yield return RunAsync.Await(LoopbackUse()); }
        [Test]      public async Task  LoopbackUseAsync() { await LoopbackUse(); }
        
        private async Task LoopbackUse() {
            using (var _                = SharedHostEnv.Instance) // for LeakTestsFixture
            using (var fileDatabase     = new FileDatabase(TestGlobals.PocStoreFolder))
            using (var testHub          = new TestDatabaseHub(fileDatabase))
            using (var loopbackHub      = new LoopbackHub(testHub))
            using (var useStore         = new PocStore(loopbackHub) { UserId = "useStore", ClientId = "use-client"}) {
                await TestStoresErrors(useStore, testHub);
            }
        }
        
        [UnityTest] public IEnumerator HttpUseCoroutine() { yield return RunAsync.Await(HttpUse()); }
        [Test]      public async Task  HttpUseAsync() { await HttpUse(); }
        
        private async Task HttpUse() {
            using (var _            = SharedHostEnv.Instance) // for LeakTestsFixture
            using (var fileDatabase = new FileDatabase(TestGlobals.PocStoreFolder))
            using (var testHub      = new TestDatabaseHub(fileDatabase))
            using (var hostHub      = new HttpHostHub(testHub))
            using (var server       = new HttpListenerHost("http://+:8080/", hostHub)) {
                await Happy.TestStore.RunServer(server, async () => {
                    using (var remoteDatabase   = new HttpClientHub("http://localhost:8080/"))
                    using (var useStore         = new PocStore(remoteDatabase) { UserId = "useStore", ClientId = "use-client"}) {
                        await TestStoresErrors(useStore, testHub);
                    }
                });
            }
        }

        // ------ Test all topics on different EntityDatabase implementations
        private static async Task TestStoresErrors(PocStore useStore, TestDatabaseHub testHub) {
            await AssertQueryTask       (useStore, testHub);
            await AssertReadTask        (useStore, testHub);
            await AssertTaskExceptions  (useStore, testHub);
            await AssertTaskError       (useStore, testHub);
            await AssertEntityWrite     (useStore, testHub);
            await AssertEntityPatch     (useStore, testHub);
            await AssertLogChangesPatch (useStore, testHub);
            await AssertLogChangesCreate(useStore, testHub);
            await AssertSyncErrors      (useStore, testHub);
        }
        
        private static async Task Test(Func<PocStore, TestDatabaseHub, Task> test) {
            using (var _            = SharedHostEnv.Instance) // for LeakTestsFixture
            using (var fileDatabase = new FileDatabase(TestGlobals.PocStoreFolder))
            using (var testHub      = new TestDatabaseHub(fileDatabase))
            using (var useStore     = new PocStore(testHub) { UserId = "useStore"}) {
                await test(useStore, testHub);
            }
        }
    }
}
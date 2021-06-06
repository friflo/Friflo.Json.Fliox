// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Sync;
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
        // ------ Test each topic individual - using a FileDatabase
        [Test] public async Task TestSyncErrors     () { await Test(async (store, database) => await AssertSyncErrors       (store, database)); }
        
        private static async Task AssertSyncErrors(PocStore store, TestDatabase testDatabase) {
            testDatabase.ClearErrors();
            // use Echo to simulate error/exception
            const string echoSyncError = "echo-sync-error";
            const string echoSyncException      = "echo-sync-exception";
            testDatabase.syncErrors.Add(echoSyncError,       () => new SyncResponse{error = new SyncError{message = "simulated SyncError"}});
            testDatabase.syncErrors.Add(echoSyncException,   () => throw new SimulationException ("simulated SyncException"));
            
            var helloTask = store.Echo("Hello World");
            AreEqual("EchoTask (message: Hello World)", helloTask.ToString());
            
            await store.Sync(); // -------- Sync --------
            
            AreEqual("Hello World", helloTask.Result);

            // --- Sync error
            {
                var syncError = store.Echo(echoSyncError);
                
                // test throwing exception in case of Sync errors
                try {
                    await store.Sync(); // -------- Sync --------
                    
                    Fail("Sync() intended to fail - code cannot be reached");
                } catch (SyncResultException sre) {
                    AreEqual("simulated SyncError", sre.Message);
                    AreEqual(1, sre.failed.Count);
                    AreEqual("SyncError ~ simulated SyncError", sre.failed[0].Error.ToString());
                }
                AreEqual("SyncError ~ simulated SyncError", syncError.Error.ToString());
            }
            // --- Sync exception
            {
                var syncException = store.Echo(echoSyncException);
                
                var sync = await store.TrySync(); // -------- Sync --------
                
                IsFalse(sync.Success);
                AreEqual("SimulationException: simulated SyncException", sync.Message);
                AreEqual(1, sync.failed.Count);
                AreEqual("SyncError ~ SimulationException: simulated SyncException", sync.failed[0].Error.ToString());
                
                AreEqual("SyncError ~ SimulationException: simulated SyncException", syncException.Error.ToString());
            }
        }
    }
}
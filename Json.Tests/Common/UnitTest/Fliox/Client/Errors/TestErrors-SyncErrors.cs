// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Client;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.Mapper;
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
        [Test] public async Task TestSyncErrors     () { await Test(async (store, database) => await AssertSyncErrors       (store, database)); }
        
        private static async Task AssertSyncErrors(PocStore store, TestDatabaseHub testHub) {
            testHub.ClearErrors();
            // use SendCommand() to simulate error/exception
            const string msgSyncError      = "msg-sync-error";
            const string msgSyncException  = "msg-sync-exception";
            testHub.syncErrors.Add(msgSyncError,       () => new ExecuteSyncResult("simulated SyncError"));
            testHub.syncErrors.Add(msgSyncException,   () => throw new SimulationException ("simulated SyncException"));
            
            //var helloTask1 = store.Echo(new JsonValue("\"Hello World 1\""));
            var helloTask1 = store.SendCommand(StdCommand.Echo, "Hello World 1");
            var helloTask2 = store.SendCommand(StdCommand.Echo, "Hello World 2");
            var helloTask3 = store.SendCommand(StdCommand.Echo);
            
            AreEqual("CommandTask (name: Echo)", helloTask1.ToString());

            await store.SyncTasks(); // ----------------
            
            AreEqual("\"Hello World 1\"",   helloTask1.ResultJson.AsString());
            AreEqual("Hello World 1",       helloTask1.ReadResult<string>());
            
            IsTrue(helloTask1.TryReadResult<string>(out var helloTask1Result, out _));
            AreEqual("Hello World 1",       helloTask1Result);
            
            var e = Throws<JsonReaderException>(() => helloTask1.ReadResult<int>());
            AreEqual("JsonReader/error: Cannot assign string to int. got: 'Hello World 1' path: '(root)' at position: 15", e.Message);
            
            helloTask1.TryReadResult<int>(out _, out e);
            AreEqual("JsonReader/error: Cannot assign string to int. got: 'Hello World 1' path: '(root)' at position: 15", e.Message);

            AreEqual("Hello World 2",       helloTask2.ReadResult<string>());
            AreEqual(null,                  helloTask3.ReadResult<string>());
            

            // --- SyncTasks error
            {
                var syncError = store.SendCommand(msgSyncError);
                
                // test throwing exception in case of SyncTasks errors
                try {
                    await store.SyncTasks(); // ----------------
                    
                    Fail("SyncTasks() intended to fail - code cannot be reached");
                } catch (ExecuteTasksException sre) {
                    AreEqual("simulated SyncError", sre.Message);
                    AreEqual(1, sre.failed.Count);
                    AreEqual("SyncError ~ simulated SyncError", sre.failed[0].Error.ToString());
                }
                AreEqual("SyncError ~ simulated SyncError", syncError.Error.ToString());
            }
            // --- SyncTasks exception
            {
                var syncException = store.SendCommand(msgSyncException);
                
                var sync = await store.TrySyncTasks(); // ----------------
                
                IsFalse(sync.Success);
                AreEqual("SimulationException: simulated SyncException", sync.Message);
                AreEqual(1, sync.failed.Count);
                AreEqual("SyncError ~ SimulationException: simulated SyncException", sync.failed[0].Error.ToString());
                
                AreEqual("SyncError ~ SimulationException: simulated SyncException", syncException.Error.ToString());
            }
        }
    }
}
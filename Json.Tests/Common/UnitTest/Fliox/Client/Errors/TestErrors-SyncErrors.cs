// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Mapper;
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
        [Test] public async Task TestSyncErrors     () { await Test(async (store, database) => await AssertSyncErrors       (store, database)); }
        
        private static async Task AssertSyncErrors(PocStore store, TestDatabaseHub testHub) {
            testHub.ClearErrors();
            // use SendCommand() to simulate error/exception
            var msgSyncError        = "MsgSyncError";
            var msgSyncException    = "MsgSyncException";
            testHub.syncErrors.Add(msgSyncError,       () => new ExecuteSyncResult("simulated SyncError", ErrorResponseType.BadRequest));
            testHub.syncErrors.Add(msgSyncException,   () => throw new SimulationException ("simulated SyncException"));
            
            var helloTask1 = store.std.Echo("Hello World 1");
            var helloTask2 = store.std.Echo("Hello World 2");
            var helloTask3 = store.std.Echo((string)null);
            
            AreEqual("CommandTask (name: std.Echo)", helloTask1.ToString());

            await store.SyncTasks(); // ----------------
            
            AreEqual("Hello World 1",       helloTask1.Result);
            AreEqual("Hello World 1",       helloTask1.ReadResult<string>());
            AreEqual("\"Hello World 1\"",   helloTask1.RawResult.AsString());
            
            IsTrue(helloTask1.TryReadResult<string>(out var helloTask1Result, out _));
            AreEqual("Hello World 1",       helloTask1Result);
            
            var e = Throws<JsonReaderException>(() => helloTask1.ReadResult<int>());
            AreEqual("JsonReader/error: Cannot assign string to int. got: 'Hello World 1' path: '(root)' at position: 15", e.Message);
            
            helloTask1.TryReadResult<int>(out _, out e);
            AreEqual("JsonReader/error: Cannot assign string to int. got: 'Hello World 1' path: '(root)' at position: 15", e.Message);

            AreEqual("Hello World 2",       helloTask2.Result);
            AreEqual(null,                  helloTask3.Result);
            

            // --- SyncTasks error
            {
                var syncError = store.SendCommand<bool?>(msgSyncError);
                
                // test throwing exception in case of SyncTasks errors
                try {
                    await store.SyncTasks(); // ----------------
                    
                    Fail("SyncTasks() intended to fail - code cannot be reached");
                } catch (SyncTasksException sre) {
                    AreEqual("simulated SyncError", sre.Message);
                    AreEqual(1, sre.failed.Count);
                    AreEqual("SyncError ~ simulated SyncError", sre.failed[0].Error.ToString());
                }
                AreEqual("SyncError ~ simulated SyncError", syncError.Error.ToString());
            }
            // --- SyncTasks exception
            {
                var syncException = store.SendCommand<bool?, bool?>(msgSyncException, null); // use SendCommand with two generic
                
                var sync = await store.TrySyncTasks(); // ----------------
                
                IsFalse(sync.Success);
                AreEqualTrimStack("Internal SimulationException: simulated SyncException", sync.Message);
                AreEqual(1, sync.Failed.Count);
                AreEqualTrimStack("SyncError ~ Internal SimulationException: simulated SyncException", sync.Failed[0].Error.Message);
                
                AreEqualTrimStack("SyncError ~ Internal SimulationException: simulated SyncException", syncException.Error.Message);
            }
            // --- SyncTasks exception
            {
                var syncException = store.SendCommand<bool?, bool?>(msgSyncException, null);
                try {
                    await store.SyncTasks(); // ----------------
                    
                    Fail("SyncTasks() intended to fail - code cannot be reached");
                } catch (SyncTasksException sre) {
                    AreEqualTrimStack("Internal SimulationException: simulated SyncException", sre.Message);
                    AreEqual(1, sre.failed.Count);
                    AreEqualTrimStack("SyncError ~ Internal SimulationException: simulated SyncException", sre.failed[0].Error.Message);
                
                    AreEqualTrimStack("SyncError ~ Internal SimulationException: simulated SyncException", syncException.Error.Message);
                }
            }
        }
    }
}
// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Host.Event.Compact;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Event
{
    public sealed class TestEventCompactor
    {
        public class Record
        {
            public int     id;
            public float   x;
            public float   y;
        }
        
        public class GameClient : FlioxClient
        {
            // --- containers
            public readonly EntitySet <int, Record>     records;

            public GameClient(FlioxHub hub, string dbName = null) : base (hub, dbName) { }
        }
        
        
        [Test]
        public static  void TestEventCompactor_Upsert() {
            using (var sharedEnv = SharedEnv.Default) {
                var database        = new MemoryDatabase("remote-memory", smallValueSize: 1024);
                var hub             = new FlioxHub(database, sharedEnv);
                var dispatcher      = new EventDispatcher(EventDispatching.Queue);
                var compactor       = new ChangeCompactor();
                dispatcher.ChangeCompactor = compactor;
                compactor.AddDatabase(database);
                hub.EventDispatcher = dispatcher;

                var client          = new GameClient(hub);
                client.records.SubscribeChanges(Change.All, (changes, context) => {});
                client.SyncTasksSynchronous();
                
                var record = new Record();
                for (int n = 1; n <= 10; n++) {
                    record.id   = n;
                    record.x    = n;
                    record.y    = n;
                    client.records.Upsert(record);
                    client.SyncTasksSynchronous();
                }
                
                hub.EventDispatcher.SendQueuedEvents();
            }
        }
    }
}
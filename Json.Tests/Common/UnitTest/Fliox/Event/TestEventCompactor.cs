// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Host.Event.Compact;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable NotAccessedField.Local
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Event
{
    public static class TestEventCompactor
    {
        private class Record
        {
            public int     id;
            public float   x;
            public float   y;
        }

        private class TestCompactClient : FlioxClient
        {
            // --- containers
            public readonly EntitySet <int, Record>     records = null;

            public TestCompactClient(FlioxHub hub, EventReceiver receiver)
                : base (hub, null, receiver == null ? null : new ClientOptions ((h, c)  => receiver)) { }
        }
        
        /// <summary> Used to test performance and memory usage of <see cref="EventDispatcher"/>.EnqueueSyncTasks() </summary>
        private class IgnoreReceiver : EventReceiver
        {
            internal int count;
            
            public override bool    IsOpen()           => true;
            public override bool    IsRemoteTarget()   => false;
            public override void    SendEvent(in ClientEvent clientEvent) { count++; }
        }
        
        [Test]
        public static  void TestEventCompactor_Upsert() {
            TestEventCompactor_UpsertIntern(2, 5, null);
        }
        
        /// <summary>
        /// Skip parsing <see cref="ClientEvent"/>'s to check <see cref="ChangeCompactor"/> performance <br/>
        /// E.g in case of 1000 upserts using 1000 subscribers result in parsing 1.000.000 upsert tasks
        /// </summary>
        [Test]
        public static  void TestEventCompactor_UpsertPerf() {
            const int clientCount = 10; // 1000;
            var eventReceiver = new IgnoreReceiver();
            TestEventCompactor_UpsertIntern(clientCount, clientCount, eventReceiver);
        }
        
        private static  void TestEventCompactor_UpsertIntern(int subCount, int upsertCount, IgnoreReceiver receiver) {
            using (var sharedEnv = SharedEnv.Default) {
                var database        = new MemoryDatabase("remote-memory", smallValueSize: 1024);
                var hub             = new FlioxHub(database, sharedEnv);
                var dispatcher      = new EventDispatcher(EventDispatching.Queue);
                var compactor       = new ChangeCompactor();
                dispatcher.ChangeCompactor = compactor;
                compactor.AddDatabase(database);
                hub.EventDispatcher = dispatcher;
                var receivedEvents  = 0;
                
                for (int i = 0; i < subCount; i++) {
                    var sub = new TestCompactClient(hub, receiver) { UserId = $"client-{i}" };
                    sub.records.SubscribeChanges(Change.All, (changes, context) => {
                        receivedEvents++;
                        if (upsertCount != changes.Upserts.Count) {
                            Fail($"Expect: {upsertCount} was: {changes.Upserts.Count}");
                        }
                    });
                    sub.SyncTasksSynchronous();
                }
                var client = new TestCompactClient(hub, receiver) { UserId = "sender" };
                var record = new Record();
                for (int n = 0; n < upsertCount; n++) {
                    record.id   = n;
                    record.x    = n;
                    record.y    = n;
                    client.records.Upsert(record);
                    client.SyncTasksSynchronous();
                }
                hub.EventDispatcher.SendQueuedEvents();
                if (receiver != null) {
                    AreEqual(subCount, receiver.count);
                } else {
                    AreEqual(subCount, receivedEvents);
                }
            }
        }
    }
}
// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Event;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable NotAccessedField.Local
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Event
{
    internal class Record
    {
        public int     id;
        public float   x;
        public float   y;
    }
    
    public static class TestEventAccumulation
    {
        private class TestAccumulationClient : FlioxClient
        {
            // --- containers
            public readonly EntitySet <int, Record>     records = default;

            public TestAccumulationClient(FlioxHub hub)
                : base (hub) { }
        }
        
        [Test]
        public static  void TestEventAccumulation_Changes()
        {
            using (var sharedEnv = SharedEnv.Default) {
                var database        = new MemoryDatabase("remote-memory") { SmallValueSize = 1024 };
                var hub             = new FlioxHub(database, sharedEnv);
                var dispatcher      = new EventDispatcher(EventDispatching.Queue);
                dispatcher.EnableChangeAccumulation(database);
                hub.EventDispatcher = dispatcher;
                
                var sub             = new TestAccumulationClient(hub);
                var send            = new TestAccumulationClient(hub);
                var changeEvents    = 0;

                sub.records.SubscribeChanges(Change.All, (changes, context) => {
                    switch (changeEvents) {
                        case 0:
                            changeEvents++;
                            AreEqual(1, changes.Creates.Count);
                            AreEqual(2, changes.Upserts.Count);
                            AreEqual(1, changes.Deletes.Count);
                            // raw changes
                            AreEqual(1, changes.raw.creates.Count);
                            AreEqual(2, changes.raw.upserts.Count);
                            AreEqual(1, changes.raw.deletes.Count);
                            AreEqual(3, changes.raw.deletes[0].AsLong());
                            break;
                        case 1:
                            changeEvents++;
                            AreEqual(1, changes.Patches.Count);
                            // raw changes
                            AreEqual(1, changes.raw.patches.Count);
                            break;
                    }
                });
                sub.SyncTasksSynchronous();
                
                var record1 = new Record { id = 1 };
                send.records.Create(record1);
                send.records.Upsert(new Record { id = 2 });
                send.records.Delete(3);
                send.records.Upsert(new Record { id = 4 });
                send.SyncTasksSynchronous();
                dispatcher.SendQueuedEvents();
                
                record1.x = 42;
                send.records.DetectPatches(record1); // create merge task
                send.SyncTasksSynchronous();
                dispatcher.SendQueuedEvents();
                
                AreEqual(2, changeEvents);
            }
        }
        
        /// <summary> Used to test performance and memory usage of <see cref="EventDispatcher"/>.EnqueueSyncTasks() </summary>
        private class IgnoreReceiver : IEventReceiver
        {
            internal int count;
            
            public  string  Endpoint           => nameof(IgnoreReceiver);
            public  bool    IsOpen()           => true;
            public  bool    IsRemoteTarget()   => false;
            public  void    SendEvent(in ClientEvent clientEvent) { count++; }
        }
        
        [Test]
        public static void TestEventAccumulation_Upsert() {
            const int clientCount = 2;
            const int upsertCount = 5;
            TestEventAccumulation_UpsertIntern(clientCount, 2, upsertCount, null);
        }
        
        /// <summary>
        /// Skip parsing <see cref="ClientEvent"/>'s to check <see cref="EventDispatcher.EnableChangeAccumulation"/> performance <br/>
        /// E.g in case of 1000 upserts using 1000 subscribers result in parsing 1.000.000 <see cref="Record"/>'s
        /// </summary>
        [Test]
        public static void TestEventAccumulation_UpsertPerf() {
            var eventReceiver = new IgnoreReceiver();
            // 500 clients, 60 Hz  =>  with change accumulation: 267 ms,  without change accumulation 7173 ms
            const int clientCount = 10; // 1000;
            const int frameCount  = 60; // 60 Hz
            const int upsertCount = clientCount; // typically each client send a single upsert to the Hub per frame
            TestEventAccumulation_UpsertIntern(clientCount, frameCount, upsertCount, eventReceiver);
        }
        
        private static void TestEventAccumulation_UpsertIntern(
            int             clientCount,
            int             frameCount,
            int             upsertCount,
            IgnoreReceiver  receiver)
        {
            using (var sharedEnv = SharedEnv.Default) {
                var database        = new MemoryDatabase("remote-memory") { SmallValueSize = 1024 };
                var hub             = new FlioxHub(database, sharedEnv);
                var dispatcher      = new EventDispatcher(EventDispatching.Queue);
                dispatcher.EnableChangeAccumulation(database);
                hub.EventDispatcher = dispatcher;
                var changeEvents  = 0;
                
                // --- setup subscribers
                for (int i = 0; i < clientCount; i++) {
                    var subClient = new TestAccumulationClient(hub) { UserId = $"client-{i}" };
                    if (receiver != null) {
                        subClient.Options.DebugEventReceiver = receiver;
                    }
                    subClient.records.SubscribeChanges(Change.All, (changes, context) => {
                        changeEvents++;
                        if (upsertCount != changes.Upserts.Count) {
                            Fail($"Expect: {upsertCount} was: {changes.Upserts.Count}");
                        }
                    });
                    subClient.SyncTasksSynchronous();
                }
                var client = new TestAccumulationClient(hub) { UserId = "sender" };
                var record = new Record();
                
                // --- simulate sending upsert's
                for (int i = 0; i < frameCount; i++) {
                    // Creating upsert's has significant cost. In real scenario this is done by remote clients.
                    // Make a reference run with upsert but without sending queued events to get its cost.
                    for (int n = 0; n < upsertCount; n++) {
                        record.id   = n;
                        record.x    = n;
                        record.y    = n;
                        client.records.Upsert(record);
                        var result = client.SyncTasksSynchronous();
                        result.Reuse(client);
                    }
                    dispatcher.SendQueuedEvents();
                }
                var eventCount = clientCount * frameCount;
                if (receiver != null) {
                    AreEqual(eventCount, receiver.count);
                } else {
                    AreEqual(eventCount, changeEvents);
                }
            }
        }
    }
}

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Tests.Common.UnitTest.Fliox.Event
{
    // ReSharper disable once InconsistentNaming
    internal static class Gen_Record
    {
        private const int Gen_id    = 0;
        private const int Gen_x     = 1;
        private const int Gen_y     = 2;

        private static bool ReadField (ref Record obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_id:    obj.id = reader.ReadInt16 (field, out success);  return success;
                case Gen_x:     obj.x  = reader.ReadSingle(field, out success);  return success;
                case Gen_y:     obj.y  = reader.ReadSingle(field, out success);  return success;
            }
            return false;
        }

        private static void Write(ref Record obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteInt32 (fields[Gen_id], obj.id,  ref firstMember);
            writer.WriteSingle(fields[Gen_x],  obj.x,   ref firstMember);
            writer.WriteSingle(fields[Gen_y],  obj.y,   ref firstMember);
        }
    }
}
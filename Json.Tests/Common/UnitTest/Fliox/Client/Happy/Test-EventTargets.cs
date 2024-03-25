// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable ConvertToConstant.Local
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public partial class TestHappy
    {
        [Test]
        public static void TestEventTargets() {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            using (var database         = new MemoryDatabase("test", null, new PocService()))
            using (var hub              = new FlioxHub(database))
            using (var client1          = new PocStore(hub))
            using (var client2          = new PocStore(hub))
            using (hub.EventDispatcher  = new EventDispatcher(EventDispatching.Send)) // dispatch events directly to simplify test
            {
                client1.ClientId    = "client-1";
                client1.UserId      = "user-1";
                var userParam       = new UserParam { addGroups = new List<string> { "group-1" } };
                client1.std.User(userParam);
                client1.SyncTasks().Wait();
                
                for (int n = 0; n < 5; n++) {
                    int eventCount      = 0;
                    client1.SubscribeMessage("*", (message, context) => { eventCount++; });
                    client1.SubscriptionEventHandler = context => {
                        AreEqual("user-1", context.UserId.AsString());
                        var expect = new [] { "msg-1", "msg-2", "msg-3", "msg-4", "msg-5", "msg-6", "msg-7", "msg-8", "msg-9", "msg-10", "Command1" };
                        var actual = context.Messages.Select(msg => msg.Name);
                        AreEqual(expect, actual);
                    };
                    SendMessages(client1);
                    AreEqual(11, eventCount);
                }
                //
                client2.ClientId    = "client-2";
                client2.UserId      = "user-2";
                for (int n = 0; n < 5; n++) {
                    int eventCount      = 0;
                    client2.SubscribeMessage("*", (message, context) => { eventCount++; });
                    client2.SubscriptionEventHandler = context => {
                        Fail("unexpected events");
                    };
                    SendMessages(client2);
                    AreEqual(0, eventCount);
                }
            }
        }
        
        private static void SendMessages(PocStore client) {
            // --- client1 receive all events
            var clientId    = "client-1";
            var userId      = "user-1";
            var clientKey   = new ShortString(clientId);   // test interface using a JsonKey
            var userKey     = new ShortString(userId);     // test interface using a JsonKey

            // --- single target
            client.SendMessage("msg-1").EventTargetUser(userId);
            client.SendMessage("msg-2").EventTargetUser(userKey);
            
            client.SendMessage("msg-3").EventTargetClient(clientId);
            client.SendMessage("msg-4").EventTargetClient(clientKey);

            // --- multi target
            client.SendMessage("msg-5").EventTargetUsers (new[] { userId });
            client.SendMessage("msg-6").EventTargetUsers (new[] { userKey });

            client.SendMessage("msg-7").EventTargetClients (new[] { clientId });
            client.SendMessage("msg-8").EventTargetClients (new[] { clientKey });
            
            var eventTargets = new EventTargets();
            eventTargets.AddUser(userId);
            client.SendMessage("msg-9").EventTargets = eventTargets;
            
            client.SendMessage("msg-10").EventTargetGroup("group-1");
            
            client.Command1().EventTargetUser(userId);
        //  todo client.CommandInt(111).EventTargetUser(user1Key);
            
            client.SyncTasks().Wait();
            
            client.UnsubscribeMessage("*", null);
            client.SyncTasks().Wait();
        }
    }
}
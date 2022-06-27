// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Linq;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable ConvertToConstant.Local
// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public partial class TestHappy
    {
        [Test]
        public static void TestEventTargets() {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            using (var database         = new MemoryDatabase("test", new PocHandler()))
            using (var hub              = new FlioxHub(database))
            using (var store            = new PocStore(hub))
            using (hub.EventDispatcher  = new EventDispatcher(false)) {
                AssertEventTargets(store);
            }
        }
        
        private static void AssertEventTargets(PocStore store) {
            var client1     = "client-1";
            var user1       = "user-1";
            var client1Key  = new JsonKey(client1);
            var user1Key    = new JsonKey(user1);
            store.ClientId  = client1;
            store.UserId    = user1;
            store.SubscribeMessage("*", (message, context) => { });
            store.SubscriptionEventHandler += context => {
                AreEqual("user-1", context.SrcUserId.ToString());
                var expect = new [] { "msg-1", "msg-3", "msg-3", "msg-4", "msg-5", "msg-6", "msg-7", "msg-8", "Command1" };
                var actual = context.Messages.Select(msg => msg.Name);
                AreEqual(expect, actual);
            };

            // --- single target
            IsTask(store.SendMessage("msg-1").EventTargetUser(user1));
            IsTask(store.SendMessage("msg-2").EventTargetUser(user1Key));
            
            IsTask(store.SendMessage("msg-3").EventTargetClient(client1));
            IsTask(store.SendMessage("msg-4").EventTargetClient(client1Key));

            // --- multi target
            IsTask(store.SendMessage("msg-5").EventTargetUsers (new[] { user1 }));
            IsTask(store.SendMessage("msg-6").EventTargetUsers (new[] { user1Key }));

            IsTask(store.SendMessage("msg-7").EventTargetClients (new[] { client1 }));
            IsTask(store.SendMessage("msg-8").EventTargetClients (new[] { client1Key }));
            
            // var eventTargets = new EventTargets();
            // store.SendMessage("msg-5" ).EventTargets = eventTargets;
            
            store.Command1().EventTargetUser(user1);
        //  todo store.CommandInt(111).EventTargetUser(user1Key);
            
            store.SyncTasks().Wait();
        }
        
        private static void IsTask(MessageTask _) {
            
        }
    }
}
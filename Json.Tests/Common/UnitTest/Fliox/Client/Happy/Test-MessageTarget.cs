// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using NUnit.Framework;

// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public partial class TestHappy
    {
        [Test]
        public static void TestMessageTarget() {
            using (var _           = SharedEnv.Default) // for LeakTestsFixture
            using (var database    = new MemoryDatabase("test"))
            using (var hub         = new FlioxHub(database))
            using (var store       = new PocStore(hub)) {
                AssertMessageTarget(store);
            }
        }
        
        private static void AssertMessageTarget(PocStore store) {
            JsonKey userId      = store.UserInfo.userId;
            JsonKey clientId    = store.UserInfo.clientId;
            var userClient      = new EventTargetClient(userId, clientId);
            
            // --- single target
            IsTask(store.SendMessage("msg-1").EventTargetUser("user-1"));
            IsTask(store.SendMessage("msg-1").EventTargetUser(store.UserId));
            
            IsTask(store.SendMessage("msg-1").EventTargetClient("user-1", "client-1"));
            IsTask(store.SendMessage("msg-1").EventTargetClient(userId, clientId));
            IsTask(store.SendMessage("msg-1").EventTargetClient(userClient));

            // --- multi target
            IsTask(store.SendMessage("msg-4").EventTargetUsers (new[] { "user-1" }));
            IsTask(store.SendMessage("msg-4").EventTargetUsers (new[] { userId }));

            IsTask(store.SendMessage("msg-4").EventTargetClients (new[] { ("user-1", "client-1")}));
            IsTask(store.SendMessage("msg-4").EventTargetClients (new[] { userClient }));
            
            var eventTargets1 = new EventTargets("user-1");
            var eventTargets2 = new EventTargets(userId);
            var eventTargets3 = new EventTargets(userClient);
            
            store.SendMessage("msg-5" ).EventTargets = eventTargets1;
            store.SendMessage("msg-5" ).EventTargets = eventTargets2;
            store.SendMessage("msg-5" ).EventTargets = eventTargets3;
            
            var cmd = store.SendCommand<int, int>("cmd", 123).EventTargetUser("ddd");
            // cmd.Target = target1; // must error with:   [CS1061] 'CommandTask<int>' does not contain a definition for 'Target' ...
        }
        
        private static void IsTask(MessageTask _) {
            
        }
    }
}
// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using NUnit.Framework;

// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public partial class TestHappy
    {
        [Test]
        public static void TestEventTargets() {
            using (var _           = SharedEnv.Default) // for LeakTestsFixture
            using (var database    = new MemoryDatabase("test"))
            using (var hub         = new FlioxHub(database))
            using (var store       = new PocStore(hub)) {
                AssertEventTargets(store);
            }
        }
        
        private static void AssertEventTargets(PocStore store) {
            JsonKey userId      = store.UserInfo.userId;
            JsonKey clientId    = store.UserInfo.clientId;
            
            // --- single target
            IsTask(store.SendMessage("msg-1").EventTargetUser("user-1"));
            IsTask(store.SendMessage("msg-1").EventTargetUser(userId));
            
            IsTask(store.SendMessage("msg-1").EventTargetClient("client-1"));
            IsTask(store.SendMessage("msg-1").EventTargetClient(clientId));

            // --- multi target
            IsTask(store.SendMessage("msg-4").EventTargetUsers (new[] { "user-1" }));
            IsTask(store.SendMessage("msg-4").EventTargetUsers (new[] { userId }));

            IsTask(store.SendMessage("msg-4").EventTargetClients (new[] { "client-1 "}));
            IsTask(store.SendMessage("msg-4").EventTargetClients (new[] { clientId }));
            
            var eventTargets = new EventTargets();
            store.SendMessage("msg-5" ).EventTargets = eventTargets;
            
            store.SendCommand<int, int>("cmd", 123).EventTargetUser("ddd");
        }
        
        private static void IsTask(MessageTask _) {
            
        }
    }
}
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
            var userClient      = new UserClient(userId, clientId);
            
            // --- single target
            IsTask(store.SendMessage("msg-1").TargetUser("user-1"));
            IsTask(store.SendMessage("msg-1").TargetUser(store.UserId));
            
            IsTask(store.SendMessage("msg-1").TargetClient("user-1", "client-1"));
            IsTask(store.SendMessage("msg-1").TargetClient(userId, clientId));
            IsTask(store.SendMessage("msg-1").TargetClient(userClient));

            // --- multi target
            IsTask(store.SendMessage("msg-4").TargetUsers (new[] { "user-1" }));
            IsTask(store.SendMessage("msg-4").TargetUsers (new[] { userId }));

            IsTask(store.SendMessage("msg-4").TargetClients (new[] { ("user-1", "client-1")}));
            IsTask(store.SendMessage("msg-4").TargetClients (new[] { userClient }));
            
            var target1 = new MessageTargets("user-1");
            var target2 = new MessageTargets(userId);
            var target3 = new MessageTargets(userClient);
            
            store.SendMessage("msg-5" ).Targets = target1;
            store.SendMessage("msg-5" ).Targets = target2;
            store.SendMessage("msg-5" ).Targets = target3;
            
            var cmd = store.SendCommand<int, int>("cmd", 123).TargetUser("ddd");
            // cmd.Target = target1; // must error with:   [CS1061] 'CommandTask<int>' does not contain a definition for 'Target' ...
        }
        
        private static void IsTask(MessageTask _) {
            
        }
    }
}
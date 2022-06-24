// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Client;

// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public partial class TestHappy
    {
        public static void LabSendMessage(PocStore store) {
            JsonKey userId      = store.UserInfo.userId;
            JsonKey clientId    = store.UserInfo.clientId;
            var userClient      = new UserClient(userId, clientId);
            
            // --- single target
            IsTask(store.SendMessage("msg-1").ToUser("user-1"));
            IsTask(store.SendMessage("msg-1").ToUser(store.UserId));
            
            IsTask(store.SendMessage("msg-1").ToClient("user-1", "client-1"));
            IsTask(store.SendMessage("msg-1").ToClient(userId, clientId));
            IsTask(store.SendMessage("msg-1").ToClient(userClient));

            // --- multi target
            IsTask(store.SendMessage("msg-4").ToUsers (new[] { "user-1" }));
            IsTask(store.SendMessage("msg-4").ToUsers (new[] { userId }));

            IsTask(store.SendMessage("msg-4").ToClients (new[] { ("user-1", "client-1")}));
            IsTask(store.SendMessage("msg-4").ToClients (new[] { userClient }));
            
            var target1 = new MessageTarget("user-1");
            var target2 = new MessageTarget(userId);
            var target3 = new MessageTarget(userClient);
            
            store.SendMessage("msg-5" ).Target = target1;
            store.SendMessage("msg-5" ).Target = target2;
            store.SendMessage("msg-5" ).Target = target3;
        }
        
        private static void IsTask(MessageTask _) {
            
        }
    }
}
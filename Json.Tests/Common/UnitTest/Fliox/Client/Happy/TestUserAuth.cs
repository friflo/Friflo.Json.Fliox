// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.DB.UserAuth;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Threading;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public static class TestUserAuth
    {
        private class Context
        {
            internal FlioxHub            hub;
            internal UserAuthenticator   authenticator;
        }
        
        private static async Task<Context> CreateHub () {
            var userDatabase    = new MemoryDatabase(TestGlobals.UserDB, new UserDBService());
            var authenticator   = new UserAuthenticator(userDatabase, TestGlobals.Shared);
            var database        = new MemoryDatabase(TestGlobals.DB);
            var hub          	= new FlioxHub(database, TestGlobals.Shared);
            var eventDispatcher = new EventDispatcher(EventDispatching.Send); // required for SubscribeMessage() and SubscribeChanges()
            
            hub.Authenticator   = authenticator;
            hub.EventDispatcher = eventDispatcher;
            hub.AddExtensionDB(userDatabase);
            await authenticator.SetAdminPermissions();
            await authenticator.SubscribeUserDbChanges(hub.EventDispatcher);
            
            return new Context { hub = hub, authenticator = authenticator };
        }
        
        [Test]
        public static void TestUserAuth_ChangePermission () {
            SingleThreadSynchronizationContext.Run(async () => {
                var cx          = await CreateHub();
                var hub         = cx.hub;
                
                var client      = new FlioxClient(hub)                      { UserId = "unknown", Token = "ddd" };
                var userStore   = new UserStore(hub, TestGlobals.UserDB)    { UserId = "admin",   Token = "admin" };
                //
                var message = client.SendMessage("test");
                await client.TrySyncTasks();
                IsFalse(message.Success);
                AreEqual("PermissionDenied ~ not authorized. Authentication failed. user: 'unknown'", message.Error.Message);
                
                var allUsersPermission = new UserPermission { id = "all-users", roles = new List<string> { "hub-admin"} };
                userStore.permissions.Create(allUsersPermission);
                await userStore.SyncTasks();
                
                message = client.SendMessage("test");
                await client.SyncTasks();
                IsTrue(message.Success);
                
                allUsersPermission.roles.Clear();
                userStore.permissions.Upsert(allUsersPermission);
                await userStore.SyncTasks();
                
                message = client.SendMessage("test");
                await client.TrySyncTasks();
                IsFalse(message.Success);
                AreEqual("PermissionDenied ~ not authorized. Authentication failed. user: 'unknown'", message.Error.Message);
            });
        }
    }
}
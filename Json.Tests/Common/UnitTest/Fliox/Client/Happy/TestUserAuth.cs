// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox;
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
        [Test]
        public static void TestUserAuth_ChangePermission () {
            SingleThreadSynchronizationContext.Run(async () => {
                using (var userDatabase     = new MemoryDatabase(TestGlobals.UserDB, new UserDBService()))
                using (var authenticator    = new UserAuthenticator(userDatabase, TestGlobals.Shared))
                using (var database         = new MemoryDatabase(TestGlobals.DB))
                using (var hub          	= new FlioxHub(database, TestGlobals.Shared))
                using (var eventDispatcher  = new EventDispatcher(EventDispatching.Send)) // required for SubscribeMessage() and SubscribeChanges()
                {
                    hub.Authenticator   = authenticator.SetAdminPermissions();
                    hub.EventDispatcher = eventDispatcher;
                    hub.AddExtensionDB(userDatabase);
                    authenticator.SubscribeUserDbChanges(hub.EventDispatcher);
                    
                    var client      = new FlioxClient(hub)                      { UserId = "unknown", Token = "ddd" };
                    var userStore   = new UserStore(hub, TestGlobals.UserDB)    { UserId = "admin",   Token = "admin" };
                    //
                    var message = client.SendMessage("test");
                    await client.TrySyncTasks();
                    IsFalse(message.Success);
                    AreEqual("PermissionDenied ~ not authorized. Authentication failed. user: 'unknown'", message.Error.Message);
                    
                    var allUsersPermission = new UserPermission { id = new ShortString("all-users"), roles = new List<string> { "hub-admin"} };
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
                }
            });
        }
    }
}
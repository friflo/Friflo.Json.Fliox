// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.DB.Cluster;
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
            var userDatabase    = new MemoryDatabase(TestGlobals.UserDB, UserDB.Schema, new UserDBService());
            var authenticator   = new UserAuthenticator(userDatabase, TestGlobals.Shared);
            var database        = new MemoryDatabase(TestGlobals.DB);
            var hub          	= new FlioxHub(database, TestGlobals.Shared);
            var eventDispatcher = new EventDispatcher(EventDispatching.Send); // required for SubscribeMessage() and SubscribeChanges()
            
            hub.AddExtensionDB (new ClusterDB("cluster", hub));     // optional - expose info of hosted databases. cluster is required by Hub Explorer
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
                
                var allUsersPermission = new UserPermission { id = ".all-users", roles = new HashSet<string> { "hub-admin"} };
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
        
        [Test]
        public static void TestUserAuth_ClusterAccessAll () {
            SingleThreadSynchronizationContext.Run(async () => {
                var cx          = await CreateHub();
                var hub         = cx.hub;
                var cluster     = new ClusterStore(hub, "cluster")    { UserId = "unknown",   Token = "unknown" };
                
                var containers = cluster.containers.QueryAll();
                await cluster.TrySyncTasks();
                IsFalse(containers.Success);
                AreEqual("PermissionDenied ~ not authorized. Authentication failed. user: 'unknown'", containers.Error.Message);
                
                await cx.authenticator.SetClusterPermissions("cluster", Users.All);
                containers = cluster.containers.QueryAll();
                await cluster.TrySyncTasks();
                IsTrue(containers.Success);
                
                await cx.authenticator.SetClusterPermissions("cluster", Users.Authenticated);
                containers = cluster.containers.QueryAll();
                await cluster.TrySyncTasks();
                IsFalse(containers.Success);
                AreEqual("PermissionDenied ~ not authorized. Authentication failed. user: 'unknown'", containers.Error.Message);
            });
        }
        
        [Test]
        public static void TestUserAuth_ClusterAccessAuthenticated () {
            SingleThreadSynchronizationContext.Run(async () => {
                var cx          = await CreateHub();
                var hub         = cx.hub;
                var peterCred   = new UserCredential { id = "Peter", token = "Peter" };
                var userStore   = new UserStore(hub, TestGlobals.UserDB)    { UserId = "admin",   Token = "admin" };
                userStore.credentials.Create(peterCred);
                await userStore.SyncTasks();
                
                //
                var cluster     = new ClusterStore(hub, "cluster")    { UserId = "Peter",   Token = "Peter" };
                var containers = cluster.containers.QueryAll();
                await cluster.TrySyncTasks();
                IsFalse(containers.Success);
                AreEqual("PermissionDenied ~ not authorized. user: 'Peter'", containers.Error.Message);
                
                await cx.authenticator.SetClusterPermissions("cluster", Users.Authenticated);
                containers = cluster.containers.QueryAll();
                await cluster.TrySyncTasks();
                IsTrue(containers.Success);
            });
        }
    }
}
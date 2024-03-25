// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.DB.Monitor;
using Friflo.Json.Fliox.Hub.DB.UserAuth;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Hubs;
using Friflo.Json.Tests.Common.Utils;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

// ReSharper disable PossibleNullReferenceException
// ReSharper disable UseObjectOrCollectionInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public partial class TestHappy
    {
        private static readonly string HostName = "test-host";
        
        [Test]
        public static async Task TestMonitoringFile() {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            using (var database         = new FileDatabase(TestGlobals.DB, TestGlobals.PocStoreFolder))
            using (var hub          	= new FlioxHub(database, TestGlobals.Shared) { HostName = HostName })
            using (var monitorDB        = new MonitorDB(TestGlobals.Monitor, hub)) {
                hub.AddExtensionDB(monitorDB);
                // assert same behavior with default Authenticator or UserAuthenticator
                await AssertNoAuthMonitoringDB  (hub);
                await AssertAuthMonitoringDB    (hub, hub);
            }
        }
        
        [Test]
        public static async Task TestMonitoringLoopback() {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            using (var database         = new FileDatabase(TestGlobals.DB, TestGlobals.PocStoreFolder))
            using (var hub          	= new FlioxHub(database, TestGlobals.Shared) { HostName = HostName })
            using (var monitor          = new MonitorDB(TestGlobals.Monitor, hub))
            using (var loopbackHub      = new LoopbackHub(hub)) {
                hub.AddExtensionDB(monitor);
                // assert same behavior with default Authenticator or UserAuthenticator 
                await AssertNoAuthMonitoringDB  (loopbackHub);
                await AssertAuthMonitoringDB    (loopbackHub, hub);
            }
        }
        
        [Test]
        public static async Task TestMonitoringHttp() {
            using (var _            = SharedEnv.Default) // for LeakTestsFixture
            using (var database     = new FileDatabase(TestGlobals.DB, TestGlobals.PocStoreFolder))
            using (var hub          = new FlioxHub(database, TestGlobals.Shared) { HostName = HostName })
            using (var httpHost     = new HttpHost(hub, "/"))
            using (var server       = new HttpServer("http://+:8080/", httpHost)) 
            using (var monitor      = new MonitorDB(TestGlobals.Monitor, hub))
            using (var remoteHub    = new HttpClientHub(TestGlobals.DB, "http://localhost:8080/", TestGlobals.Shared)) {
                hub.AddExtensionDB(monitor);
                await RunServer(server, async () => {
                    // assert same behavior with default Authenticator or UserAuthenticator
                    await AssertNoAuthMonitoringDB  (remoteHub);
                    await AssertAuthMonitoringDB    (remoteHub, hub);
                });
            }
        }
        
        private static async Task AssertAuthMonitoringDB(FlioxHub hub, FlioxHub database) {
            using (var userDatabase     = new FileDatabase(TestGlobals.Monitor, CommonUtils.GetBasePath() + "assets~/DB/user_db", UserDB.Schema, new UserDBService()))
            using (var authenticator    = new UserAuthenticator(userDatabase, TestGlobals.Shared)) {
                database.Authenticator  = authenticator;
                await AssertAuthSuccessMonitoringDB (hub);
                await AssertAuthFailedMonitoringDB  (hub);
            }
        }

        private  static async Task AssertNoAuthMonitoringDB(FlioxHub hub) {
            const string userId     = "monitor-admin";
            const string token      = "monitor-admin"; 
            using (var store    = new PocStore(hub))
            using (var monitor  = new MonitorStore(hub, TestGlobals.Monitor)) {
                var result = await Monitor(store, monitor, userId, token);
                AssertResult(result);
                
                // as clearing monitor stats subsequent call has same result
                result = await Monitor(store, monitor, userId, token);
                AssertResult(result);
            }
        }

        private  static async Task AssertAuthSuccessMonitoringDB(FlioxHub hub) {
            const string userId     = "monitor-admin";
            const string token      = "monitor-admin";
            using (var store    = new PocStore(hub))
            using (var monitor  = new MonitorStore(hub, TestGlobals.Monitor)) {
                var result = await Monitor(store, monitor, userId, token);
                AssertResult(result);
                
                // as clearing monitor stats subsequent call has same result
                result = await Monitor(store, monitor, userId, token);
                AssertResult(result);
                
                await AssertMonitorErrors(monitor);
            }
        }
        
        private  static async Task AssertAuthFailedMonitoringDB(FlioxHub hub) {
            const string userId     = "anonymous-user";
            const string token      = "invalid";
            using (var store    = new PocStore(hub))
            using (var monitor  = new MonitorStore(hub, TestGlobals.Monitor)) {
                var result = await Monitor(store, monitor, userId, token);
                AssertAuthFailedResult(result);
                
                // as clearing monitor stats subsequent call has same result
                result = await Monitor(store, monitor, userId, token);
                AssertAuthFailedResult(result);
            }
        }

        private static void AssertResult(MonitorResult result) {
            // --- hosts
            var hosts   = result.hosts.Result;
            AreEqual(1, hosts.Count);
            
            var host    = hosts[0];
            AreEqual("{'id':'test-host','counts':{'requests':2,'tasks':3}}", host.ToString());
            
            // --- users
            var users   = result.users.Result;
            AreEqual(2, users.Count);
            
            var anonymousInfo = users.Find(i => i.id.IsEqual(new ShortString(User.AnonymousId)));
            IsNull(anonymousInfo);
            
            var adminInfo = users.Find(i => i.id.IsEqual(new ShortString("monitor-admin"))).ToString();
            AreEqual("{'id':'monitor-admin','clients':['monitor-client'],'counts':[{'db':'monitor','requests':1,'tasks':1}]}", adminInfo);
            
            var pocAdmin = users.Find(i => i.id.IsEqual(new ShortString("poc-admin"))).ToString();
            AreEqual("{'id':'poc-admin','clients':['poc-client'],'counts':[{'db':'main_db','requests':1,'tasks':2}]}", pocAdmin);
            
            // --- clients
            var clients = result.clients.Result;
            AreEqual(2, clients.Count);
            
            var pocClientInfo = clients.Find(i => i.id.IsEqual(new ShortString("poc-client"))).ToString();
            AreEqual("{'id':'poc-client','user':'poc-admin','counts':[{'db':'main_db','requests':1,'tasks':2}]}", pocClientInfo);
            
            var monitorClientInfo = clients.Find(i => i.id.IsEqual(new ShortString("monitor-client"))).ToString();
            AreEqual("{'id':'monitor-client','user':'monitor-admin','counts':[{'db':'monitor','requests':1,'tasks':1}]}", monitorClientInfo);
            
            NotNull(result.user.Result);
            NotNull(result.client.Result);
        }
        
        private static void AssertAuthFailedResult(MonitorResult result) {
            IsFalse(result.users.Success);
            IsFalse(result.clients.Success);
        }

        private  static async Task AssertMonitorErrors(MonitorStore monitor) {
            var deleteUser      = monitor.users.Delete(new ShortString("123"));
            var createUser      = monitor.users.Create(new UserHits{id = new ShortString("abc")});
            await monitor.TrySyncTasks();
            AreEqual("InvalidTask ~ MonitorDB does not support task: 'create'",   createUser.Error.Message);
            AreEqual("InvalidTask ~ MonitorDB does not support task: 'delete'",   deleteUser.Error.Message);
        }
        
        private  static async Task<MonitorResult> Monitor(PocStore store, MonitorStore monitor, string userId, string token) {
            monitor.ClientId    = "monitor-client";
            // clear stats requires successful authentication as admin
            monitor.UserId      = "monitor-admin";
            monitor.Token       = "monitor-admin";
            monitor.ClearStats();
            await monitor.TrySyncTasks();
            
            store.UserId        = "poc-admin";
            store.ClientId      = "poc-client";
            store.Token         = "poc-admin";

            store.articles.Read().Find("xxx");
            store.customers.Read().Find("yyy");
            await store.SyncTasks();
            
            monitor.UserId      = userId;
            monitor.Token       = token;
            
            var result = new MonitorResult();
            result.users       = monitor.users.QueryAll();
            result.clients     = monitor.clients.QueryAll();
            result.user        = monitor.users.Read().Find(new ShortString("poc-admin"));
            result.client      = monitor.clients.Read().Find(new ShortString("poc-client"));
            result.hosts       = monitor.hosts.QueryAll();
            result.sync        = await monitor.TrySyncTasks();
            
            return result;
        }
        
        internal class MonitorResult {
            internal    SyncResult                      sync;
            internal    QueryTask<ShortString,  UserHits>   users;
            internal    QueryTask<ShortString,  ClientHits> clients;
            internal    Find<ShortString,       UserHits>   user;
            internal    Find<ShortString,       ClientHits> client;
            internal    QueryTask<ShortString,  HostHits>   hosts;
        }
    }
}

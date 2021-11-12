// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Auth;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Monitor;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Hub.UserAuth;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Hubs;
using Friflo.Json.Tests.Common.Utils;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

// ReSharper disable UseObjectOrCollectionInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public partial class TestStore
    {
        private static readonly string HostName  = "Test";
        
        [Test]
        public static async Task TestMonitoringFile() {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            using (var database         = new FileDatabase(TestGlobals.PocStoreFolder))
            using (var hub          	= new FlioxHub(database, TestGlobals.Shared, HostName))
            using (var monitorDB        = new MonitorDatabase(hub)) {
                hub.AddExtensionDB(monitorDB);
                await AssertNoAuthMonitoringDB  (hub);
                await AssertAuthMonitoringDB    (hub, hub);
            }
        }
        
        [Test]
        public static async Task TestMonitoringLoopback() {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            using (var database         = new FileDatabase(TestGlobals.PocStoreFolder))
            using (var hub          	= new FlioxHub(database, TestGlobals.Shared, HostName))
            using (var monitor          = new MonitorDatabase(hub))
            using (var loopbackHub      = new LoopbackHub(hub)) {
                hub.AddExtensionDB(monitor);
                await AssertNoAuthMonitoringDB  (loopbackHub);
                await AssertAuthMonitoringDB    (loopbackHub, hub);
            }
        }
        
        [Test]
        public static async Task TestMonitoringHttp() {
            using (var _            = SharedEnv.Default) // for LeakTestsFixture
            using (var database     = new FileDatabase(TestGlobals.PocStoreFolder))
            using (var hub          = new FlioxHub(database, TestGlobals.Shared, HostName))
            using (var hostHub      = new HttpHostHub(hub))
            using (var server       = new HttpListenerHost("http://+:8080/", hostHub)) 
            using (var monitor      = new MonitorDatabase(hub))
            using (var clientHub    = new HttpClientHub("http://localhost:8080/", TestGlobals.Shared)) {
                hub.AddExtensionDB(monitor);
                await RunServer(server, async () => {
                    await AssertNoAuthMonitoringDB  (clientHub);
                    await AssertAuthMonitoringDB    (clientHub, hub);
                });
            }
        }
        
        private static async Task AssertAuthMonitoringDB(FlioxHub hub, FlioxHub database) {
            using (var userDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets~/DB/UserStore", new UserDBHandler()))
            using (var authenticator    = new UserAuthenticator(userDatabase, TestGlobals.Shared)) {
                database.Authenticator  = authenticator;
                await AssertAuthSuccessMonitoringDB (hub);
                await AssertAuthFailedMonitoringDB  (hub);
            }
        }

        private  static async Task AssertNoAuthMonitoringDB(FlioxHub hub) {
            const string userId     = "poc-user";
            const string token      = "invalid"; 
            using (var store    = new PocStore(hub))
            using (var monitor  = new MonitorStore(hub, MonitorDatabase.Name)) {
                var result = await Monitor(store, monitor, userId, token);
                AssertNoAuthResult(result);
                
                // as clearing monitor stats subsequent call has same result
                result = await Monitor(store, monitor, userId, token);
                AssertNoAuthResult(result);
            }
        }

        private  static async Task AssertAuthSuccessMonitoringDB(FlioxHub hub) {
            const string userId     = "monitor-admin";
            const string token      = "monitor-admin";
            using (var store    = new PocStore(hub))
            using (var monitor  = new MonitorStore(hub, MonitorDatabase.Name)) {
                var result = await Monitor(store, monitor, userId, token);
                AssertAuthResult(result);
                
                // as clearing monitor stats subsequent call has same result
                result = await Monitor(store, monitor, userId, token);
                AssertAuthResult(result);
                
                await AssertMonitorErrors(monitor);
            }
        }
        
        private  static async Task AssertAuthFailedMonitoringDB(FlioxHub hub) {
            const string userId     = "anonymous-user";
            const string token      = "invalid";
            using (var store    = new PocStore(hub))
            using (var monitor  = new MonitorStore(hub, MonitorDatabase.Name)) {
                var result = await Monitor(store, monitor, userId, token);
                AssertAuthFailedResult(result);
                
                // as clearing monitor stats subsequent call has same result
                result = await Monitor(store, monitor, userId, token);
                AssertAuthFailedResult(result);
            }
        }

        private static void AssertNoAuthResult(MonitorResult result) {
            IsFalse(result.users.Success);
            IsFalse(result.clients.Success);
            IsFalse(result.hosts.Success);
            IsFalse(result.user.Success);
            IsFalse(result.client.Success);
        }

        private static void AssertAuthResult(MonitorResult result) {
            var users   = result.users.Results;
            var clients = result.clients.Results;
            var host    = result.hosts.Results[new JsonKey("Test")];
            AreEqual("{'id':'Test','counts':{'requests':2,'tasks':3}}",                                      host.ToString());
            AreEqual("{'id':'anonymous','clients':[],'counts':[]}",                                          users[User.AnonymousId].ToString());
            
            var adminInfo = users[new JsonKey("monitor-admin")].ToString();
            AreEqual("{'id':'monitor-admin','clients':[],'counts':[{'db':'monitor','requests':1,'tasks':1}]}", adminInfo);
                
            var pocClientInfo = clients[new JsonKey("poc-client")].ToString();
            AreEqual("{'id':'poc-client','user':'poc-admin','counts':[{'db':'default','requests':1,'tasks':2}]}", pocClientInfo);
            var monitorClientInfo = clients[new JsonKey("monitor-client")].ToString();
            AreEqual("{'id':'monitor-client','user':'monitor-admin','counts':[{'db':'monitor','requests':1,'tasks':1}]}", monitorClientInfo);
            
            NotNull(result.user.Result);
            NotNull(result.client.Result);
        }
        
        private static void AssertAuthFailedResult(MonitorResult result) {
            IsFalse(result.users.Success);
            IsFalse(result.clients.Success);
        }

        private  static async Task AssertMonitorErrors(MonitorStore monitor) {
            var deleteUser      = monitor.users.Delete(new JsonKey("123"));
            var createUser      = monitor.users.Create(new UserInfo{id = new JsonKey("abc")});
            await monitor.TrySyncTasks();
            AreEqual("InvalidTask ~ MonitorDatabase does not support task: 'create'",   createUser.Error.Message);
            AreEqual("InvalidTask ~ MonitorDatabase does not support task: 'delete'",   deleteUser.Error.Message);
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
            result.user        = monitor.users.Read().Find(new JsonKey("poc-admin"));
            result.client      = monitor.clients.Read().Find(new JsonKey("poc-client"));
            result.hosts       = monitor.hosts.QueryAll();
            result.sync        = await monitor.TrySyncTasks();
            
            return result;
        }
        
        internal class MonitorResult {
            internal    SyncResult                      sync;
            internal    QueryTask<JsonKey,  UserInfo>   users;
            internal    QueryTask<JsonKey,  ClientInfo> clients;
            internal    Find<JsonKey,       UserInfo>   user;
            internal    Find<JsonKey,       ClientInfo> client;
            internal    QueryTask<JsonKey,  HostInfo>   hosts;
        }
    }
}

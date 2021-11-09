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
                await AssertNoAuthMonitoringDB  (hub, monitorDB);
                await AssertAuthMonitoringDB    (hub, monitorDB, hub);
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
                var monitorDB = new RemoteExtensionDatabase(loopbackHub, MonitorDatabase.Name);
                await AssertNoAuthMonitoringDB  (loopbackHub, monitorDB);
                await AssertAuthMonitoringDB    (loopbackHub, monitorDB, hub);
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
                    var monitorDB   = new RemoteExtensionDatabase(clientHub, MonitorDatabase.Name);
                    await AssertNoAuthMonitoringDB  (clientHub, monitorDB);
                    await AssertAuthMonitoringDB    (clientHub, monitorDB, hub);
                });
            }
        }
        
        private static async Task AssertAuthMonitoringDB(FlioxHub hub, EntityDatabase monitorDB, FlioxHub database) {
            using (var userDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets~/DB/UserStore", new UserDBHandler()))
            using (var authenticator    = new UserAuthenticator(userDatabase, TestGlobals.Shared)) {
                database.Authenticator  = authenticator;
                await AssertAuthSuccessMonitoringDB (hub, monitorDB);
                await AssertAuthFailedMonitoringDB  (hub, monitorDB);
            }
        }

        private  static async Task AssertNoAuthMonitoringDB(FlioxHub hub, EntityDatabase monitorDb) {
            const string userId     = "poc-user";
            const string clientId   = "poc-client"; 
            using (var store    = new PocStore(hub))
            using (var monitor  = new MonitorStore(monitorDb, store)) {
                var result = await Monitor(store, monitor, userId, clientId);
                AssertNoAuthResult(result);
                
                // as clearing monitor stats subsequent call has same result
                result = await Monitor(store, monitor, userId, clientId);
                AssertNoAuthResult(result);
                
                await AssertMonitoringErrors(monitor);
            }
        }

        private  static async Task AssertAuthSuccessMonitoringDB(FlioxHub hub, EntityDatabase monitorDb) {
            const string userId     = "admin";
            const string clientId   = "admin-client"; 
            using (var store    = new PocStore(hub))
            using (var monitor  = new MonitorStore(monitorDb, store)) {
                store.Token = "admin-token";
                var result = await Monitor(store, monitor, userId, clientId);
                AssertAuthResult(result);
                
                // as clearing monitor stats subsequent call has same result
                result = await Monitor(store, monitor, userId, clientId);
                AssertAuthResult(result);
            }
        }
        
        private  static async Task AssertAuthFailedMonitoringDB(FlioxHub hub, EntityDatabase monitorDb) {
            const string userId     = "admin";
            const string clientId   = "admin-xxx"; 
            using (var store    = new PocStore(hub))
            using (var monitor  = new MonitorStore(monitorDb, store)) {
                store.Token = "invalid";
                var result = await Monitor(store, monitor, userId, clientId);
                AssertAuthFailedResult(result);
                
                // as clearing monitor stats subsequent call has same result
                result = await Monitor(store, monitor, userId, clientId);
                AssertAuthFailedResult(result);
            }
        }

        private static void AssertNoAuthResult(MonitorResult result) {
            var users   = result.users.Results;
            var clients = result.clients.Results;
            AreEqual("{'id':'anonymous','clients':[],'counts':[]}",   users[User.AnonymousId].ToString());
            
            var pocUserInfo = users[new JsonKey("poc-user")].ToString();
            AreEqual("{'id':'poc-user','clients':['poc-client'],'counts':[{'db':'default','requests':1,'tasks':2},{'db':'monitor','requests':1,'tasks':1}]}", pocUserInfo);
            AreEqual(2, users.Count);
                
            var pocClientInfo = clients[new JsonKey("poc-client")].ToString();
            AreEqual("{'id':'poc-client','user':'poc-user','counts':[{'db':'default','requests':1,'tasks':2},{'db':'monitor','requests':1,'tasks':1}]}", pocClientInfo);
            AreEqual(1, clients.Count);
            
            NotNull(result.user.Result);
            NotNull(result.client.Result);
        }

        private static void AssertAuthResult(MonitorResult result) {
            var users   = result.users.Results;
            var clients = result.clients.Results;
            var host    = result.hosts.Results[new JsonKey("Test")];
            AreEqual("{'id':'Test','counts':{'requests':2,'tasks':3}}",                                      host.ToString());
            AreEqual("{'id':'anonymous','clients':[],'counts':[]}",                                          users[User.AnonymousId].ToString());
            
            var adminInfo = users[new JsonKey("admin")].ToString();
            AreEqual("{'id':'admin','clients':['admin-client'],'counts':[{'db':'default','requests':1,'tasks':2},{'db':'monitor','requests':1,'tasks':1}]}", adminInfo);
                
            var adminClientInfo = clients[new JsonKey("admin-client")].ToString();
            AreEqual("{'id':'admin-client','user':'admin','counts':[{'db':'default','requests':1,'tasks':2},{'db':'monitor','requests':1,'tasks':1}]}", adminClientInfo);
            
            NotNull(result.user.Result);
            NotNull(result.client.Result);
        }
        
        private static void AssertAuthFailedResult(MonitorResult result) {
            IsFalse(result.users.Success);
            IsFalse(result.clients.Success);
        }

        private  static async Task AssertMonitoringErrors(MonitorStore monitor) {
            var deleteUser      = monitor.users.Delete(new JsonKey("123"));
            var createUser      = monitor.users.Create(new UserInfo{id = new JsonKey("abc")});
            await monitor.TrySyncTasks();
            AreEqual("InvalidTask ~ MonitorDatabase does not support task: 'create'",   createUser.Error.Message);
            AreEqual("InvalidTask ~ MonitorDatabase does not support task: 'delete'",   deleteUser.Error.Message);
        }
        
        private  static async Task<MonitorResult> Monitor(PocStore store, MonitorStore monitor, string userId, string clientId) {
            store.UserId      = userId;
            store.ClientId    = clientId;
            
            monitor.ClearStats();
            await monitor.TrySyncTasks();
            
            store.articles.Read().Find("xxx");
            store.customers.Read().Find("yyy");
            await store.TrySyncTasks();
            
            var result = new MonitorResult {
                users       = monitor.users.QueryAll(),
                clients     = monitor.clients.QueryAll(),
                user        = monitor.users.Read().Find(new JsonKey(userId)),
                client      = monitor.clients.Read().Find(new JsonKey(clientId)),
                hosts       = monitor.hosts.QueryAll(),
                sync        = await monitor.TrySyncTasks()
            };
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

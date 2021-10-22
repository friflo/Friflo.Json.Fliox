// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Auth;
using Friflo.Json.Fliox.DB.Client;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Host.Monitor;
using Friflo.Json.Fliox.DB.Remote;
using Friflo.Json.Fliox.DB.UserAuth;
using Friflo.Json.Fliox.Mapper;
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
            using (var _                = UtilsInternal.SharedPools) // for LeakTestsFixture
            using (var database         = new FileDatabase(CommonUtils.GetBasePath() + "assets~/DB/PocStore"))
            using (var hub          	= new DatabaseHub(database, HostName))
            using (var monitorDB        = new MonitorDatabase(hub)) {
                hub.AddExtensionDB(monitorDB);
                await AssertNoAuthMonitoringDB  (hub, monitorDB);
                await AssertAuthMonitoringDB    (hub, monitorDB, hub);
            }
        }
        
        [Test]
        public static async Task TestMonitoringLoopback() {
            using (var _                = UtilsInternal.SharedPools) // for LeakTestsFixture
            using (var database         = new FileDatabase(CommonUtils.GetBasePath() + "assets~/DB/PocStore"))
            using (var hub          	= new DatabaseHub(database, HostName))
            using (var monitor          = new MonitorDatabase(hub))
            using (var loopbackHub      = new LoopbackHub(hub)) {
                hub.AddExtensionDB(monitor);
                var monitorDB = loopbackHub.AddExtensionDB(MonitorDatabase.Name);
                await AssertNoAuthMonitoringDB  (loopbackHub, monitorDB);
                await AssertAuthMonitoringDB    (loopbackHub, monitorDB, hub);
            }
        }
        
        [Test]
        public static async Task TestMonitoringHttp() {
            using (var _                = UtilsInternal.SharedPools) // for LeakTestsFixture
            using (var database         = new FileDatabase(CommonUtils.GetBasePath() + "assets~/DB/PocStore"))
            using (var hub          	= new DatabaseHub(database, HostName))
            using (var hostDatabase     = new HttpHostHub(hub))
            using (var server           = new HttpListenerHost("http://+:8080/", hostDatabase)) 
            using (var monitor          = new MonitorDatabase(hub))
            using (var clientHub        = new HttpClientHub("http://localhost:8080/")) {
                hub.AddExtensionDB(monitor);
                await RunServer(server, async () => {
                    var monitorDB   = clientHub.AddExtensionDB(MonitorDatabase.Name);
                    await AssertNoAuthMonitoringDB  (clientHub, monitorDB);
                    await AssertAuthMonitoringDB    (clientHub, monitorDB, hub);
                });
            }
        }
        
        private static async Task AssertAuthMonitoringDB(DatabaseHub storeDB, EntityDatabase monitorDB, DatabaseHub database) {
            using (var userDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets~/DB/UserStore"))
            using (var userHub         	= new DatabaseHub(userDatabase))
            using (var userStore        = new UserStore (userHub, UserStore.AuthenticationUser, null))
            using (var _                = new UserDatabaseHandler   (userHub)) {
                database.Authenticator  = new UserAuthenticator(userStore, userStore);
                await AssertAuthSuccessMonitoringDB (storeDB, monitorDB);
                await AssertAuthFailedMonitoringDB  (storeDB, monitorDB);
            }
        }

        private  static async Task AssertNoAuthMonitoringDB(DatabaseHub hub, EntityDatabase monitorDb) {
            const string userId     = "poc-user";
            const string clientId   = "poc-client"; 
            using (var store    = new PocStore(hub, null))
            using (var monitor  = new MonitorStore(monitorDb, store)) {
                var result = await Monitor(store, monitor, userId, clientId);
                AssertNoAuthResult(result);
                
                // as clearing monitor stats subsequent call has same result
                result = await Monitor(store, monitor, userId, clientId);
                AssertNoAuthResult(result);
                
                await AssertMonitoringErrors(monitor);
            }
        }

        private  static async Task AssertAuthSuccessMonitoringDB(DatabaseHub hub, EntityDatabase monitorDb) {
            const string userId     = "admin";
            const string clientId   = "admin-client"; 
            using (var store    = new PocStore(hub, null))
            using (var monitor  = new MonitorStore(monitorDb, store)) {
                store.SetToken("admin-token");
                var result = await Monitor(store, monitor, userId, clientId);
                AssertAuthResult(result);
                
                // as clearing monitor stats subsequent call has same result
                result = await Monitor(store, monitor, userId, clientId);
                AssertAuthResult(result);
            }
        }
        
        private  static async Task AssertAuthFailedMonitoringDB(DatabaseHub hub, EntityDatabase monitorDb) {
            const string userId     = "admin";
            const string clientId   = "admin-xxx"; 
            using (var store    = new PocStore(hub, null))
            using (var monitor  = new MonitorStore(monitorDb, store)) {
                store.SetToken("invalid");
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
            AreEqual("{'id':'poc-user','clients':['poc-client'],'counts':[{'db':'monitor','requests':1,'tasks':1},{'db':'default','requests':1,'tasks':2}]}",       users[new JsonKey("poc-user")].ToString());
            AreEqual(2, users.Count);
                
            AreEqual("{'id':'poc-client','user':'poc-user','counts':[{'db':'monitor','requests':1,'tasks':1},{'db':'default','requests':1,'tasks':2}]}",            clients[new JsonKey("poc-client")].ToString());
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
            AreEqual("{'id':'admin','clients':['admin-client'],'counts':[{'db':'monitor','requests':1,'tasks':1},{'db':'default','requests':1,'tasks':2}]}",        users[new JsonKey("admin")].ToString());
                
            AreEqual("{'id':'admin-client','user':'admin','counts':[{'db':'monitor','requests':1,'tasks':1},{'db':'default','requests':1,'tasks':2}]}",             clients[new JsonKey("admin-client")].ToString());
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
            await monitor.TryExecuteTasksAsync();
            AreEqual("InvalidTask ~ MonitorDatabase does not support task: 'create'",   createUser.Error.Message);
            AreEqual("InvalidTask ~ MonitorDatabase does not support task: 'delete'",   deleteUser.Error.Message);
        }
        
        private  static async Task<MonitorResult> Monitor(PocStore store, MonitorStore monitor, string userId, string clientId) {
            var userKey    = new JsonKey(userId);
            var clientKey  = new JsonKey(clientId);
            store.SetUserClient(userKey, clientKey);
            
            monitor.SendMessage(MonitorStore.ClearStats);
            await monitor.ExecuteTasksAsync();
            
            store.articles.Read().Find("xxx");
            store.customers.Read().Find("yyy");
            await store.TryExecuteTasksAsync();
            
            var result = new MonitorResult {
                users       = monitor.users.QueryAll(),
                clients     = monitor.clients.QueryAll(),
                user        = monitor.users.Read().Find(userKey),
                client      = monitor.clients.Read().Find(clientKey),
                hosts       = monitor.hosts.QueryAll(),
                sync        = await monitor.TryExecuteTasksAsync()
            };
            return result;
        }
        
        internal class MonitorResult {
            internal    ExecuteTasksResult                      sync;
            internal    QueryTask<JsonKey,  UserInfo>   users;
            internal    QueryTask<JsonKey,  ClientInfo> clients;
            internal    Find<JsonKey,       UserInfo>   user;
            internal    Find<JsonKey,       ClientInfo> client;
            internal    QueryTask<JsonKey,  HostInfo>   hosts;
        }
    }
}

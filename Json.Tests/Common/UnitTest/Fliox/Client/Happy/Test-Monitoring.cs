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
        private static readonly DbOpt DbOpt = new DbOpt(hostName: "Test");
        
        [Test]
        public static async Task TestMonitoringFile() {
            using (var _                = SingletonUtils.SharedPools) // for LeakTestsFixture
            using (var fileDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets~/DB/PocStore", DbOpt))
            using (var monitorDB        = new MonitorDatabase(fileDatabase)) {
                fileDatabase.AddExtensionDB(monitorDB);
                await AssertNoAuthMonitoringDB  (fileDatabase, monitorDB);
                await AssertAuthMonitoringDB    (fileDatabase, monitorDB, fileDatabase);
            }
        }
        
        [Test]
        public static async Task TestMonitoringLoopback() {
            using (var _                = SingletonUtils.SharedPools) // for LeakTestsFixture
            using (var fileDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets~/DB/PocStore", DbOpt))
            using (var monitor          = new MonitorDatabase(fileDatabase))
            using (var loopbackDatabase = new LoopbackDatabase(fileDatabase)) {
                fileDatabase.AddExtensionDB(monitor);
                var monitorDB = loopbackDatabase.AddExtensionDB(MonitorDatabase.Name);
                await AssertNoAuthMonitoringDB  (loopbackDatabase, monitorDB);
                await AssertAuthMonitoringDB    (loopbackDatabase, monitorDB, fileDatabase);
            }
        }
        
        [Test]
        public static async Task TestMonitoringHttp() {
            using (var _                = SingletonUtils.SharedPools) // for LeakTestsFixture
            using (var fileDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets~/DB/PocStore", DbOpt))
            using (var hostDatabase     = new HttpHostDatabase(fileDatabase, "http://+:8080/")) 
            using (var monitor          = new MonitorDatabase(fileDatabase))
            using (var remoteDatabase   = new HttpClientDatabase("http://localhost:8080/")) {
                fileDatabase.AddExtensionDB(monitor);
                await RunRemoteHost(hostDatabase, async () => {
                    var monitorDB   = remoteDatabase.AddExtensionDB(MonitorDatabase.Name);
                    await AssertNoAuthMonitoringDB  (remoteDatabase, monitorDB);
                    await AssertAuthMonitoringDB    (remoteDatabase, monitorDB, fileDatabase);
                });
            }
        }
        
        private static async Task AssertAuthMonitoringDB(EntityDatabase storeDB, EntityDatabase monitorDB, EntityDatabase database) {
            using (var userDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets~/DB/UserStore"))
            using (var userStore        = new UserStore (userDatabase, UserStore.AuthenticationUser, null))
            using (var _                = new UserDatabaseHandler   (userDatabase)) {
                database.Authenticator  = new UserAuthenticator(userStore, userStore);
                await AssertAuthSuccessMonitoringDB (storeDB, monitorDB);
                await AssertAuthFailedMonitoringDB  (storeDB, monitorDB);
            }
        }

        private  static async Task AssertNoAuthMonitoringDB(EntityDatabase database, EntityDatabase monitorDb) {
            const string userId     = "poc-user";
            const string clientId   = "poc-client"; 
            using (var store    = new PocStore(database, null))
            using (var monitor  = new MonitorStore(monitorDb, store)) {
                var result = await Monitor(store, monitor, userId, clientId);
                AssertNoAuthResult(result);
                
                // as clearing monitor stats subsequent call has same result
                result = await Monitor(store, monitor, userId, clientId);
                AssertNoAuthResult(result);
                
                await AssertMonitoringErrors(monitor);
            }
        }

        private  static async Task AssertAuthSuccessMonitoringDB(EntityDatabase database, EntityDatabase monitorDb) {
            const string userId     = "admin";
            const string clientId   = "admin-client"; 
            using (var store    = new PocStore(database, null))
            using (var monitor  = new MonitorStore(monitorDb, store)) {
                store.SetToken("admin-token");
                var result = await Monitor(store, monitor, userId, clientId);
                AssertAuthResult(result);
                
                // as clearing monitor stats subsequent call has same result
                result = await Monitor(store, monitor, userId, clientId);
                AssertAuthResult(result);
            }
        }
        
        private  static async Task AssertAuthFailedMonitoringDB(EntityDatabase database, EntityDatabase monitorDb) {
            const string userId     = "admin";
            const string clientId   = "admin-xxx"; 
            using (var store    = new PocStore(database, null))
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
            await monitor.TrySync();
            AreEqual("InvalidTask ~ MonitorDatabase does not support task: 'create'",   createUser.Error.Message);
            AreEqual("InvalidTask ~ MonitorDatabase does not support task: 'delete'",   deleteUser.Error.Message);
        }
        
        private  static async Task<MonitorResult> Monitor(PocStore store, MonitorStore monitor, string userId, string clientId) {
            var userKey    = new JsonKey(userId);
            var clientKey  = new JsonKey(clientId);
            store.SetUserClient(userKey, clientKey);
            
            monitor.SendMessage(MonitorStore.ClearStats);
            await monitor.Sync();
            
            store.articles.Read().Find("xxx");
            store.customers.Read().Find("yyy");
            await store.TrySync();
            
            var result = new MonitorResult {
                users       = monitor.users.QueryAll(),
                clients     = monitor.clients.QueryAll(),
                user        = monitor.users.Read().Find(userKey),
                client      = monitor.clients.Read().Find(clientKey),
                hosts       = monitor.hosts.QueryAll(),
                sync        = await monitor.TrySync()
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

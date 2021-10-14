// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Auth;
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
        [Test]
        public static async Task TestMonitoringFile() {
            using (var _                = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets~/DB/PocStore"))
            using (var monitorMemory    = new MemoryDatabase())
            using (var monitorDb        = new MonitorDatabase(monitorMemory, fileDatabase)) {
                await AssertMonitoringDB (fileDatabase, monitorDb, "poc-user", "poc-client", false);
                
                await RunWithUserAuth(fileDatabase, async () => {
                    await AssertMonitoringDB (fileDatabase, monitorDb, "admin", "admin-client", true);
                });
            }
        }
        
        [Test]
        public static async Task TestMonitoringHttp() {
            using (var _                = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets~/DB/PocStore"))
            using (var hostDatabase     = new HttpHostDatabase(fileDatabase, "http://+:8080/"))
            using (var remoteDatabase   = new HttpClientDatabase("http://localhost:8080/")) {
                await RunRemoteHost(hostDatabase, async () => {
                    var remoteMonitor   = new ExtensionDatabase(remoteDatabase, MonitorDatabase.Name);
                    await AssertMonitoringDB(remoteDatabase, remoteMonitor, "poc-user", "poc-client", false);
                    
                    await RunWithUserAuth(fileDatabase, async () => {
                        await AssertMonitoringDB (fileDatabase, remoteMonitor, "admin", "admin-client", true);
                    });
                });
            }
        }
        
        private static async Task RunWithUserAuth(EntityDatabase database, Func<Task> action) {
            using (var userDatabase    = new FileDatabase(CommonUtils.GetBasePath() + "assets~/DB/UserStore"))
            using (var userStore       = new UserStore (userDatabase, UserStore.AuthenticationUser, null))
            using (var _                = new UserDatabaseHandler   (userDatabase)) {
                database.authenticator = new UserAuthenticator(userStore, userStore);
                await action();
            }
        }

        private  static async Task AssertMonitoringDB(EntityDatabase database, EntityDatabase monitorDb, string userId, string clientId, bool userAuth) {
            using (var store    = new PocStore(database, null))
            using (var monitor  = new MonitorStore(monitorDb, TestGlobals.typeStore)) {
                store.SetToken("admin-token");
                await AssertMonitoring(store, monitor, userId, clientId, userAuth);
                // as clearing monitor stats subsequent call has same result
                await AssertMonitoring(store, monitor, userId, clientId, userAuth); 
                await AssertMonitoringErrors(monitor);
            }
        }
        
        private  static async Task AssertMonitoringErrors(MonitorStore monitor) {
            var deleteUser      = monitor.users.Delete(new JsonKey("123"));
            var createUser      = monitor.users.Create(new UserInfo{id = new JsonKey("abc")});
            await monitor.TrySync();
            AreEqual("InvalidTask ~ MonitorDatabase does not support task: 'create'",   createUser.Error.Message);
            AreEqual("InvalidTask ~ MonitorDatabase does not support task: 'delete'",   deleteUser.Error.Message);
        }
        
        private  static async Task AssertMonitoring(PocStore store, MonitorStore monitor, string userId, string clientId, bool userAuth) {
            var userKey    = new JsonKey(userId);
            var clientKey  = new JsonKey(clientId);
            store.SetUserClient(userKey, clientKey);
            
            monitor.SendMessage(MonitorStore.ClearStats);
            await monitor.Sync();
            
            store.articles.Read().Find("xxx");
            store.customers.Read().Find("yyy");
            await store.Sync();
            
            var allUsers        = monitor.users.QueryAll();
            var allClients      = monitor.clients.QueryAll();
            var findPocUser     = monitor.users.Read().Find(userKey);
            var findUserClient  = monitor.clients.Read().Find(clientKey);

            await monitor.Sync();
            
            var users       = allUsers.Results;
            var clients     = allClients.Results;
            var user        = users     [userKey]; 
            var anonymous   = users     [User.AnonymousId];
            var client      = clients   [clientKey];
            
            NotNull(findPocUser.Result);
            NotNull(findUserClient.Result);
            
            if (userAuth) {
                AreEqual("{'id':'anonymous','clients':[],'stats':[]}",                                          anonymous.ToString());
                AreEqual("{'id':'admin','clients':['admin-client'],'stats':[{'requests':1,'tasks':2}]}",        user.ToString());
                
                AreEqual("{'id':'admin-client','user':'admin','stats':[{'requests':1,'tasks':2}]}",             client.ToString());
            } else {
                AreEqual("{'id':'anonymous','clients':[],'stats':[{'db':'monitor','requests':1,'tasks':1}]}",   anonymous.ToString());
                AreEqual("{'id':'poc-user','clients':['poc-client'],'stats':[{'requests':1,'tasks':2}]}",       user.ToString());
                AreEqual(2, users.Count);
                
                AreEqual("{'id':'poc-client','user':'poc-user','stats':[{'requests':1,'tasks':2}]}",            client.ToString());
                AreEqual(1, clients.Count);
            }
        }
    }
}

// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

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
                await AssertMonitoringDB (fileDatabase, monitorDb);
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
                    await AssertMonitoringDB(remoteDatabase, remoteMonitor);
                });
            }
        }

        private  static async Task AssertMonitoringDB(EntityDatabase database, EntityDatabase monitorDb) {
            using (var store    = new PocStore(database, null))
            using (var monitor  = new MonitorStore(monitorDb, TestGlobals.typeStore)) {
                await AssertMonitoring(store, monitor);
                await AssertMonitoring(store, monitor);
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
        
        private  static async Task AssertMonitoring(PocStore store, MonitorStore monitor) {
            var user    = new JsonKey("poc-user");
            var client  = new JsonKey("poc-client");
            store.SetUserClient(user, client);
            
            monitor.SendMessage(MonitorStore.ClearStats);
            await monitor.Sync();
            
            store.articles.Read().Find("xxx");
            store.customers.Read().Find("yyy");
            await store.Sync();
            
            var allUsers        = monitor.users.QueryAll();
            var allClients      = monitor.clients.QueryAll();
            var findPocUser     = monitor.users.Read().Find(user);
            var findUserClient  = monitor.clients.Read().Find(client);

            await monitor.Sync();
            
            var users       = allUsers.Results;
            var clients     = allClients.Results;
            var pocUser     = users     [user]; 
            var anonymous   = users     [User.AnonymousId];
            var userClient  = clients   [client];
            
            NotNull(findPocUser.Result);
            NotNull(findUserClient.Result);
            
            AreEqual("{'id':'anonymous','clients':[],'stats':[{'db':'monitor','requests':1,'tasks':1}]}", anonymous.ToString());
            AreEqual("{'id':'poc-user','clients':['poc-client'],'stats':[{'requests':1,'tasks':2}]}",   pocUser.ToString());
            AreEqual(2, users.Count);
            
            AreEqual("{'id':'poc-client','user':'poc-user','stats':[{'requests':1,'tasks':2}]}",        userClient.ToString());
            AreEqual(1, clients.Count);
        }

        private static UserAuthenticator CreateUserAuthenticator () {
            var userDatabase    = new FileDatabase("./Json.Tests/assets~/DB/UserStore");
            var userStore       = new UserStore (userDatabase, UserStore.AuthenticationUser, null);
            var _               = new UserDatabaseHandler   (userDatabase);
            return new UserAuthenticator(userStore, userStore);
        }
    }
}

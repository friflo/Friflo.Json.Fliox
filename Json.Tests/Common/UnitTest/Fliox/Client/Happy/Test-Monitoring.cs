// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
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
            using (var store    = new PocStore(database, "poc-user", "poc-client"))
            using (var monitor  = new MonitorStore(monitorDb, TestGlobals.typeStore)) {
                await AssertMonitoringStore(store, monitor);
            }
        }
        
        private  static async Task AssertMonitoringStore(PocStore store, MonitorStore monitor) {
            store.articles.Read().Find("xxx");
            await store.Sync();
            
            var allUsers        = monitor.users.QueryAll();
            var allClients      = monitor.clients.QueryAll();
            var findPocUser     = monitor.users.Read().Find(new JsonKey("poc-user"));
            var findUserClient  = monitor.clients.Read().Find(new JsonKey("poc-client"));

            await monitor.Sync();
            
            var users       = allUsers.Results;
            var clients     = allClients.Results;
            var pocUser     = users     [new JsonKey("poc-user")]; 
            var anonymous   = users     [new JsonKey("anonymous")];
            var userClient  = clients   [new JsonKey("poc-client")];
            
            NotNull(findPocUser.Result);
            NotNull(findUserClient.Result);
            
            AreEqual("{'id':'anonymous','clients':[],'stats':[]}",                                      anonymous.ToString());
            AreEqual("{'id':'poc-user','clients':['poc-client'],'stats':[{'requests':1,'tasks':1}]}",   pocUser.ToString());
            AreEqual(2, users.Count);
            
            AreEqual("{'id':'poc-client','user':'poc-user','stats':[{'requests':1,'tasks':1}]}",        userClient.ToString());
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

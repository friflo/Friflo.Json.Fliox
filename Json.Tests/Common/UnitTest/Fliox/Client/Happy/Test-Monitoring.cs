// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Host.Monitor;
using Friflo.Json.Fliox.DB.UserAuth;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public partial class TestStore
    {
        [Test]
        public static async Task TestMonitoring() {
            using (var _                = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets~/DB/PocStore"))
            using (var monitorMemory    = new MemoryDatabase())
            using (var monitorDb        = new MonitorDatabase(monitorMemory, fileDatabase)) {
                await AssertMonitoring (fileDatabase, monitorDb);
            }
        }
        
        private  static async Task AssertMonitoring(EntityDatabase database, EntityDatabase monitorDb) {
            using (var store    = new PocStore(database, "poc-user", "poc-user-client"))
            using (var monitor  = new MonitorStore(monitorDb, TestGlobals.typeStore)) {
                store.articles.Read().Find("xxx");
                await store.Sync();
                
                var allUsers    = monitor.users.QueryAll();
                var allClients  = monitor.clients.QueryAll();
                await monitor.Sync();
                
                var users   = allUsers.Results;
                var clients = allClients.Results;
                var pocUser     = users     [new JsonKey("poc-user")]; 
                var userClient  = clients   [new JsonKey("poc-user-client")];
                
                AreEqual("id: poc-user, stats: (db: , requests: 1, tasks: 1)", pocUser.ToString());
                AreEqual("id: poc-user-client, user: poc-user, stats: (db: , requests: 1, tasks: 1)", userClient.ToString());
            }
        }

        private static UserAuthenticator CreateUserAuthenticator () {
            var userDatabase    = new FileDatabase("./Json.Tests/assets~/DB/UserStore");
            var userStore       = new UserStore (userDatabase, UserStore.AuthenticationUser, null);
            var _               = new UserDatabaseHandler   (userDatabase);
            return new UserAuthenticator(userStore, userStore);
        }
    }
}

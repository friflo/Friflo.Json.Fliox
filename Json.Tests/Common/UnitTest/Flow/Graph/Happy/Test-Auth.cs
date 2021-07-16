// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Database.Utils;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.UserAuth;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph.Happy
{
    public partial class TestStore
    {
        [Test] public static void TestAuth () {
            using (var _                = Pools.SharedPools) // for LeakTestsFixture
            {
                SingleThreadSynchronizationContext.Run(async () => {
                    using (var userDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets/auth"))
                    using (                       new UserDatabaseHandler   (userDatabase))
                    using (var userStore        = new UserStore(userDatabase, UserStore.AuthUser))
                    using (var database         = new MemoryDatabase()) {
                        var authenticator = new UserAuthenticator(userStore, userStore);
                        database.authenticator = authenticator;
                        database.authenticator.RegisterPredicate(nameof(TestPredicate), TestPredicate);
                        await authenticator.ValidateRoles();
                        await AssertAuthReadWrite   (database);
                        await AssertAuthMessage     (database);
                    }
                });
            }
        }
        
        private static bool TestPredicate (DatabaseTask task, MessageContext messageContext) {
            return false;
        }
        
        [Test] public static void TestAuthAccess () {
            using (var _                = Pools.SharedPools) // for LeakTestsFixture
            {
                SingleThreadSynchronizationContext.Run(async () => {
                    using (var userDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets/auth"))
                    using (var serverStore      = new UserStore             (userDatabase, UserStore.Server))
                    using (var authUserStore    = new UserStore             (userDatabase, UserStore.AuthUser))
                    using (                       new UserDatabaseHandler   (userDatabase)) {
                        // assert access to user database with different users: "Server" & "AuthUser"
                        await AssertNotAuthorized   (serverStore);
                        await AssertNotAuthorized   (authUserStore);
                        await AssertServerStore     (serverStore);
                        await AssertAuthUserStore   (authUserStore);
                    }
                });
            }
        }
        
        private static async Task AssertNotAuthorized(UserStore store) {
            var allCredentials  = store.credentials.QueryAll();
            var createTask      = store.credentials.Create(new UserCredential{ id="create-id" });
            var updateTask      = store.credentials.Update(new UserCredential{ id="update-id" });
            await store.TrySync();
            
            AreEqual("PermissionDenied ~ not authorized", allCredentials.Error.Message);
            AreEqual("PermissionDenied ~ not authorized", createTask.Error.Message);
            AreEqual("PermissionDenied ~ not authorized", updateTask.Error.Message);
        }
        
        private static async Task AssertServerStore(UserStore store) {
            var credTask        = store.credentials.Read().Find("user-mutate");
            await store.TrySync();
            
            var cred = credTask.Result;
            AreEqual("user-mutate-token", cred.token);
        }
        
        private static async Task AssertAuthUserStore(UserStore store) {
            var credTask        = store.credentials.Read().Find("user-mutate");
            await store.TrySync();
            
            AreEqual("PermissionDenied ~ not authorized", credTask.Error.Message);
        }

        private static async Task AssertAuthReadWrite(EntityDatabase database) {
            using (var nullUser         = new PocStore(database, null))
            using (var unknownUser      = new PocStore(database, "unknown"))
            using (var mutateUser       = new PocStore(database, "user-mutate"))
            using (var readUser         = new PocStore(database, "user-read"))
            {
                ReadWriteTasks tasks;
                var newArticle = new Article{ id="new-article" };
                
                // test: clientId == null
                tasks = new ReadWriteTasks(nullUser, newArticle);
                var sync = await nullUser.TrySync();
                AreEqual(2, sync.failed.Count);
                AreEqual("PermissionDenied ~ not authorized (user authentication requires clientId)", tasks.findArticle.Error.Message);
                AreEqual("PermissionDenied ~ not authorized (user authentication requires clientId)", tasks.updateArticles.Error.Message);
                
                // test: token ==  null
                unknownUser.SetToken(null);
                tasks = new ReadWriteTasks(unknownUser, newArticle);
                sync = await unknownUser.TrySync();
                AreEqual(2, sync.failed.Count);
                AreEqual("PermissionDenied ~ not authorized (user authentication requires token)", tasks.findArticle.Error.Message);
                AreEqual("PermissionDenied ~ not authorized (user authentication requires token)", tasks.updateArticles.Error.Message);
                
                // test: invalid token 
                unknownUser.SetToken("some token");
                tasks = new ReadWriteTasks(unknownUser, newArticle);
                sync = await unknownUser.TrySync();
                AreEqual(2, sync.failed.Count);
                AreEqual("PermissionDenied ~ not authorized (invalid user token)", tasks.findArticle.Error.Message);
                AreEqual("PermissionDenied ~ not authorized (invalid user token)", tasks.updateArticles.Error.Message);
                
                // test: allow readOnly & mutate 
                mutateUser.SetToken("user-mutate-token");
                tasks = new ReadWriteTasks(mutateUser, newArticle);
                sync = await mutateUser.TrySync();
                AreEqual(0, sync.failed.Count);
                IsTrue(tasks.Success);
                
                // test: store already authorized 
                tasks = new ReadWriteTasks(mutateUser, newArticle);
                sync = await mutateUser.TrySync();
                AreEqual(0, sync.failed.Count);
                IsTrue(tasks.Success);

                // test: allow read
                readUser.SetToken("user-read-token");
                tasks = new ReadWriteTasks(readUser, newArticle);
                sync = await readUser.TrySync();
                AreEqual(1, sync.failed.Count);
                AreEqual("PermissionDenied ~ not authorized", tasks.updateArticles.Error.Message);
            }
        }
        
        private static async Task AssertAuthMessage(EntityDatabase database) {
            using (var denyUser      = new PocStore(database, "user-deny"))
            using (var messageUser   = new PocStore(database, "user-message")) {
                // test: deny message
                denyUser.SetToken("user-deny-token");
                var message = denyUser.SendMessage("test-message");
                await denyUser.TrySync();
                AreEqual("PermissionDenied ~ not authorized", message.Error.Message);
                
                // test: allow message
                messageUser.SetToken("user-message-token");
                message = messageUser.SendMessage("test-message");
                await messageUser.TrySync();
                IsTrue(message.Success);                
            }
        }
        
        
        public class ReadWriteTasks {
            public  ReadTask<Article>       readArticles;
            public  Find<Article>           findArticle;
            public  UpdateTask<Article>     updateArticles;
            
            public ReadWriteTasks (PocStore store, Article newArticle) {
                readArticles    = store.articles.Read();
                findArticle     = readArticles.Find("some-id");
                updateArticles  = store.articles.Update(newArticle);
            }
            
            public bool Success => findArticle.Success && updateArticles.Success;
        }
    }
}

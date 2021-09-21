// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Auth.Rights;
using Friflo.Json.Fliox.DB.Graph;
using Friflo.Json.Fliox.DB.NoSQL;
using Friflo.Json.Fliox.DB.NoSQL.Event;
using Friflo.Json.Fliox.DB.NoSQL.Utils;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.DB.UserAuth;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Graph.Happy
{
    public partial class TestStore
    {
        // ----------------------------- Test authorization rights to a database -----------------------------
        [Test] public static void TestAuthRights () {
            using (var _                = Pools.SharedPools) // for LeakTestsFixture
            {
                SingleThreadSynchronizationContext.Run(async () => {
                    using (var userDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets~/DB/UserStore"))
                    using (                       new UserDatabaseHandler   (userDatabase)) // authorize access to UserStore db and handle AuthenticateUser command
                    using (var userStore        = new UserStore(userDatabase, UserStore.AuthUser))
                    using (var database         = new MemoryDatabase())
                    using (var eventBroker      = new EventBroker(false)) // require for SubscribeMessage() and SubscribeChanges()
                    {
                        var authenticator = new UserAuthenticator(userStore, userStore);
                        authenticator.RegisterPredicate(TestPredicate);
                        database.authenticator  = authenticator;
                        database.eventBroker    = eventBroker;
                        await authenticator.ValidateRoles();
                        await AssertNotAuthenticated        (database);
                        await AssertAuthAccessOperations    (database);
                        await AssertAuthAccessSubscriptions (database);
                        await AssertAuthMessage             (database);
                    }
                });
            }
        }
        
        /// <summary>
        /// A predicate function enables custom authorization via code, which cannot be expressed by one of the
        /// provided <see cref="Right"/> implementations.
        /// If called its parameters are intended to filter the aspired condition and return true if task execution is granted.
        /// To reject task execution it returns false.
        /// </summary>
        private static bool TestPredicate (SyncRequestTask task, MessageContext messageContext) {
            switch (task) {
                case ReadEntitiesList read:
                    return read.container   == nameof(PocStore.articles);
                case UpsertEntities   upsert:
                    return upsert.container == nameof(PocStore.articles);
            }
            return false;
        }

        // Test cases where authentication fails.
        // In these cases error messages contain details about authentication problems. 
        private static async Task AssertNotAuthenticated(EntityDatabase database) {
            var newArticle = new Article{ id="new-article" };
            using (var nullUser         = new PocStore(database, null)) {
                // test: clientId == null
                var tasks = new ReadWriteTasks(nullUser, newArticle);
                var sync = await nullUser.TrySync();
                AreEqual(2, sync.failed.Count);
                AreEqual("PermissionDenied ~ not authorized (user authentication requires clientId)", tasks.findArticle.Error.Message);
                AreEqual("PermissionDenied ~ not authorized (user authentication requires clientId)", tasks.upsertArticles.Error.Message);
            }
            using (var unknownUser      = new PocStore(database, "unknown")) {
                // test: token ==  null
                unknownUser.SetToken(null);
                
                var tasks = new ReadWriteTasks(unknownUser, newArticle);
                var sync = await unknownUser.TrySync();
                AreEqual(2, sync.failed.Count);
                AreEqual("PermissionDenied ~ not authorized (user authentication requires token)", tasks.findArticle.Error.Message);
                AreEqual("PermissionDenied ~ not authorized (user authentication requires token)", tasks.upsertArticles.Error.Message);
                
                // test: invalid token 
                unknownUser.SetToken("some token");
                await unknownUser.TrySync(); // authenticate to simplify debugging below
                    
                tasks = new ReadWriteTasks(unknownUser, newArticle);
                sync = await unknownUser.TrySync();
                AreEqual(2, sync.failed.Count);
                AreEqual("PermissionDenied ~ not authorized (invalid user token)", tasks.findArticle.Error.Message);
                AreEqual("PermissionDenied ~ not authorized (invalid user token)", tasks.upsertArticles.Error.Message);
            }
        }

        /// test authorization of read and write operations on a container
        private static async Task AssertAuthAccessOperations(EntityDatabase database) {
            var newArticle = new Article{ id="new-article" };
            using (var mutateUser       = new PocStore(database, "user-database")) {
                // test: allow read & mutate 
                mutateUser.SetToken("user-database-token");
                await mutateUser.TrySync(); // authenticate to simplify debugging below
                
                var tasks = new ReadWriteTasks(mutateUser, newArticle);
                var sync = await mutateUser.TrySync();
                AreEqual(0, sync.failed.Count);
                IsTrue(tasks.Success);
            }
            using (var readUser         = new PocStore(database, "user-task")) {
                // test: allow read
                readUser.SetToken("user-task-token");
                await readUser.TrySync(); // authenticate to simplify debugging below
                
                var tasks = new ReadWriteTasks(readUser, newArticle);
                var sync = await readUser.TrySync();
                AreEqual(1, sync.failed.Count);
                AreEqual("PermissionDenied ~ not authorized", tasks.upsertArticles.Error.Message);
            }
            using (var readPredicate         = new PocStore(database, "user-predicate")) {
                // test: allow by predicate function
                readPredicate.SetToken("user-predicate-token");
                await readPredicate.TrySync(); // authenticate to simplify debugging below
                
                _ = new ReadWriteTasks(readPredicate, newArticle);
                var sync = await readPredicate.TrySync();
                AreEqual(0, sync.failed.Count);
            }
        }
        
        /// test authorization of subscribing to container changes. E.g. create, upsert, delete & patch.
        private static async Task AssertAuthAccessSubscriptions(EntityDatabase database) {
            using (var mutateUser       = new PocStore(database, "user-deny")) {
                mutateUser.SetToken("user-deny-token");
                await mutateUser.TrySync(); // authenticate to simplify debugging below

                var articleChanges = mutateUser.articles.SubscribeChanges(new [] {Change.upsert});
                await mutateUser.TrySync();
                AreEqual("PermissionDenied ~ not authorized", articleChanges.Error.Message);
                
                var articleDeletes = mutateUser.articles.SubscribeChanges(new [] {Change.delete});
                await mutateUser.TrySync();
                AreEqual("PermissionDenied ~ not authorized", articleDeletes.Error.Message);
            }
            using (var mutateUser       = new PocStore(database, "user-database")) {
                mutateUser.SetToken("user-database-token");
                await mutateUser.TrySync(); // authenticate to simplify debugging below

                var articleChanges = mutateUser.articles.SubscribeChanges(new [] {Change.upsert});
                var sync = await mutateUser.TrySync();
                AreEqual(0, sync.failed.Count);
                IsTrue(articleChanges.Success);
                
                var articleDeletes = mutateUser.articles.SubscribeChanges(new [] {Change.delete});
                await mutateUser.TrySync();
                AreEqual("PermissionDenied ~ not authorized", articleDeletes.Error.Message);
            }
        }
        
        /// test authorization of sending messages and subscriptions to messages. Commands are messages too.
        private static async Task AssertAuthMessage(EntityDatabase database) {
            using (var denyUser      = new PocStore(database, "user-deny"))
            {
                // test: deny message
                denyUser.SetToken("user-deny-token");
                await denyUser.TrySync(); // authenticate to simplify debugging below
                
                var message     = denyUser.SendMessage("test-message");
                var subscribe   = denyUser.SubscribeMessage("test-subscribe", msg => {});
                await denyUser.TrySync();
                AreEqual("PermissionDenied ~ not authorized", message.Error.Message);
                AreEqual("PermissionDenied ~ not authorized", subscribe.Error.Message);
            }
            using (var messageUser   = new PocStore(database, "user-message")){
                // test: allow message
                messageUser.SetToken("user-message-token");
                await messageUser.TrySync(); // authenticate to simplify debugging below
                
                var message     = messageUser.SendMessage("test-message");
                var subscribe   = messageUser.SubscribeMessage("test-subscribe", msg => {});
                await messageUser.TrySync();
                IsTrue(message.Success);
                IsTrue(subscribe.Success);
            }
        }

        /// Composition of read and write tasks
        public class ReadWriteTasks {
            public readonly     Find<string, Article>           findArticle;
            public readonly     UpsertTask<Article>             upsertArticles;

            
            public ReadWriteTasks (PocStore store, Article newArticle) {
                var readArticles    = store.articles.Read();
                findArticle         = readArticles.Find("some-id");
                upsertArticles      = store.articles.Upsert(newArticle);
            }
            
            public bool Success => findArticle.Success && upsertArticles.Success;
        }
        
        // ------------------------------------- Test access to user database -------------------------------------
        [Test] public static void TestAuthUserStore () {
            using (var _                = Pools.SharedPools) // for LeakTestsFixture
            {
                SingleThreadSynchronizationContext.Run(async () => {
                    using (var userDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets~/DB/UserStore"))
                    using (var serverStore      = new UserStore             (userDatabase, UserStore.Server))
                    using (var authUserStore    = new UserStore             (userDatabase, UserStore.AuthUser))
                    using (                       new UserDatabaseHandler   (userDatabase)) {
                        // assert access to user database with different users: "Server" & "AuthUser"
                        await AssertUserStore       (serverStore);
                        await AssertUserStore       (authUserStore);
                        await AssertServerStore     (serverStore);
                        await AssertAuthUserStore   (authUserStore);
                    }
                });
            }
        }
        
        private static async Task AssertUserStore(UserStore store) {
            var allCredentials  = store.credentials.QueryAll();
            var createTask      = store.credentials.Create(new UserCredential{ id="create-id" });
            var upsertTask      = store.credentials.Upsert(new UserCredential{ id="upsert-id" });
            await store.TrySync();
            
            AreEqual("PermissionDenied ~ not authorized", allCredentials.Error.Message);
            AreEqual("PermissionDenied ~ not authorized", createTask.Error.Message);
            AreEqual("PermissionDenied ~ not authorized", upsertTask.Error.Message);
        }
        
        private static async Task AssertServerStore(UserStore store) {
            var credTask        = store.credentials.Read().Find("user-database");
            await store.TrySync();
            
            var cred = credTask.Result;
            AreEqual("user-database-token", cred.token);
        }
        
        private static async Task AssertAuthUserStore(UserStore store) {
            var credTask        = store.credentials.Read().Find("user-database");
            await store.TrySync();
            
            AreEqual("PermissionDenied ~ not authorized", credTask.Error.Message);
        }
    }
}

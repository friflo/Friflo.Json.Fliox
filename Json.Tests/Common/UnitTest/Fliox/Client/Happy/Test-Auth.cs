// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Auth.Rights;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Threading;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.UserAuth;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public partial class TestStore
    {
        // ----------------------------- Test authorization rights to a database -----------------------------
        [Test] public static void TestAuthRights () {
            using (var _                = UtilsInternal.SharedPools) // for LeakTestsFixture
            {
                SingleThreadSynchronizationContext.Run(async () => {
                    using (var userDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets~/DB/UserStore", new UserDBHandler()))
                    using (var userHub        	= new FlioxHub(userDatabase))
                    using (var userStore        = new UserStore(userHub, UserStore.AuthenticationUser))
                    using (var database         = new MemoryDatabase())
                    using (var hub          	= new FlioxHub(database))
                    using (var eventBroker      = new EventBroker(false)) // require for SubscribeMessage() and SubscribeChanges()
                    {
                        userHub.Authenticator   = new UserDatabaseAuthenticator();  // authorize access to UserStore db and handle AuthenticateUser command
                        var authenticator = new UserAuthenticator(userStore, userStore);
                        authenticator.RegisterPredicate(TestPredicate);
                        hub.Authenticator  = authenticator;
                        hub.EventBroker    = eventBroker;
                        await authenticator.ValidateRoles();
                        await AssertNotAuthenticated        (hub);
                        await AssertAuthAccessOperations    (hub);
                        await AssertAuthAccessSubscriptions (hub);
                        await AssertAuthMessage             (hub);
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
        private static bool TestPredicate (SyncRequestTask task, IPools pools) {
            switch (task) {
                case ReadEntities   read:
                    return read.container   == nameof(PocStore.articles);
                case UpsertEntities upsert:
                    return upsert.container == nameof(PocStore.articles);
            }
            return false;
        }
        
        // Test cases where authentication fails.
        // In these cases error messages contain details about authentication problems. 
        private static async Task AssertNotAuthenticated(FlioxHub hub) {
            var newArticle = new Article{ id="new-article" };
            using (var nullUser         = new PocStore(hub, null)) {
                // test: userId == null
                var tasks = new ReadWriteTasks(nullUser, newArticle);
                var sync = await nullUser.TrySyncTasks();
                AreEqual(2, sync.failed.Count);
                AreEqual("PermissionDenied ~ not authorized. user authentication requires 'user' id", tasks.findArticle.Error.Message);
                AreEqual("PermissionDenied ~ not authorized. user authentication requires 'user' id", tasks.upsertArticles.Error.Message);
            }
            using (var unknownUser      = new PocStore(hub, "unknown")) {
                // test: token ==  null
                unknownUser.SetToken(null);
                
                var tasks = new ReadWriteTasks(unknownUser, newArticle);
                var sync = await unknownUser.TrySyncTasks();
                AreEqual(2, sync.failed.Count);
                AreEqual("PermissionDenied ~ not authorized. user authentication requires 'token'", tasks.findArticle.Error.Message);
                AreEqual("PermissionDenied ~ not authorized. user authentication requires 'token'", tasks.upsertArticles.Error.Message);
                
                // test: invalid token 
                unknownUser.SetToken("some token");
                await unknownUser.TrySyncTasks(); // authenticate to simplify debugging below
                    
                tasks = new ReadWriteTasks(unknownUser, newArticle);
                sync = await unknownUser.TrySyncTasks();
                AreEqual(2, sync.failed.Count);
                AreEqual("PermissionDenied ~ not authorized. Authentication failed", tasks.findArticle.Error.Message);
                AreEqual("PermissionDenied ~ not authorized. Authentication failed", tasks.upsertArticles.Error.Message);
            }
        }

        /// test authorization of read and write operations on a container
        private static async Task AssertAuthAccessOperations(FlioxHub hub) {
            var newArticle = new Article{ id="new-article" };
            using (var mutateUser       = new PocStore(hub, "user-database")) {
                // test: allow read & mutate 
                mutateUser.SetToken("user-database-token");
                await mutateUser.TrySyncTasks(); // authenticate to simplify debugging below
                
                var tasks = new ReadWriteTasks(mutateUser, newArticle);
                var sync = await mutateUser.TrySyncTasks();
                AreEqual(0, sync.failed.Count);
                IsTrue(tasks.Success);
                
                // test: same tasks, but changed token
                mutateUser.SetToken("arbitrary token");
                await mutateUser.TrySyncTasks(); // authenticate to simplify debugging below
                
                tasks = new ReadWriteTasks(mutateUser, newArticle);
                sync = await mutateUser.TrySyncTasks();
                
                AreEqual(2, sync.failed.Count);
                AreEqual("PermissionDenied ~ not authorized. Authentication failed", tasks.findArticle.Error.Message);
                AreEqual("PermissionDenied ~ not authorized. Authentication failed", tasks.upsertArticles.Error.Message);
                
                // test: same tasks, but cleared token
                mutateUser.SetToken(null);
                await mutateUser.TrySyncTasks(); // authenticate to simplify debugging below
                
                tasks = new ReadWriteTasks(mutateUser, newArticle);
                sync = await mutateUser.TrySyncTasks();
                AreEqual(2, sync.failed.Count);
                AreEqual("PermissionDenied ~ not authorized. user authentication requires 'token'", tasks.findArticle.Error.Message);
                AreEqual("PermissionDenied ~ not authorized. user authentication requires 'token'", tasks.upsertArticles.Error.Message);
            }
            using (var readUser         = new PocStore(hub, "user-task")) {
                // test: allow read
                readUser.SetToken("user-task-token");
                await readUser.TrySyncTasks(); // authenticate to simplify debugging below
                
                var tasks = new ReadWriteTasks(readUser, newArticle);
                var sync = await readUser.TrySyncTasks();
                AreEqual(1, sync.failed.Count);
                AreEqual("PermissionDenied ~ not authorized", tasks.upsertArticles.Error.Message);
            }
            using (var readPredicate         = new PocStore(hub, "user-predicate")) {
                // test: allow by predicate function
                readPredicate.SetToken("user-predicate-token");
                await readPredicate.TrySyncTasks(); // authenticate to simplify debugging below
                
                _ = new ReadWriteTasks(readPredicate, newArticle);
                var sync = await readPredicate.TrySyncTasks();
                AreEqual(0, sync.failed.Count);
            }
        }
        
        /// test authorization of subscribing to container changes. E.g. create, upsert, delete & patch.
        private static async Task AssertAuthAccessSubscriptions(FlioxHub hub) {
            using (var mutateUser       = new PocStore(hub, "user-deny")) {
                mutateUser.SetToken("user-deny-token");
                await mutateUser.TrySyncTasks(); // authenticate to simplify debugging below

                var articleChanges = mutateUser.articles.SubscribeChanges(new [] {Change.upsert});
                await mutateUser.TrySyncTasks();
                AreEqual("PermissionDenied ~ not authorized", articleChanges.Error.Message);
                
                var articleDeletes = mutateUser.articles.SubscribeChanges(new [] {Change.delete});
                await mutateUser.TrySyncTasks();
                AreEqual("PermissionDenied ~ not authorized", articleDeletes.Error.Message);
            }
            using (var mutateUser       = new PocStore(hub, "user-database")) {
                mutateUser.SetToken("user-database-token");
                await mutateUser.TrySyncTasks(); // authenticate to simplify debugging below

                var articleChanges = mutateUser.articles.SubscribeChanges(new [] {Change.upsert});
                var sync = await mutateUser.TrySyncTasks();
                AreEqual(0, sync.failed.Count);
                IsTrue(articleChanges.Success);
                
                var articleDeletes = mutateUser.articles.SubscribeChanges(new [] {Change.delete});
                await mutateUser.TrySyncTasks();
                AreEqual("PermissionDenied ~ not authorized", articleDeletes.Error.Message);
            }
        }
        
        /// test authorization of sending messages and subscriptions to messages. Commands are messages too.
        private static async Task AssertAuthMessage(FlioxHub hub) {
            using (var denyUser      = new PocStore(hub, "user-deny"))
            {
                // test: deny message
                denyUser.SetToken("user-deny-token");
                await denyUser.TrySyncTasks(); // authenticate to simplify debugging below
                
                var message     = denyUser.SendMessage("test-message");
                var subscribe   = denyUser.SubscribeMessage("test-subscribe", msg => {});
                await denyUser.TrySyncTasks();
                AreEqual("PermissionDenied ~ not authorized", message.Error.Message);
                AreEqual("PermissionDenied ~ not authorized", subscribe.Error.Message);
            }
            using (var messageUser   = new PocStore(hub, "user-message")){
                // test: allow message
                messageUser.SetToken("user-message-token");
                await messageUser.TrySyncTasks(); // authenticate to simplify debugging below
                
                var message     = messageUser.SendMessage("test-message");
                var subscribe   = messageUser.SubscribeMessage("test-subscribe", msg => {});
                await messageUser.TrySyncTasks();
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
        [Test] public static void TestUserStore () {
            using (var _                = UtilsInternal.SharedPools) // for LeakTestsFixture
            {
                SingleThreadSynchronizationContext.Run(async () => {
                    using (var userDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets~/DB/UserStore", new UserDBHandler()))
                    using (var userHub        	= new FlioxHub(userDatabase))
                    using (var serverStore      = new UserStore             (userHub, UserStore.Server))
                    using (var userStore        = new UserStore             (userHub, UserStore.AuthenticationUser)) {
                        userHub.Authenticator   = new UserDatabaseAuthenticator();
                        // assert access to user database with different users: "Server" & "AuthenticationUser"
                        await AssertUserStore       (serverStore);
                        await AssertUserStore       (userStore);
                        await AssertServerStore     (serverStore);
                        await AssertAuthUserStore   (userStore);
                    }
                });
            }
        }
        
        private static async Task AssertUserStore(UserStore store) {
            var allCredentials  = store.credentials.QueryAll();
            var createTask      = store.credentials.Create(new UserCredential{ id= new JsonKey("create-id") });
            var upsertTask      = store.credentials.Upsert(new UserCredential{ id= new JsonKey("upsert-id") });
            await store.TrySyncTasks();
            
            AreEqual("PermissionDenied ~ not authorized", allCredentials.Error.Message);
            AreEqual("PermissionDenied ~ not authorized", createTask.Error.Message);
            AreEqual("PermissionDenied ~ not authorized", upsertTask.Error.Message);
        }
        
        private static async Task AssertServerStore(UserStore store) {
            var credTask        = store.credentials.Read().Find(new JsonKey("user-database"));
            await store.TrySyncTasks();
            
            var cred = credTask.Result;
            AreEqual("user-database-token", cred.token);
        }
        
        private static async Task AssertAuthUserStore(UserStore store) {
            var credTask        = store.credentials.Read().Find(new JsonKey("user-database"));
            await store.TrySyncTasks();
            
            AreEqual("PermissionDenied ~ not authorized", credTask.Error.Message);
        }
    }
}

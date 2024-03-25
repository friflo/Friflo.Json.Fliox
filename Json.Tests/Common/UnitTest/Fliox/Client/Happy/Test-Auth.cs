// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.DB.UserAuth;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Auth.Rights;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Threading;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public partial class TestHappy
    {
        // ----------------------------- Test authorization rights to a database -----------------------------
        [Test] public static void TestAuthRights () {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            {
                SingleThreadSynchronizationContext.Run(async () => {
                    using (var userDatabase     = new FileDatabase(TestGlobals.UserDB, CommonUtils.GetBasePath() + "assets~/DB/user_db", UserDB.Schema, new UserDBService()))
                    using (var authenticator    = new UserAuthenticator(userDatabase, TestGlobals.Shared))
                    using (var database         = new MemoryDatabase(TestGlobals.DB))
                    using (var hub          	= new FlioxHub(database, TestGlobals.Shared))
                    using (var eventDispatcher  = new EventDispatcher(EventDispatching.Send)) // required for SubscribeMessage() and SubscribeChanges()
                    {
                        authenticator.RegisterPredicate(TestPredicate);
                        hub.Authenticator   = authenticator;
                        hub.EventDispatcher = eventDispatcher;
                        var errors          = await authenticator.ValidateUserDb(null);
                        AreEqual(new[] {
                            "role not found. role: 'missing-role' in permission: 'invalid-roles'",
                            "missing database in role: invalid-role, right: dbFull"}, errors);
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
        /// provided <see cref="TaskRight"/> implementations.
        /// If called its parameters are intended to filter the aspired condition and return true if task execution is granted.
        /// To reject task execution it returns false.
        /// </summary>
        private static bool TestPredicate (SyncRequestTask task, SyncContext syncContext) {
            switch (task) {
                case ReadEntities   read:
                    return read.container.  IsEqual(new ShortString(nameof(PocStore.articles)));
                case UpsertEntities upsert:
                    return upsert.container.IsEqual(new ShortString(nameof(PocStore.articles)));
            }
            return false;
        }
        
        // Test cases where authentication fails.
        // In these cases error messages contain details about authentication problems. 
        private static async Task AssertNotAuthenticated(FlioxHub hub) {
            var newArticle = new Article{ id="new-article" };
            using (var nullUser         = new PocStore(hub)) {
                // test: userId == null
                var tasks = new ReadWriteTasks(nullUser, newArticle);
                var sync = await nullUser.TrySyncTasks();
                AreEqual(2, sync.Failed.Count);
                AreEqual("PermissionDenied ~ not authorized. user authentication requires 'user' id", tasks.findArticle.Error.Message);
                AreEqual("PermissionDenied ~ not authorized. user authentication requires 'user' id", tasks.upsertArticles.Error.Message);
            }
            using (var unknownUser      = new PocStore(hub) { UserId = "unknown" }) {
                // test: token ==  null
                unknownUser.Token = null;
                
                var tasks = new ReadWriteTasks(unknownUser, newArticle);
                var sync = await unknownUser.TrySyncTasks();
                AreEqual(2, sync.Failed.Count);
                AreEqual("PermissionDenied ~ not authorized. user authentication requires 'token'. user: 'unknown'", tasks.findArticle.Error.Message);
                AreEqual("PermissionDenied ~ not authorized. user authentication requires 'token'. user: 'unknown'", tasks.upsertArticles.Error.Message);
                
                // test: invalid token 
                unknownUser.Token = "some token";
                await unknownUser.TrySyncTasks(); // authenticate to simplify debugging below
                    
                tasks = new ReadWriteTasks(unknownUser, newArticle);
                sync = await unknownUser.TrySyncTasks();
                AreEqual(2, sync.Failed.Count);
                AreEqual("PermissionDenied ~ not authorized. Authentication failed. user: 'unknown'", tasks.findArticle.Error.Message);
                AreEqual("PermissionDenied ~ not authorized. Authentication failed. user: 'unknown'", tasks.upsertArticles.Error.Message);
            }
        }

        /// test authorization of read and write operations on a container
        private static async Task AssertAuthAccessOperations(FlioxHub hub) {
            var newArticle = new Article{ id="new-article" };
            using (var mutateUser       = new PocStore(hub) { UserId ="test-operation"}) {
                // test: allow read & mutate 
                mutateUser.Token = "test-operation-token";
                await mutateUser.TrySyncTasks(); // authenticate to simplify debugging below
                
                var tasks = new ReadWriteTasks(mutateUser, newArticle);
                var sync = await mutateUser.TrySyncTasks();
                AreEqual(0, sync.Failed.Count);
                IsTrue(tasks.Success);
                
                // test: same tasks, but changed token
                mutateUser.Token = "arbitrary token";
                await mutateUser.TrySyncTasks(); // authenticate to simplify debugging below
                
                tasks = new ReadWriteTasks(mutateUser, newArticle);
                sync = await mutateUser.TrySyncTasks();
                
                AreEqual(2, sync.Failed.Count);
                AreEqual("PermissionDenied ~ not authorized. Authentication failed. user: 'test-operation'", tasks.findArticle.Error.Message);
                AreEqual("PermissionDenied ~ not authorized. Authentication failed. user: 'test-operation'", tasks.upsertArticles.Error.Message);
                
                // test: same tasks, but cleared token
                mutateUser.Token = null;
                await mutateUser.TrySyncTasks(); // authenticate to simplify debugging below
                
                tasks = new ReadWriteTasks(mutateUser, newArticle);
                sync = await mutateUser.TrySyncTasks();
                AreEqual(2, sync.Failed.Count);
                AreEqual("PermissionDenied ~ not authorized. user authentication requires 'token'. user: 'test-operation'", tasks.findArticle.Error.Message);
                AreEqual("PermissionDenied ~ not authorized. user authentication requires 'token'. user: 'test-operation'", tasks.upsertArticles.Error.Message);
            }
            using (var readUser         = new PocStore(hub) { UserId = "test-task"}) {
                // test: allow read
                readUser.Token = "test-task-token";
                await readUser.TrySyncTasks(); // authenticate to simplify debugging below
                
                var tasks = new ReadWriteTasks(readUser, newArticle);
                var sync = await readUser.TrySyncTasks();
                AreEqual(1, sync.Failed.Count);
                AreEqual("PermissionDenied ~ not authorized. user: 'test-task'", tasks.upsertArticles.Error.Message);
            }
            using (var readPredicate         = new PocStore(hub) { UserId = "test-predicate"}) {
                // test: allow by predicate function
                readPredicate.Token = "test-predicate-token";
                await readPredicate.TrySyncTasks(); // authenticate to simplify debugging below
                
                _ = new ReadWriteTasks(readPredicate, newArticle);
                var sync = await readPredicate.TrySyncTasks();
                AreEqual(0, sync.Failed.Count);
            }
        }
        
        /// test authorization of subscribing to container changes. E.g. create, upsert, delete and patch.
        private static async Task AssertAuthAccessSubscriptions(FlioxHub hub) {
            using (var mutateUser       = new PocStore(hub) { UserId = "test-empty", ClientId = "auth-1" }) {
                mutateUser.Token = "test-empty-token";
                await mutateUser.TrySyncTasks(); // authenticate to simplify debugging below

                var articleChanges = mutateUser.articles.SubscribeChanges(Change.upsert, (changes, context) => { });
                await mutateUser.TrySyncTasks();
                AreEqual("PermissionDenied ~ not authorized. user: 'test-empty'", articleChanges.Error.Message);
                
                var articleDeletes = mutateUser.articles.SubscribeChanges(Change.delete, (changes, context) => { });
                await mutateUser.TrySyncTasks();
                AreEqual("PermissionDenied ~ not authorized. user: 'test-empty'", articleDeletes.Error.Message);
            }
            using (var mutateUser       = new PocStore(hub) { UserId = "test-operation", ClientId = "auth-2" }) {
                mutateUser.Token = "test-operation-token";
                await mutateUser.TrySyncTasks(); // authenticate to simplify debugging below

                var articleChanges = mutateUser.articles.SubscribeChanges(Change.upsert, (changes, context) => { });
                var sync = await mutateUser.TrySyncTasks();
                AreEqual(0, sync.Failed.Count);
                IsTrue(articleChanges.Success);
                
                var articleDeletes = mutateUser.articles.SubscribeChanges(Change.delete, (changes, context) => { });
                await mutateUser.TrySyncTasks();
                AreEqual("PermissionDenied ~ not authorized. user: 'test-operation'", articleDeletes.Error.Message);
            }
        }
        
        /// test authorization of sending messages and subscriptions to messages. Commands are messages too.
        private static async Task AssertAuthMessage(FlioxHub hub) {
            using (var denyUser      = new PocStore(hub) { UserId = "test-empty", ClientId = "auth-3" })
            {
                // test: deny message
                denyUser.Token = "test-empty-token";
                await denyUser.TrySyncTasks(); // authenticate to simplify debugging below
                
                var message     = denyUser.SendMessage("test-message");
                var subscribe   = denyUser.SubscribeMessage("test-subscribe", (msg, context) => {});
                await denyUser.TrySyncTasks();
                AreEqual("PermissionDenied ~ not authorized. user: 'test-empty'", message.Error.Message);
                AreEqual("PermissionDenied ~ not authorized. user: 'test-empty'", subscribe.Error.Message);
            }
            using (var messageUser   = new PocStore(hub) { UserId = "test-message", ClientId = "auth-4" }){
                // test: allow message
                messageUser.Token = "test-message-token";
                await messageUser.TrySyncTasks(); // authenticate to simplify debugging below
                
                var message     = messageUser.SendMessage("test-message");
                var subscribe   = messageUser.SubscribeMessage("test-subscribe", (msg, context) => {});
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
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            {
                SingleThreadSynchronizationContext.Run(async () => {
                    using (var userDatabase     = new FileDatabase(TestGlobals.UserDB, CommonUtils.GetBasePath() + "assets~/DB/user_db", null, new UserDBService()))
                    using (var userHub        	= new FlioxHub  (userDatabase, TestGlobals.Shared))
                    using (var serverStore      = new UserStore (userHub) { UserId = UserDB.ID.Server })
                    using (var userStore        = new UserStore (userHub) { UserId = UserDB.ID.AuthenticationUser }) {
                        userHub.Authenticator   = new UserDatabaseAuthenticator(userDatabase.name);
                        // assert access to user database with different users: "Server" & "AuthenticationUser"
                        await AssertUserStore       (serverStore,   "Server");
                        await AssertUserStore       (userStore,     "AuthenticationUser");
                        await AssertServerStore     (serverStore);
                        await AssertAuthUserStore   (userStore);
                    }
                });        
            }
        }
        
        private static async Task AssertUserStore(UserStore store, string user) {
            var allCredentials  = store.credentials.QueryAll();
            // create credential is granted - to enable creating default admin token
            // var createTask      = store.credentials.Create(new UserCredential{ id= new ShortString("create-id") });
            var upsertTask      = store.credentials.Upsert(new UserCredential{ id= "upsert-id" });
            await store.TrySyncTasks();
            
            AreEqual($"PermissionDenied ~ not authorized. user: '{user}'", allCredentials.Error.Message);
            AreEqual($"PermissionDenied ~ not authorized. user: '{user}'", upsertTask.Error.Message);
        }
        
        private static async Task AssertServerStore(UserStore store) {
            var credTask        = store.credentials.Read().Find("test-operation");
            await store.TrySyncTasks();
            
            var cred = credTask.Result;
            AreEqual("test-operation-token", cred.token);
        }
        
        private static async Task AssertAuthUserStore(UserStore store) {
            var credTask        = store.credentials.Read().Find("test-operation");
            await store.TrySyncTasks();
            
            AreEqual("PermissionDenied ~ not authorized. user: 'AuthenticationUser'", credTask.Error.Message);
        }
    }
}

// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Database.Auth;
using Friflo.Json.Flow.Database.Utils;
using Friflo.Json.Flow.Graph;
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
                    using (var authDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets/auth"))
                    using (var userStore        = new UserStore(authDatabase, null, "userStore"))
                    using (var fileDatabase     = new MemoryDatabase()) {
                        fileDatabase.authenticator = new UserAuthenticator(userStore, new AuthorizeDeny());
                        await AssertAuth(fileDatabase);
                    }
                });
            }
        }

        private static async Task AssertAuth(EntityDatabase database) {
            using (var unknownUser      = new PocStore(database, "unknown"))
            using (var mutateUser       = new PocStore(database, "user-mutate"))
            using (var readOnlyUser     = new PocStore(database, "user-readOnly"))
            {
                Tasks tasks;
                
                mutateUser.    SetToken("user-mutate-token");
                readOnlyUser.  SetToken("user-readOnly-token");
                tasks = new Tasks(unknownUser);
                    
                var sync = await unknownUser.TrySync();
                AreEqual(2, sync.failed.Count);
                AreEqual("PermissionDenied ~ not authorized - tasks[1]", tasks.findArticle.Error.Message);
                AreEqual("PermissionDenied ~ not authorized - tasks[0]", tasks.createArticles.Error.Message);

                var _ = new Tasks(mutateUser);
                sync = await mutateUser.TrySync();
                AreEqual(0, sync.failed.Count);

                tasks = new Tasks(readOnlyUser);
                sync = await readOnlyUser.TrySync();
                AreEqual(1, sync.failed.Count);
                AreEqual("PermissionDenied ~ not authorized - tasks[0]", tasks.createArticles.Error.Message);
            }
        }
        
        
        public class Tasks {
            public  ReadTask<Article>       readArticles;
            public  Find<Article>           findArticle;
            public  CreateTask<Article>     createArticles;
            
            public Tasks (PocStore store) {
                readArticles    = store.articles.Read();
                findArticle     = readArticles.Find("some-id");
                createArticles  = store.articles.Create(new Article{ id="new-article" });
            }
        }
    }
}

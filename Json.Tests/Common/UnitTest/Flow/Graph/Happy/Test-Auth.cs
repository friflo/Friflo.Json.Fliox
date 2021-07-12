// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Database.Auth;
using Friflo.Json.Flow.Database.Utils;
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
                    using (var fileDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets/db")) {
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
                mutateUser.    SetToken("user-mutate-token");
                readOnlyUser.  SetToken("user-readOnly-token");
                    
                var sync = await unknownUser.TrySync();
                AreEqual(0, sync.failed.Count);

                sync = await mutateUser.TrySync();
                AreEqual(0, sync.failed.Count);
                
                sync = await readOnlyUser.TrySync();
                AreEqual(0, sync.failed.Count);
            }
        }
    }
}

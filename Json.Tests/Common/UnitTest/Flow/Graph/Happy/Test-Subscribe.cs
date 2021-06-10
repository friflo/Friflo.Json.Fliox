// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Tests.Common.Utils;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph.Happy
{
    public partial class TestStore
    {
        [Test] public async Task TestSubscribe      () { await TestCreate(async (store) => await AssertSubscribe ()); }
        
        class ArticleChange : ChangeListener<Article> {
            public override void CreatedEntities(List<Article> entities) {
            }
        }
        
        private static async Task AssertSubscribe() {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var messageBroker= new MessageBroker())
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var useStore     = new PocStore(fileDatabase)) {
                fileDatabase.messageBroker = messageBroker;
                var types = new HashSet<TaskType>(new [] {TaskType.create, TaskType.update, TaskType.delete, TaskType.patch});
                var subscribeArticles = useStore.articles.SubscribeAll(types, new ArticleChange());
                
                await useStore.Sync(); // -------- Sync --------
                
                IsTrue(subscribeArticles.Success);
                
                using (await TestRelationPoC.CreateStore(fileDatabase)) {

                }
            }
        }
    }
}
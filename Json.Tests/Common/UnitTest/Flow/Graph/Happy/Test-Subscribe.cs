// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Database.Event;
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

        internal class StoreChanges : ChangeListener {
            internal int changeCount;
            internal int createArticleCount;
            internal int updateArticleCount;
            internal int deleteArticleCount;
            
            public override void OnChanges (ChangesEvent changes, EntityStore store) {
                base.OnChanges(changes, store);
                var articleChanges = GetEntitySetChanges<Article>();
                changeCount++;
                createArticleCount += articleChanges.creates.Count;
                updateArticleCount += articleChanges.updates.Count;
                deleteArticleCount += articleChanges.deletes.Count;
            }
        }
        
        private static async Task AssertSubscribe() {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var eventBroker  = new EventBroker())
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var useStore     = new PocStore(fileDatabase)) {
                fileDatabase.eventBroker = eventBroker;
                var types = new HashSet<TaskType>(new [] {TaskType.create, TaskType.update, TaskType.delete, TaskType.patch});
                var storeChanges = new StoreChanges();
                useStore.SetChangeListener(storeChanges);
                var subscribeArticles = useStore.articles.SubscribeAll(types);
                
                await useStore.Sync(); // -------- Sync --------
                
                IsTrue(subscribeArticles.Success);
                
                using (await TestRelationPoC.CreateStore(fileDatabase)) { }
                AreEqual(7, storeChanges.changeCount);
                AreEqual(9, storeChanges.createArticleCount);
                AreEqual(0, storeChanges.updateArticleCount);
                AreEqual(4, storeChanges.deleteArticleCount);
            }
        }
    }
}
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
        
        private static async Task AssertSubscribe() {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var eventBroker  = new EventBroker())
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var listenDb     = new PocStore(fileDatabase)) {
                fileDatabase.eventBroker = eventBroker;
                var types = new HashSet<TaskType>(new [] {TaskType.create, TaskType.update, TaskType.delete, TaskType.patch});
                var storeChanges = new StoreChanges();
                listenDb.SetChangeListener(storeChanges);
                var subscribeArticles = listenDb.articles.SubscribeAll(types);
                
                await listenDb.Sync(); // -------- Sync --------
                
                IsTrue(subscribeArticles.Success);
                
                using (await TestRelationPoC.CreateStore(fileDatabase)) { }
                
                storeChanges.AssertCreateStoreChanges();
            }
        }
    }
    
    internal class StoreChanges : ChangeListener {
        private int onChangeCount;
        private int changeArticleCount;
        private int createArticleCount;
        private int updateArticleCount;
        private int deleteArticleCount;
            
        public override void OnChanges (ChangesEvent changes, EntityStore store) {
            base.OnChanges(changes, store);
            var articleChanges = GetEntityChanges<Article>();
            onChangeCount++;
            changeArticleCount  += articleChanges.Count;
            createArticleCount  += articleChanges.creates.Count;
            updateArticleCount  += articleChanges.updates.Count;
            deleteArticleCount  += articleChanges.deletes.Count;
        }
        
        public void AssertCreateStoreChanges() {
            AreEqual(7,  onChangeCount);
            AreEqual(13, changeArticleCount);
            AreEqual(9,  createArticleCount);
            AreEqual(0,  updateArticleCount);
            AreEqual(4,  deleteArticleCount);
        }
    }
}
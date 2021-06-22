// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Database.Event;
using Friflo.Json.Flow.Database.Utils;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.Transform;
using Friflo.Json.Tests.Common.Utils;
using UnityEngine.TestTools;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Common.Utils.AssertUtils;

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
        [UnityTest] public IEnumerator  SubscribeCoroutine() { yield return RunAsync.Await(AssertSubscribe()); }
        [Test]      public void         SubscribeAsync() { SingleThreadSynchronizationContext.Run(AssertSubscribe); }
        
        private static async Task AssertSubscribe() {
            var sc = SynchronizationContext.Current;
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var eventBroker  = new EventBroker(false))
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var listenDb     = new PocStore(fileDatabase, "listenDb")) {
                fileDatabase.eventBroker = eventBroker;
                var pocSubscriber        = await CreatePocSubscriber(listenDb, sc);
                using (var createStore = new PocStore(fileDatabase, "createStore")) {
                    var createSubscriber = await TestRelationPoC.SubscribeChanges(createStore, sc);
                    await TestRelationPoC.CreateStore(createStore);
                    createSubscriber.ProcessChanges();
                    AreEqual(0, createSubscriber.ChangeSequence);  // received no change events for changes done by itself
                }
                pocSubscriber.ProcessChanges();
                pocSubscriber.AssertCreateStoreChanges();
                AreEqual(8, pocSubscriber.ChangeSequence);           // non protected access
                AreSimilar("(creates: 9, updates: 0, deletes: 4, patches: 2)",  pocSubscriber.GetChangeInfo<Article>());  // non protected access
                await eventBroker.FinishQueues();
            }
        }
        
        private static async Task<PocSubscriber> CreatePocSubscriber (PocStore store, SynchronizationContext sc) {
            var subscriber = new PocSubscriber(store, sc);
            store.SetChangeSubscriber(subscriber);
            
            var changes = new HashSet<Change>(new [] {Change.create, Change.update, Change.delete, Change.patch});
            var subscriptions = store.SubscribeAll(changes);
                
            await store.Sync(); // -------- Sync --------

            foreach (var subscription in subscriptions) {
                IsTrue(subscription.Success);    
            }
            return subscriber;
        }
    }

    // assert expected database changes by counting the entity changes for each DatabaseContainer / EntitySet<>
    internal class PocSubscriber : ChangeSubscriber {
        private readonly    ChangeInfo<Order>       orderSum     = new ChangeInfo<Order>();
        private readonly    ChangeInfo<Customer>    customerSum  = new ChangeInfo<Customer>();
        private readonly    ChangeInfo<Article>     articleSum   = new ChangeInfo<Article>();
        private readonly    ChangeInfo<Producer>    producerSum  = new ChangeInfo<Producer>();
        private readonly    ChangeInfo<Employee>    employeeSum  = new ChangeInfo<Employee>();
        
        internal PocSubscriber (EntityStore store, SynchronizationContext synchronizationContext) : base (store, synchronizationContext) { }
            
        /// All tests using <see cref="PocSubscriber"/> are required to use "createStore" as clientId
        protected override void OnChange (ChangeEvent change) {
            AreEqual("createStore", change.clientId);
            base.OnChange(change);
            var orderChanges    = GetEntityChanges<Order>   (change);
            var customerChanges = GetEntityChanges<Customer>(change);
            var articleChanges  = GetEntityChanges<Article> (change);
            var producerChanges = GetEntityChanges<Producer>(change);
            var employeeChanges = GetEntityChanges<Employee>(change);
            
            var changeInfo = change.GetChangeInfo();
            IsTrue(changeInfo.Count > 0);

            orderSum.   AddChanges(orderChanges);
            customerSum.AddChanges(customerChanges);
            articleSum. AddChanges(articleChanges);
            producerSum.AddChanges(producerChanges);
            employeeSum.AddChanges(employeeChanges);
            
            switch (ChangeSequence) {
                case 1:
                    AreEqual("(creates: 2, updates: 0, deletes: 1, patches: 0)", articleChanges.info.ToString());
                    AreEqual("iPad Pro", articleChanges.creates["article-ipad"].name);
                    IsTrue(articleChanges.deletes.Contains("article-iphone"));
                    break;
                case 4:
                    AreEqual("(creates: 0, updates: 0, deletes: 1, patches: 1)", articleChanges.info.ToString());
                    ChangePatch<Article> articlePatch = articleChanges.patches["article-1"];
                    AreEqual("article-1",               articlePatch.ToString());
                    var articlePatch0 = (PatchReplace)  articlePatch.patches[0];
                    AreEqual("Changed name",            articlePatch.entity.name);
                    AreEqual("/name",                   articlePatch0.path);
                    AreEqual("\"Changed name\"",        articlePatch0.value.json);
                    break;
            }
        }
        
        /// assert that all database changes by <see cref="TestRelationPoC.CreateStore"/> are reflected
        public void AssertCreateStoreChanges() {
            AreEqual(8,  ChangeSequence);
            AreSimilar("(creates: 2, updates: 0, deletes: 0, patches: 0)", orderSum);
            AreSimilar("(creates: 6, updates: 0, deletes: 0, patches: 0)", customerSum);
            AreSimilar("(creates: 9, updates: 0, deletes: 4, patches: 2)", articleSum);
            AreSimilar("(creates: 3, updates: 0, deletes: 0, patches: 0)", producerSum);
            AreSimilar("(creates: 1, updates: 0, deletes: 0, patches: 0)", employeeSum);
            
            IsTrue(orderSum      .IsEqual(GetChangeInfo<Order>()));
            IsTrue(customerSum   .IsEqual(GetChangeInfo<Customer>()));
            IsTrue(articleSum    .IsEqual(GetChangeInfo<Article>()));
            IsTrue(producerSum   .IsEqual(GetChangeInfo<Producer>()));
            IsTrue(employeeSum   .IsEqual(GetChangeInfo<Employee>()));
        }
    }
    
    static class PocUtils
    {
        public static void AddChanges<T> (this ChangeInfo<T> sum, EntityChanges<T> changes) where T: Entity {
            sum.creates += changes.creates.Count;
            sum.updates += changes.updates.Count;
            sum.deletes += changes.deletes.Count;
            sum.patches += changes.patches.Count;
        }
    }
}
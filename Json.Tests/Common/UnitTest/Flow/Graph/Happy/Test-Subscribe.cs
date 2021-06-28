// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Database.Event;
using Friflo.Json.Flow.Database.Utils;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.Transform;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;
using UnityEngine.TestTools;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Common.Utils.AssertUtils;



// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph.Happy
{
    internal enum EventAssertion {
        /// <summary>Assert a <see cref="SubscriptionHandler"/> will not get change events from the
        /// <see cref="EntityStore"/> it is attached to.</summary>.
        NoChanges,
        /// <summary>Assert a <see cref="SubscriptionHandler"/> will get change events from all
        /// <see cref="EntityStore"/>'s it is not attached to.</summary>.
        Changes
    }
    
    public partial class TestStore
    {
        [UnityTest] public IEnumerator  SubscribeCoroutine() { yield return RunAsync.Await(AssertSubscribe()); }
        [Test]      public void         SubscribeSync() { SingleThreadSynchronizationContext.Run(AssertSubscribe); }
        
        private static async Task AssertSubscribe() {
            var sc = SynchronizationContext.Current;
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var eventBroker  = new EventBroker(false))
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var listenDb     = new PocStore(fileDatabase, "listenDb")) {
                fileDatabase.eventBroker = eventBroker;
                var pocSubscriber        = await CreatePocHandler(listenDb, sc, EventAssertion.Changes);
                using (var createStore = new PocStore(fileDatabase, "createStore")) {
                    var createSubscriber = await CreatePocHandler(createStore, sc, EventAssertion.NoChanges);
                    await TestRelationPoC.CreateStore(createStore);
                    
                    while (!pocSubscriber.receivedAll ) { await Task.Delay(1); }

                    AreEqual(1, createSubscriber.EventSequence);  // received no change events for changes done by itself
                }
                pocSubscriber.AssertCreateStoreChanges();
                AreEqual(8, pocSubscriber.EventSequence);           // non protected access
                AreSimilar("(creates: 9, updates: 0, deletes: 4, patches: 2)",  pocSubscriber.GetChangeInfo<Article>());  // non protected access
                await eventBroker.FinishQueues();
            }
        }
        
        private static async Task<PocHandler> CreatePocHandler (PocStore store, SynchronizationContext sc, EventAssertion eventAssertion) {
            var subscriber = new PocHandler(store, sc, eventAssertion);
            store.SetSubscriptionHandler(subscriber);
            
            var subscriptions       = store.SubscribeAllChanges(Changes.All);
            var subscribeMessages   = store.SubscribeMessages(new [] { TestRelationPoC.EndCreate });
            var subscribeTestEvent  = store.SubscribeMessage<TestEvent>((ev) => {
            });
                
            await store.Sync(); // -------- Sync --------

            foreach (var subscription in subscriptions) {
                IsTrue(subscription.Success);    
            }
            IsTrue(subscribeMessages.Success);
            IsTrue(subscribeTestEvent.Success);
            return subscriber;
        }
    }

    // assert expected database changes by counting the entity changes for each DatabaseContainer / EntitySet<>
    internal class PocHandler : SubscriptionHandler {
        private readonly    ChangeInfo<Order>       orderSum     = new ChangeInfo<Order>();
        private readonly    ChangeInfo<Customer>    customerSum  = new ChangeInfo<Customer>();
        private readonly    ChangeInfo<Article>     articleSum   = new ChangeInfo<Article>();
        private readonly    ChangeInfo<Producer>    producerSum  = new ChangeInfo<Producer>();
        private readonly    ChangeInfo<Employee>    employeeSum  = new ChangeInfo<Employee>();
        private             int                     messageSum;
        internal            bool                    receivedAll;
        
        private readonly    EventAssertion          eventAssertion;
        
        internal PocHandler (EntityStore store, SynchronizationContext synchronizationContext, EventAssertion eventAssertion)
            : base (store, synchronizationContext)
        {
            this.eventAssertion = eventAssertion;
        }
            
        /// All tests using <see cref="PocHandler"/> are required to use "createStore" as clientId
        protected override void ProcessEvent (SubscriptionEvent ev) {
            AreEqual("createStore", ev.clientId);
            base.ProcessEvent(ev);
            var orderChanges    = GetEntityChanges<Order>   (ev);
            var customerChanges = GetEntityChanges<Customer>(ev);
            var articleChanges  = GetEntityChanges<Article> (ev);
            var producerChanges = GetEntityChanges<Producer>(ev);
            var employeeChanges = GetEntityChanges<Employee>(ev);
            var messages        = GetMessages                  (ev);
            
            orderSum.   AddChanges(orderChanges);
            customerSum.AddChanges(customerChanges);
            articleSum. AddChanges(articleChanges);
            producerSum.AddChanges(producerChanges);
            employeeSum.AddChanges(employeeChanges);
            messageSum += messages.Count;
            
            if (messages.Contains(TestRelationPoC.EndCreate))
                receivedAll = true;
            
            switch (eventAssertion) {
                case EventAssertion.NoChanges:
                    var changeInfo = ev.GetChangeInfo();
                    IsTrue(changeInfo.Count == 0);
                    break;
                case EventAssertion.Changes:
                    changeInfo = ev.GetChangeInfo();
                    IsTrue(changeInfo.Count > 0);
                    AssertChangeEvent(articleChanges);
                    break;
            }
        }
        
        private  void AssertChangeEvent (EntityChanges<Article> articleChanges) {
            switch (EventSequence) {
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
            AreEqual(8,  EventSequence);
            AreSimilar("(creates: 2, updates: 0, deletes: 0, patches: 0)", orderSum);
            AreSimilar("(creates: 6, updates: 0, deletes: 0, patches: 0)", customerSum);
            AreSimilar("(creates: 9, updates: 0, deletes: 4, patches: 2)", articleSum);
            AreSimilar("(creates: 3, updates: 0, deletes: 0, patches: 0)", producerSum);
            AreSimilar("(creates: 1, updates: 0, deletes: 0, patches: 0)", employeeSum);
            AreEqual(2, messageSum);
            
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
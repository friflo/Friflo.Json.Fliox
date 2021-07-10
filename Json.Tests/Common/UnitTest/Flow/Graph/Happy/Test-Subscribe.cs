// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Database.Event;
using Friflo.Json.Flow.Database.Utils;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.Transform;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;
using UnityEngine.TestTools;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Common.Utils.AssertUtils;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph.Happy
{
    internal enum EventAssertion {
        /// <summary>Assert a <see cref="SubscriptionProcessor"/> will not get change events from the
        /// <see cref="EntityStore"/> it is attached to.</summary>.
        NoChanges,
        /// <summary>Assert a <see cref="SubscriptionProcessor"/> will get change events from all
        /// <see cref="EntityStore"/>'s it is not attached to.</summary>.
        Changes
    }
    
    public partial class TestStore
    {
        [UnityTest] public IEnumerator  SubscribeCoroutine() { yield return RunAsync.Await(AssertSubscribe()); }
        [Test]      public void         SubscribeSync() { SingleThreadSynchronizationContext.Run(AssertSubscribe); }
        
        private static async Task AssertSubscribe() {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var eventBroker  = new EventBroker(false))
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var listenDb     = new PocStore(fileDatabase, "listenDb")) {
                fileDatabase.eventBroker = eventBroker;
                var listenProcessor   = await CreateSubscriptionProcessor(listenDb, EventAssertion.Changes);
                using (var createStore  = new PocStore(fileDatabase, "createStore")) {
                    var createProcessor = await CreateSubscriptionProcessor(createStore, EventAssertion.NoChanges);
                    await TestRelationPoC.CreateStore(createStore);
                    
                    while (!listenProcessor.receivedAll ) { await Task.Delay(1); }

                    AreEqual(1, createProcessor.EventSequence);  // received no change events for changes done by itself
                }
                listenProcessor.AssertCreateStoreChanges();
                AreEqual(8, listenProcessor.EventSequence);           // non protected access
                await eventBroker.FinishQueues();
            }
        }
        
        private static async Task<PocSubscriptionProcessor> CreateSubscriptionProcessor (PocStore store, EventAssertion eventAssertion) {
            var processor = new PocSubscriptionProcessor(store, eventAssertion);
            store.SetSubscriptionProcessor(processor);
            store.SetSubscriptionHandler((handler, ev) => {
                processor.subscribeEventsCalls++;
                var eventInfo = ev.GetEventInfo();
                switch (handler.EventSequence) {
                    case 2:
                        AreEqual(6, eventInfo.Count);
                        AreEqual(6, eventInfo.changes.Count);
                        AreEqual("(creates: 6, updates: 0, deletes: 0, patches: 0, messages: 0)", eventInfo.ToString());
                        var articleChanges  = handler.GetEntityChanges<Article> (ev);
                        var producerChanges = handler.GetEntityChanges<Producer>(ev);
                        AreEqual(5, articleChanges.creates.Count);
                        AreEqual("(creates: 5, updates: 0, deletes: 0, patches: 0)", articleChanges.ToString());
                        AreEqual("(creates: 1, updates: 0, deletes: 0, patches: 0)", producerChanges.ToString());
                        break;
                    case 8:
                        AreEqual(6, eventInfo.Count);
                        AreEqual(5, eventInfo.messages);
                        AreEqual(1, eventInfo.changes.creates);
                        var messages = handler.GetMessages(ev);
                        AreEqual(5, messages.Count);
                        break;
                }
            });
            
            var subscriptions   = store.SubscribeAllChanges(Changes.All);
            // change subscription of specific EntitySet<Article>
            var articlesSub     = store.articles.SubscribeChanges(Changes.All);
            
            var subscribeMessage    = store.SubscribeMessage(TestRelationPoC.EndCreate, (msg) => {
                processor.receivedAll = true;
                AreEqual("null",            msg.Json);
            });
            var subscribeMessage1   = store.SubscribeMessage<TestMessage>((msg) => {
                processor.testMessageCalls++;
                TestMessage value = msg.Value;
                AreEqual("test message",    value.text);
                AreEqual("TestMessage",     msg.Name);
            });
            var subscribeMessage2   = store.SubscribeMessage<int>(TestRelationPoC.TestMessageInt, (msg) => {
                processor.testMessageIntCalls++;
                AreEqual(42,                            msg.Value);
                AreEqual("42",                          msg.Json);
                AreEqual(TestRelationPoC.TestMessageInt,msg.Name);
                
                IsTrue(msg.TryReadJson(out int result, out _));
                AreEqual(42, result);
                
                // test reading Json to incompatible types
                IsFalse(msg.TryReadJson<string>(out _, out var error));
                AreEqual("JsonReader/error: Cannot assign number to string. Expect: System.String, got: 42 path: '(root)' at position: 2", error.Message);
                
                var e = Throws<JsonReaderException> (() => msg.ReadJson<string>());
                AreEqual("JsonReader/error: Cannot assign number to string. Expect: System.String, got: 42 path: '(root)' at position: 2", e.Message);
            });
            var subscribeMessage3   = store.SubscribeMessage(TestRelationPoC.TestMessageInt, (msg) => {
                processor.testMessageIntCalls++;
                var val = msg.ReadJson<int>();
                AreEqual(42,                            val);
                AreEqual("42",                          msg.Json);
                AreEqual(TestRelationPoC.TestMessageInt,msg.Name);
                
                IsTrue(msg.TryReadJson(out int result, out _));
                AreEqual(42, result);
                
                // test reading Json to incompatible types
                IsFalse(msg.TryReadJson<string>(out _, out var error));
                AreEqual("JsonReader/error: Cannot assign number to string. Expect: System.String, got: 42 path: '(root)' at position: 2", error.Message);
                
                var e = Throws<JsonReaderException> (() => msg.ReadJson<string>());
                AreEqual("JsonReader/error: Cannot assign number to string. Expect: System.String, got: 42 path: '(root)' at position: 2", e.Message);
            });
            
            var subscribeMessage4   = store.SubscribeMessage  (TestRelationPoC.TestRemoveHandler, RemovedHandler);
            var unsubscribe1        = store.UnsubscribeMessage(TestRelationPoC.TestRemoveHandler, RemovedHandler);

            var subscribeMessage5   = store.SubscribeMessage  (TestRelationPoC.TestRemoveAllHandler, RemovedHandler);
            var unsubscribe2        = store.UnsubscribeMessage(TestRelationPoC.TestRemoveAllHandler, null);
            
            var subscribeAllMessages= store.SubscribeMessage  ("Test*", msg => {
                processor.testWildcardCalls++;
            });

            await store.Sync(); // -------- Sync --------

            foreach (var subscription in subscriptions) {
                IsTrue(subscription.Success);    
            }
            IsTrue(articlesSub.Success);
                
            IsTrue(subscribeMessage.        Success);
            IsTrue(subscribeMessage1.       Success);
            IsTrue(subscribeMessage2.       Success);
            IsTrue(subscribeMessage3.       Success);
            IsTrue(subscribeMessage4.       Success);
            IsTrue(subscribeMessage5.       Success);
            IsTrue(unsubscribe1.            Success);
            IsTrue(unsubscribe2.            Success);
            IsTrue(subscribeAllMessages.    Success);
            return processor;
        }
        
        private static readonly MessageHandler<int> RemovedHandler = (msg) => {
            Fail("unexpected call");
        };
    }

    // assert expected database changes by counting the entity changes for each DatabaseContainer / EntitySet<>
    internal class PocSubscriptionProcessor : SubscriptionProcessor {
        private readonly    ChangeInfo<Order>       orderSum     = new ChangeInfo<Order>();
        private readonly    ChangeInfo<Customer>    customerSum  = new ChangeInfo<Customer>();
        private readonly    ChangeInfo<Article>     articleSum   = new ChangeInfo<Article>();
        private readonly    ChangeInfo<Producer>    producerSum  = new ChangeInfo<Producer>();
        private readonly    ChangeInfo<Employee>    employeeSum  = new ChangeInfo<Employee>();
        private             int                     messageCount;
        internal            int                     testMessageCalls;
        internal            int                     testMessageIntCalls;
        internal            int                     testWildcardCalls;
        internal            int                     subscribeEventsCalls;
        internal            bool                    receivedAll;
        
        private readonly    EventAssertion          eventAssertion;
        
        internal PocSubscriptionProcessor (EntityStore store, EventAssertion eventAssertion)
            : base (store)
        {
            this.eventAssertion = eventAssertion;
        }
            
        /// All tests using <see cref="PocSubscriptionProcessor"/> are required to use "createStore" as clientId
        protected override void ProcessEvent (SubscriptionEvent ev) {
            AreEqual("createStore", ev.clientId);
            base.ProcessEvent(ev);
            var orderChanges    = GetEntityChanges<Order>   (ev);
            var customerChanges = GetEntityChanges<Customer>(ev);
            var articleChanges  = GetEntityChanges<Article> (ev);
            var producerChanges = GetEntityChanges<Producer>(ev);
            var employeeChanges = GetEntityChanges<Employee>(ev);
            var messages        = GetMessages               (ev);
            
            orderSum.   AddChanges(orderChanges);
            customerSum.AddChanges(customerChanges);
            articleSum. AddChanges(articleChanges);
            producerSum.AddChanges(producerChanges);
            employeeSum.AddChanges(employeeChanges);
            messageCount += messages.Count;

            foreach (var message in messages) {
                switch (message.Name) {
                    case nameof(TestRelationPoC.EndCreate):
                        AreEqual("null", message.Json);
                        break;
                    case nameof(TestRelationPoC.TestMessageInt):
                        var intVal = message.ReadJson<int>();
                        AreEqual(42, intVal);
                        break;
                    case nameof(TestRelationPoC.TestRemoveHandler):
                    case nameof(TestRelationPoC.TestRemoveAllHandler):
                        break;
                    case nameof(TestMessage):
                        var testVal = message.ReadJson<TestMessage>();
                        AreEqual("test message", testVal.text);
                        break;
                    default:
                        Fail("test expect handling all messages");
                        break;
                }
            }
            
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
                    AreEqual("(creates: 2, updates: 0, deletes: 1, patches: 0)", articleChanges.Info.ToString());
                    AreEqual("iPad Pro", articleChanges.creates["article-ipad"].name);
                    IsTrue(articleChanges.deletes.Contains("article-iphone"));
                    break;
                case 4:
                    AreEqual("(creates: 0, updates: 0, deletes: 1, patches: 1)", articleChanges.Info.ToString());
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
            AreEqual(8, EventSequence);
            AreEqual(8, subscribeEventsCalls);
            
            AreSimilar("(creates: 2, updates: 0, deletes: 0, patches: 0)", orderSum);
            AreSimilar("(creates: 6, updates: 0, deletes: 0, patches: 0)", customerSum);
            AreSimilar("(creates: 9, updates: 0, deletes: 4, patches: 2)", articleSum);
            AreSimilar("(creates: 3, updates: 0, deletes: 0, patches: 0)", producerSum);
            AreSimilar("(creates: 1, updates: 0, deletes: 0, patches: 0)", employeeSum);
            
            AreEqual(5, messageCount);
            AreEqual(4, testWildcardCalls);

            AreEqual(1, testMessageCalls);
            AreEqual(2, testMessageIntCalls);
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
    
    public partial class TestStore {
        [Test]      public void         AcknowledgeMessages() { SingleThreadSynchronizationContext.Run(AssertAcknowledgeMessages); }
            
        private static async Task AssertAcknowledgeMessages() {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var eventBroker  = new EventBroker(false))
            using (var database     = new MemoryDatabase())
            using (var listenDb     = new EntityStore(database, null, "listenDb")) {
                database.eventBroker = eventBroker;
                var processor = new SubscriptionProcessor(listenDb);
                listenDb.SetSubscriptionProcessor(processor);
                bool receivedHello = false;
                listenDb.SubscribeMessage("Hello", msg => {
                    receivedHello = true;
                });
                await listenDb.Sync();

                using (var sendStore  = new EntityStore(database, null, "sendStore")) {
                    sendStore.SendMessage("Hello", "some text");
                    await sendStore.Sync();
                    
                    await Task.Delay(1); // release thread to process message event handler
                    IsTrue(receivedHello);
                    
                    await listenDb.Sync();

                    // assert no send events are pending which are not acknowledged
                    foreach (var subscriber in eventBroker.GetSubscribers()) {
                        AreEqual(0, subscriber.SendEventsCount);
                    }
                }
            }
        }
    }

}
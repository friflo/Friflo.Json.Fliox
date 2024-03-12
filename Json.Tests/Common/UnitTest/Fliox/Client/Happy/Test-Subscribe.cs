// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Threading;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;
using UnityEngine.TestTools;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Common.Utils.AssertUtils;

// ReSharper disable ConvertToLambdaExpression
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    internal enum EventAssertion {
        /// <summary>Assert <see cref="EventContext.IsOrigin"/> is true for change events</summary>.
        NoChanges,
        /// <summary>Assert <see cref="EventContext.IsOrigin"/> is false for change events</summary>.
        Changes
    }
    
    public partial class TestHappy
    {
        [UnityTest] public IEnumerator  SubscribeCoroutine() { yield return RunAsync.Await(AssertSubscribe()); }
        [Test]      public void         SubscribeSync() { SingleThreadSynchronizationContext.Run(AssertSubscribe); }
        
        private static async Task AssertSubscribe() {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            using (var eventDispatcher  = new EventDispatcher(EventDispatching.Send))
            using (var database         = new FileDatabase(TestGlobals.DB, TestGlobals.PocStoreFolder, null, new PocService()))
            using (var hub              = new FlioxHub(database, TestGlobals.Shared))
            using (var listenDb         = new PocStore(hub) { UserId = "listenDb", ClientId = "listen-client" }) {
                hub.EventDispatcher     = eventDispatcher;
                var listenSubscriber    = await CreatePocStoreSubscriber(listenDb, EventAssertion.Changes);
                using (var createStore  = new PocStore(hub) { UserId = "createStore", ClientId = "create-client"}) {
                    var createSubscriber = await CreatePocStoreSubscriber(createStore, EventAssertion.NoChanges);
                    await TestRelationPoC.CreateStore(createStore);
                    
                    while (!listenSubscriber.receivedAll ) { await Task.Delay(1); }

                    AreEqual(8, createSubscriber.EventCount);
                    IsTrue(createSubscriber.IsOrigin);
                }
                listenSubscriber.AssertCreateStoreChanges();
                AreEqual(8, listenSubscriber.EventCount);           // non protected access
                IsFalse(listenSubscriber.IsOrigin);
                await eventDispatcher.StopDispatcher();
            }
        }
        
        private static async Task<PocStoreSubscriber> CreatePocStoreSubscriber (PocStore store, EventAssertion eventAssertion) {
            var subscriber = new PocStoreSubscriber(store, eventAssertion);
            store.SetEventProcessor(new EventProcessorContext());
            store.SubscriptionEventHandler += subscriber.OnEvent;
            store.std.Client(new ClientParam { queueEvents = true });
            var subscriptions   = store.SubscribeAllChanges(Change.All, context => {
                AreEqual("createStore", context.UserId.AsString());
                foreach (var changes in context.Changes) {
                    subscriber.countAllChanges += changes.Count;
                }
            });
            // change subscription of specific EntitySet<Article>
            var articlesSub         = store.articles.SubscribeChanges(Change.All, (changes, context) => { });
            
            var subscribeMessage    = store.SubscribeMessage(TestRelationPoC.EndCreate, (msg, context) => {
                AreEqual("EndCreate (param: null)", msg.ToString());
                subscriber.receivedAll = true;
                IsTrue(                     msg.RawParam.IsNull());
                AreEqual("null",            msg.RawParam.AsString());
            });
            var subscribeMessage1   = store.SubscribeMessage<TestCommand>(nameof(TestCommand), (msg, context) => {
                AreEqual(@"TestCommand (param: {""text"":""test message""})", msg.ToString());
                subscriber.testMessageCalls++;
                msg.GetParam(out TestCommand param, out _);
                AreEqual("test message",        param.text);
                AreEqual(nameof(TestCommand),   msg.Name);
            });
            var subscribeMessage2   = store.SubscribeMessage<int>(TestRelationPoC.TestMessageInt, (msg, context) => {
                subscriber.testMessageIntCalls++;
                msg.GetParam(out int param, out _);
                AreEqual(42,                            param);
                AreEqual("42",                          msg.RawParam.AsString());
                AreEqual(TestRelationPoC.TestMessageInt,msg.Name);
                
                IsTrue(msg.GetParam(out int result, out _));
                AreEqual(42, result);
                
                // test reading Json to incompatible types
                IsFalse(msg.GetParam<string>(out _, out var error));
                AreEqual("JsonReader/error: Cannot assign number to string. got: 42 path: '(root)' at position: 2", error);
                
                msg.GetParam<string>(out _, out string error2);
                AreEqual("JsonReader/error: Cannot assign number to string. got: 42 path: '(root)' at position: 2", error2);
            });
            var subscribeMessage3   = store.SubscribeMessage(TestRelationPoC.TestMessageInt, (msg, context) => {
                subscriber.testMessageIntCalls++;
                msg.GetParam(out int val, out _);
                AreEqual(42,                            val);
                AreEqual("42",                          msg.RawParam.AsString());
                AreEqual(TestRelationPoC.TestMessageInt,msg.Name);
                
                IsTrue(msg.GetParam(out int result, out _));
                AreEqual(42, result);
                
                // test reading Json to incompatible types
                IsFalse(msg.GetParam<string>(out _, out var error));
                AreEqual("JsonReader/error: Cannot assign number to string. got: 42 path: '(root)' at position: 2", error);
                
                msg.GetParam<string>(out _, out string error2);
                AreEqual("JsonReader/error: Cannot assign number to string. got: 42 path: '(root)' at position: 2", error2);
            });
            
            var subscribeMessage4   = store.SubscribeMessage  (TestRelationPoC.TestRemoveHandler, RemovedHandler);
            var unsubscribe1        = store.UnsubscribeMessage(TestRelationPoC.TestRemoveHandler, RemovedHandler);

            var subscribeMessage5   = store.SubscribeMessage  (TestRelationPoC.TestRemoveAllHandler, RemovedHandler);
            var unsubscribe2        = store.UnsubscribeMessage(TestRelationPoC.TestRemoveAllHandler, null);
            
            var subscribeAllMessages= store.SubscribeMessage  ("Test*", (msg, context) => {
                subscriber.testWildcardCalls++;
            });

            await store.SyncTasks(); // ----------------

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
            return subscriber;
        }
        
        private static readonly MessageSubscriptionHandler<int> RemovedHandler = (msg, context) => {
            Fail("unexpected call");
        };
    }

    // assert expected database changes by counting the entity changes for each DatabaseContainer / EntitySet<>
    internal class PocStoreSubscriber {
        private readonly    PocStore        client;
        private             ChangeInfo      orderSum;
        private             ChangeInfo      customerSum;
        private             ChangeInfo      articleSum;
        private             ChangeInfo      producerSum;
        private             ChangeInfo      employeeSum;
        private             ChangeInfo      typesSum;
        private             ChangeInfo      nonClsSum;
        private             int             messageCount;
        internal            int             testMessageCalls;
        internal            int             testMessageIntCalls;
        internal            int             testWildcardCalls;
        internal            int             subscribeEventsCalls;
        internal            bool            receivedAll;
        internal            int             countAllChanges;
        internal            int             EventCount { get; private set; }
        internal            bool            IsOrigin   { get; private set; }
        
        private readonly    EventAssertion  eventAssertion;
        
        internal PocStoreSubscriber (PocStore client, EventAssertion eventAssertion) {
            this.client         = client;
            this.eventAssertion = eventAssertion;
        }
            
        /// All tests using <see cref="PocStoreSubscriber"/> are required to use "createStore" as userId
        public void OnEvent (EventContext context) {
            AreEqual("createStore", context.UserId.AsString());
            EventCount  = context.EventCount;
            IsOrigin    = context.IsOrigin;
            
            context.ApplyChangesTo(client);
            
            CheckSomeMessages(context);
            
            var orderChanges    = context.GetChanges(client.orders);
            var customerChanges = context.GetChanges(client.customers);
            var articleChanges  = context.GetChanges(client.articles);
            var producerChanges = context.GetChanges(client.producers);
            var employeeChanges = context.GetChanges(client.employees);
            var typesChanges    = context.GetChanges(client.types);
            var nonClsChanges   = context.GetChanges(client.nonClsTypes);
            var messages        = context.Messages;
            
            orderSum.   Add(orderChanges.ChangeInfo);
            customerSum.Add(customerChanges.ChangeInfo);
            articleSum. Add(articleChanges.ChangeInfo);
            producerSum.Add(producerChanges.ChangeInfo);
            employeeSum.Add(employeeChanges.ChangeInfo);
            typesSum.   Add(typesChanges.ChangeInfo);
            nonClsSum.  Add(nonClsChanges.ChangeInfo);
            messageCount += messages.Count;

            foreach (var message in messages) {
                switch (message.Name) {
                    case nameof(TestRelationPoC.EndCreate):
                        IsTrue  (        message.RawParam.IsNull());
                        AreEqual("null", message.RawParam.AsString());
                        break;
                    case nameof(TestRelationPoC.TestMessageInt):
                        message.GetParam(out int intVal, out _);
                        AreEqual(42, intVal);
                        break;
                    case nameof(TestRelationPoC.TestRemoveHandler):
                    case nameof(TestRelationPoC.TestRemoveAllHandler):
                        break;
                    case nameof(TestCommand):
                        message.GetParam(out TestCommand testVal, out _);
                        AreEqual("test message", testVal.text);
                        break;
                    default:
                        Fail("test expect handling all messages");
                        break;
                }
            }
            var changeInfo = context.Changes;
            IsTrue(changeInfo.Count > 0);
            AssertChangeEvent(articleChanges);
            
            switch (eventAssertion) {
                case EventAssertion.NoChanges:
                    IsTrue(context.IsOrigin);
                    break;
                case EventAssertion.Changes:
                    IsFalse(context.IsOrigin);
                    break;
            }
        }
        
        private void CheckSomeMessages(EventContext context) {
            subscribeEventsCalls++;
            var eventInfo = context.EventInfo;
            switch (context.EventCount) {
                case 3:
                    AreEqual(7, eventInfo.Count);
                    AreEqual(7, eventInfo.changes.Count);
                    AreEqual("creates: 3, upserts: 4, deletes: 0, merges: 0, messages: 0", eventInfo.ToString());
                    var articleChanges  = context.GetChanges(client.articles);
                    var producerChanges = context.GetChanges(client.producers);
                    AreEqual(2, articleChanges.Creates.Count);
                    AreEqual("articles - creates: 2, upserts: 4, deletes: 0, merges: 0", articleChanges.ToString());
                    AreEqual("producers - creates: 1, upserts: 0, deletes: 0, merges: 0", producerChanges.ToString());
                    break;
                case 8:
                    AreEqual(6, eventInfo.Count);
                    AreEqual(5, eventInfo.messages);
                    AreEqual(1, eventInfo.changes.upserts);
                    var messages = context.Messages;
                    AreEqual(5, messages.Count);
                    break;
            }
        }
        
        private  void AssertChangeEvent (Changes<string, Article> articleChanges) {
            switch (EventCount) {
                case 2:
                    AreEqual("creates: 0, upserts: 2, deletes: 0, merges: 0", articleChanges.ChangeInfo.ToString());
                    var upsert = articleChanges.Upserts.Find(upsert => upsert.key == "article-ipad");
                    AreEqual("iPad Pro", upsert.entity.name);
                    break;
                case 5:
                    AreEqual("creates: 0, upserts: 0, deletes: 1, merges: 4", articleChanges.ChangeInfo.ToString());
                    NotNull(articleChanges.Deletes.Find(delete => delete.key == "article-delete"));
                    Patch<string> articlePatch = articleChanges.Patches.Find(p => p.key == "article-1");
                    AreEqual("article-1",               articlePatch.ToString());
                    var articleMerge =   articlePatch.patch;
                    var expect = @"{""id"":""article-1"",""name"":""Changed name""}";
                    AreEqual(expect,                    articleMerge.AsString());
                    // AreEqual("/name",                   articlePatch0.path);
                    // AreEqual("\"Changed name\"",        articlePatch0.value.AsString());
                    
                    // cached article is updated by ApplyChangesTo()
                    client.articles.Local.TryGetEntity("article-1", out var article);
                    AreEqual("Changed name",            article.name);
                    break;
            }
        }
        
        /// assert that all database changes by <see cref="TestRelationPoC.CreateStore"/> are reflected
        public void AssertCreateStoreChanges() {
            AreEqual(8, EventCount);
            AreEqual(8, subscribeEventsCalls);
            
            AreSimilar("creates: 0, upserts: 2, deletes: 0, merges: 0", orderSum);
            AreSimilar("creates: 1, upserts: 6, deletes: 1, merges: 0", customerSum);
            AreSimilar("creates: 3, upserts: 7, deletes: 6, merges: 4", articleSum);
            AreSimilar("creates: 3, upserts: 0, deletes: 3, merges: 0", producerSum);
            AreSimilar("creates: 1, upserts: 0, deletes: 1, merges: 0", employeeSum);
            AreSimilar("creates: 0, upserts: 1, deletes: 0, merges: 0", typesSum);
            AreSimilar("creates: 0, upserts: 1, deletes: 0, merges: 0", nonClsSum);
            
            AreEqual(5,  messageCount);
            AreEqual(4,  testWildcardCalls);

            AreEqual(1,  testMessageCalls);
            AreEqual(2,  testMessageIntCalls);
            
            var allChanges = orderSum.Count     +
                             customerSum.Count  +
                             articleSum.Count   +
                             producerSum.Count  +
                             employeeSum.Count  +
                             typesSum.Count     +
                             nonClsSum.Count;
            
            AreEqual(40, allChanges);
            AreEqual(40, countAllChanges);
        }
    }
    
    public partial class TestHappy {
        [Test]
        public void AcknowledgeMessages() { SingleThreadSynchronizationContext.Run(AssertAcknowledgeMessages); }
            
        private static async Task AssertAcknowledgeMessages() {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            using (var eventDispatcher  = new EventDispatcher(EventDispatching.Send))
            using (var database         = new MemoryDatabase(TestGlobals.DB))
            using (var hub              = new FlioxHub(database, TestGlobals.Shared))
            using (var listenDb         = new FlioxClient(hub) { ClientId = "listenDb" }) {
                listenDb.SetEventProcessor(new EventProcessorContext());
                hub.EventDispatcher = eventDispatcher;
                bool receivedHello = false;
                listenDb.SubscribeMessage("Hello", (msg, context) => {
                    receivedHello = true;
                });
                await listenDb.SyncTasks();

                using (var sendStore  = new FlioxClient(hub) { ClientId = "sendStore" }) {
                    sendStore.SendMessage("Hello", "some text");
                    await sendStore.SyncTasks();
                    
                    while (!receivedHello) {
                        await Task.Delay(1); // release thread to process message event handler
                    }
                    
                    await listenDb.SyncTasks();

                    // assert no send events are pending which are not acknowledged
                    AreEqual(0, eventDispatcher.QueuedEventsCount());
                }
            }
        }
        
        [Test]
        public void MultiDbSubscriptions() { SingleThreadSynchronizationContext.Run(AssertMultiDbSubscriptions); }
            
        private static async Task AssertMultiDbSubscriptions() {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            using (var eventDispatcher  = new EventDispatcher(EventDispatching.Send))
            using (var database         = new MemoryDatabase(TestGlobals.DB))
            using (var extDB            = new MemoryDatabase("ext_db"))
            using (var hub              = new FlioxHub(database, TestGlobals.Shared))
            using (var listenMainDb     = new FlioxClient(hub)              { ClientId = "listenMainDb" })
            using (var listenExtDb      = new FlioxClient(hub, "ext_db")    { ClientId = "listenExtDb" }) {
                hub.EventDispatcher = eventDispatcher;
                hub.AddExtensionDB(extDB);
                int receivedHelloMainDB = 0;
                int receivedHelloExtDB  = 0;
                listenMainDb.SubscribeMessage("hello-main_db", (msg, context) => {
                    receivedHelloMainDB++;
                });
                listenMainDb.SubscribeMessage("hello-ext_db", (msg, context) => {
                    throw new InvalidOperationException("expect only main_db messages");
                });
                listenExtDb.SubscribeMessage("hello-main_db", (msg, context) => {
                    throw new InvalidOperationException("expect only ext_db messages");
                });
                listenExtDb.SubscribeMessage("hello-ext_db", (msg, context) => {
                    receivedHelloExtDB++;
                });
                
                await listenMainDb.SyncTasks();
                await listenExtDb.SyncTasks();
                

                using (var mainDbStore  = new FlioxClient(hub)              { ClientId = "mainDbStore" })
                using (var extDbStore   = new FlioxClient(hub, "ext_db")    { ClientId = "extDbStore" })
                {
                    extDbStore.SendMessage("hello-ext_db", "some text");
                    await extDbStore.SyncTasks();
                    
                    mainDbStore.SendMessage("hello-main_db", "some text to ext_db");
                    await mainDbStore.SyncTasks();
                    
                    while (receivedHelloMainDB == 0) {
                        await Task.Delay(1); // release thread to process message event handler
                    }
                    AreEqual(1, receivedHelloMainDB);
                    AreEqual(1, receivedHelloExtDB);
                    
                    await listenMainDb.SyncTasks();
                    await listenExtDb.SyncTasks();

                    // assert no send events are pending which are not acknowledged
                    AreEqual(0, eventDispatcher.QueuedEventsCount());
                }
            }
        }
        
        [Test]
        public void TestQueueEvents() { SingleThreadSynchronizationContext.Run(AssertTestQueueEvents); }
        
        /// Ensure <see cref="HubPermission.queueEvents"/> is true if client ask for <see cref="ClientParam.queueEvents"/>
        private static async Task AssertTestQueueEvents() {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            using (var eventDispatcher  = new EventDispatcher(EventDispatching.Send))
            using (var database         = new MemoryDatabase(TestGlobals.DB))
            using (var hub              = new FlioxHub(database, TestGlobals.Shared))
            using (var testQueueEvents  = new FlioxClient(hub) { UserId = "test-queue-events" }) {
                {
                    var authenticator = hub.Authenticator as AuthenticateNone;
                    AreEqual(HubPermission.Full, authenticator.anonymous.HubPermission); // default permission
                    var sub = testQueueEvents.std.Client(new ClientParam { queueEvents = true });
                    await testQueueEvents.TrySyncTasks();
                    
                    AreEqual("CommandError ~ std.Client queueEvents requires an EventDispatcher assigned to FlioxHub", sub.Error.Message);
                } {
                    hub.EventDispatcher = eventDispatcher;
                    var clientTask = testQueueEvents.std.Client(new ClientParam { queueEvents = true });
                    await testQueueEvents.SyncTasks();
                    
                    IsTrue(clientTask.Success);
                    IsTrue(clientTask.Result.queueEvents);
                } {
                    hub.Authenticator = new AuthenticateNone(TaskAuthorizer.Full, HubPermission.None);
                    var clientTask = testQueueEvents.std.Client(new ClientParam { queueEvents = true });
                    await testQueueEvents.TrySyncTasks();
                    
                    AreEqual("CommandError ~ std.Client queueEvents requires permission (Role.hubRights) queueEvents = true", clientTask.Error.Message);
                } {
                    // clearing queueEvents require no specific authorization
                    var clientTask = testQueueEvents.std.Client(new ClientParam { queueEvents = false });
                    await testQueueEvents.SyncTasks();
                    
                    IsTrue (clientTask.Success);
                    IsFalse(clientTask.Result.queueEvents);
                } 
            }
        }
        
        [Test]
        public static void TestModifySubscriptionInHandler() { SingleThreadSynchronizationContext.Run(AssertModifySubscriptionInHandler); }
        
        /// <summary>
        /// Support changing subscriptions in subscription handlers.
        /// <br/>
        /// The handlers are stored in collections and are iterated. These collections are copied to temporary collections to avoid:
        /// InvalidOperationException : Collection was modified; enumeration operation may not execute.
        /// <see cref="Friflo.Json.Fliox.Hub.Client.Event.MessageSubscriber"/> callbackHandlers and
        /// <see cref="ClientIntern.subscriptionsPrefix"/>
        /// </summary>
        private static async Task AssertModifySubscriptionInHandler() {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            using (var eventDispatcher  = new EventDispatcher(EventDispatching.Send))
            using (var database         = new MemoryDatabase(TestGlobals.DB))
            using (var hub              = new FlioxHub(database, TestGlobals.Shared))
            using (var store            = new PocStore(hub) { UserId = "test-modify-handler" }) {
                hub.EventDispatcher = eventDispatcher;
                // bool run = true;
                store.SubscribeMessage("msg", (message, context) => {
                    store.UnsubscribeMessage("msg", null);
                });
                store.SubscribeMessage("prefix*", (message, context) => {
                    store.SubscribeMessage("prefix2*", null);
                });
                // store.SubscribeMessage("finish", (message, context) => { run = false; });
                await store.SyncTasks();
                
                store.SendMessage("msg", "hello");
                store.SendMessage("prefix", "hello");
                await store.SyncTasks();
                
                store.SendMessage("finish", "");
                await store.SyncTasks();
            }
        }
        
        [Test]
        public static void TestSubscribeChangesFilter() { SingleThreadSynchronizationContext.Run(AssertSubscribeChangesFilter); }
        private static async Task AssertSubscribeChangesFilter() {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            using (var eventDispatcher  = new EventDispatcher(EventDispatching.Send))
            using (var database         = new MemoryDatabase(TestGlobals.DB))
            using (var hub              = new FlioxHub(database, TestGlobals.Shared))
            using (var store            = new PocStore(hub) { UserId = "test-modify-handler" }) {
                hub.EventDispatcher = eventDispatcher;
                bool foundArticle1  = false;
                bool foundPaul      = false;
                
                store.articles.SubscribeChangesFilter(Change.upsert, a => a.name == "Name1", (changes, context) => {
                    var upserts = changes.Upserts; 
                    AreEqual(1, upserts.Count);
                    AreEqual("a-1", upserts[0].key);
                    foundArticle1 = true;
                });
                store.customers.SubscribeChangesFilter(Change.create, c => c.name == "Json", (changes, context) => {
                    Fail("unexpected change event");
                });
                
                var filterJohn = new EntityFilter<Employee>(e => e.firstName == "Paul");
                store.employees.SubscribeChangesByFilter(Change.upsert, filterJohn, (changes, context) => {
                    var upserts = changes.Upserts;
                    AreEqual(1, upserts.Count);
                    AreEqual("e-2", upserts[0].key);
                    foundPaul = true;
                });
                await store.SyncTasks();
                
                // --- generate change events with a single entity
                var upsertArticles = new [] {
                    new Article{ id = "a-1", name = "Name1"},
                    new Article{ id = "a-2", name = "Name2"}
                };
                store.articles.UpsertRange(upsertArticles);
                
                var upsertEmployees = new [] {
                    new Employee{ id = "e-1", firstName = "Peter"},
                    new Employee{ id = "e-2", firstName = "Paul"}
                };
                store.employees.UpsertRange(upsertEmployees);
                
                // --- following changes generate no events
                var createArticles = new [] {
                    new Article{ id = "a-3", name = "Name1"},
                    new Article{ id = "a-4", name = "Name1"}
                };
                store.articles.CreateRange(createArticles);
                store.articles.Delete("a-5");
                
                store.customers.Upsert(new Customer{ id = "c-1", name = "Name1"});
                
                await store.SyncTasks();
                
                IsTrue(foundArticle1);
                IsTrue(foundPaul);
            }
        }
        
        [Test]
        public static void TestSubscribeApplyChanges() { SingleThreadSynchronizationContext.Run(AssertSubscribeApplyChanges); }
        private static async Task AssertSubscribeApplyChanges() {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            using (var eventDispatcher  = new EventDispatcher(EventDispatching.Send))
            using (var database         = new MemoryDatabase(TestGlobals.DB))
            using (var hub              = new FlioxHub(database, TestGlobals.Shared))
            using (var store            = new PocStore(hub) { UserId = "store" })
            using (var listen           = new PocStore(hub) { UserId = "listen" }) {
                hub.EventDispatcher = eventDispatcher;
                var eventCount      = 0;
                
                listen.articles.SubscribeChanges(Change.upsert | Change.merge, (changes, context) => {
                    var applyResult = changes.ApplyChangesTo(listen.articles);
                    var applyInfos  = applyResult.applyInfos;
                    switch (eventCount++) {
                        case 0: {
                            AreEqual(1,                                     applyInfos.Count);
                            var applyInfo = applyInfos[0];
                            AreEqual(ApplyInfoType.EntityCreated,           applyInfo.type);
                            AreEqual("a-1",                                 applyInfo.key);
                        
                            NotNull (                                       applyInfo.entity);
                            AreEqual(@"{""id"":""a-1"",""name"":""Name1""}",applyInfo.rawEntity.AsString());
                            break;
                        }
                        case 1: {
                            // "a-1" is already applied. Only "a-2" is a new entity
                            AreEqual(2,                             applyInfos.Count);
                            var applyInfo0 = applyInfos[0];
                            AreEqual(ApplyInfoType.EntityUpdated,   applyInfo0.type);
                            AreEqual("a-1",                         applyInfo0.key);
                            NotNull (                               applyInfo0.entity);
                            var applyInfo1 = applyInfos[1];
                            AreEqual(ApplyInfoType.EntityCreated,   applyInfo1.type);
                            AreEqual("a-2",                         applyInfo1.key);
                            break;
                        }
                        case 2: {
                            AreEqual(1,                             applyInfos.Count);
                            var applyInfo = applyInfos[0];
                            AreEqual("a-1",                         applyInfo.key);
                            AreEqual(ApplyInfoType.EntityPatched,   applyInfo.type);
                            NotNull (                               applyInfo.entity);
                            var expect = @"{""id"":""a-1"",""name"":""name changed""}";
                            AreEqual(expect,                        applyInfo.rawEntity.AsString());
                            break;
                        }
                    }
                });
                await listen.SyncTasks();
                
                // --- upsert the old and a new one
                
                var a1 = new Article{ id = "a-1", name = "Name1" };
                var a2 = new Article{ id = "a-2", name = "Name2" };
                store.articles.Upsert(a1);
                await store.SyncTasks();

                var upsertArticles = new [] { a1, a2 };
                store.articles.UpsertRange(upsertArticles);
                await store.SyncTasks();
                
                a1.name         = "name changed";
                var patchesTask = store.articles.DetectPatches();
                var patches     = patchesTask.Patches; 
                AreEqual(1, patches.Count);
                await store.SyncTasks();
                
                AreEqual(3, eventCount);
            }
        }
    }

}
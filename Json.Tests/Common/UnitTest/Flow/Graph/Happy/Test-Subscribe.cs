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
        [Test] public async Task TestSubscribe      () { await TestCreate(async (store) => await AssertSubscribe ()); }
        
        private static async Task AssertSubscribe() {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var eventBroker  = new EventBroker())
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var listenDb     = new PocStore(fileDatabase, "listenDb")) {
                fileDatabase.eventBroker = eventBroker;
                var pocListener = await CreatePocListener(listenDb);
                
                using (await TestRelationPoC.CreateStore(fileDatabase)) { }
                
                pocListener.AssertCreateStoreChanges();
                AreEqual(8,                     pocListener.onChangeCount);             // non protected access
                AreSimilar("(13, 9, 0, 4, 2)",  pocListener.GetChangeInfo<Article>());  // non protected access
            }
        }
        
        private static async Task<PocListener> CreatePocListener (PocStore store) {
            var storeChanges = new PocListener();
            var types = new HashSet<TaskType>(new [] {TaskType.create, TaskType.update, TaskType.delete, TaskType.patch});
            store.SetChangeListener(storeChanges);
            var subscribeArticles   = store.articles. SubscribeAll(types);
            var subscribeCustomers  = store.customers.SubscribeAll(types);
            var subscribeEmployees  = store.employees.SubscribeAll(types);
            var subscribeOrders     = store.orders.   SubscribeAll(types);
            var subscribeProducers  = store.producers.SubscribeAll(types);
                
            await store.Sync(); // -------- Sync --------
                
            IsTrue(subscribeArticles    .Success);
            IsTrue(subscribeCustomers   .Success);
            IsTrue(subscribeEmployees   .Success);
            IsTrue(subscribeOrders      .Success);
            IsTrue(subscribeProducers   .Success);
            return storeChanges;
        }
    }

    internal class PocListener : ChangeListener {
        private readonly    ChangeInfo<Order>       orderInfo     = new ChangeInfo<Order>();
        private readonly    ChangeInfo<Customer>    customerInfo  = new ChangeInfo<Customer>();
        private readonly    ChangeInfo<Article>     articleInfo   = new ChangeInfo<Article>();
        private readonly    ChangeInfo<Producer>    producerInfo  = new ChangeInfo<Producer>();
        private readonly    ChangeInfo<Employee>    employeeInfo  = new ChangeInfo<Employee>();
            
        public override void OnChanges (ChangesEvent changes, EntityStore store) {
            base.OnChanges(changes, store);
            orderInfo.   AddChanges(GetEntityChanges<Order>());
            customerInfo.AddChanges(GetEntityChanges<Customer>());
            articleInfo. AddChanges(GetEntityChanges<Article>());
            producerInfo.AddChanges(GetEntityChanges<Producer>());
            employeeInfo.AddChanges(GetEntityChanges<Employee>());
        }
        
        /// assert that all database changes by <see cref="TestRelationPoC.CreateStore"/> are reflected
        public void AssertCreateStoreChanges() {
            AreEqual(8,  onChangeCount);
            AreSimilar("( 2, 2, 0, 0, 0)", orderInfo);
            AreSimilar("( 6, 6, 0, 0, 0)", customerInfo);
            AreSimilar("(13, 9, 0, 4, 0)", articleInfo); // todo patches
            AreSimilar("( 3, 3, 0, 0, 0)", producerInfo);
            AreSimilar("( 1, 1, 0, 0, 0)", employeeInfo);
            
            IsTrue(orderInfo      .IsEqual(GetChangeInfo<Order>()));
            IsTrue(customerInfo   .IsEqual(GetChangeInfo<Customer>()));
         // IsTrue(articleInfo    .IsEqual(GetChangeInfo<Article>()));
            IsTrue(producerInfo   .IsEqual(GetChangeInfo<Producer>()));
            IsTrue(employeeInfo   .IsEqual(GetChangeInfo<Employee>()));
        }
    }
}
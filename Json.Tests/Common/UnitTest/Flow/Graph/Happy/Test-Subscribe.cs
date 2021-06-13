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
    
    internal class ChangeCounts<T> where T : Entity {
        private     int changeCount;
        private     int createCount;
        private     int updateCount;
        private     int deleteCount;
        
        internal void AddChanges(EntityChanges<T> entityChanges) {
            changeCount += entityChanges.Count;
            createCount += entityChanges.creates.Count;
            updateCount += entityChanges.updates.Count;
            deleteCount += entityChanges.deletes.Count;
        }

        public override string ToString() => $"({changeCount}, {createCount}, {updateCount}, {deleteCount})";
    }
    
    internal class PocListener : ChangeListener {
        private             int                     onChangeCount;
        private readonly    ChangeCounts<Order>     orderCounts     = new ChangeCounts<Order>();
        private readonly    ChangeCounts<Customer>  customerCounts  = new ChangeCounts<Customer>();
        private readonly    ChangeCounts<Article>   articleCounts   = new ChangeCounts<Article>();
        private readonly    ChangeCounts<Producer>  produceCounts   = new ChangeCounts<Producer>();
        private readonly    ChangeCounts<Employee>  employeeCounts  = new ChangeCounts<Employee>();
            
        public override void OnChanges (ChangesEvent changes, EntityStore store) {
            base.OnChanges(changes, store);
            onChangeCount++;
            orderCounts.   AddChanges(GetEntityChanges<Order>());
            customerCounts.AddChanges(GetEntityChanges<Customer>());
            articleCounts. AddChanges(GetEntityChanges<Article>());
            produceCounts. AddChanges(GetEntityChanges<Producer>());
            employeeCounts.AddChanges(GetEntityChanges<Employee>());
        }
        
        /// assert that all database changes by <see cref="TestRelationPoC.CreateStore"/> are reflected
        public void AssertCreateStoreChanges() {
            AreEqual(8,  onChangeCount);
            AreSimilar("( 6, 2, 0, 4)", orderCounts);
            AreSimilar("(10, 6, 0, 4)", customerCounts);
            AreSimilar("(13, 9, 0, 4)", articleCounts);
            AreSimilar("( 7, 3, 0, 4)", produceCounts);
            AreSimilar("( 5, 1, 0, 4)", employeeCounts);
        }
    }
}
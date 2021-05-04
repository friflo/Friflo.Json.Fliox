using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using UnityEngine.TestTools;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph
{
    public class TestStoreErrors : LeakTestsFixture
    {
        [UnityTest] public IEnumerator FileEmptyCoroutine() { yield return RunAsync.Await(FileEmpty()); }
        [Test]      public async Task  FileEmptyAsync() { await FileEmpty(); }
        
        private async Task FileEmpty() {
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/dbErrors"))
            using (var testDatabase = new TestDatabase(fileDatabase))
            using (var useStore     = new PocStore(testDatabase)) {
                await TestStoresErrors(useStore);
            }
        }

        private static async Task TestStoresErrors(PocStore useStore) {
            await AssertQueryTask(useStore);
            await AssertReadTask(useStore);
        }

        private const string ArticleError = @"Task failed by entity errors. Count: 1
| Failed parsing entity: Article 'article-2', JsonParser/JSON error: expect key. Found: J path: 'name' at position: 52";
        
        private static async Task AssertQueryTask(PocStore store) {
            var orders = store.orders;
            var articles = store.articles;

            var readOrders  = orders.Read();
            var order1      = readOrders.Find("order-1");
            AreEqual("ReadId<Order> id: order-1", order1.ToString());
            var allArticles             = articles.QueryAll();
            var producersTask           = allArticles.ReadRefs(a => a.producer);
            var hasOrderCamera          = orders.Query(o => o.items.Any(i => i.name == "Camera"));
            var ordersWithCustomer1     = orders.Query(o => o.customer.id == "customer-1");
            var read3                   = orders.Query(o => o.items.Count(i => i.amount < 1) > 0);
            var ordersAnyAmountLower2   = orders.Query(o => o.items.Any(i => i.amount < 2));
            var ordersAllAmountGreater0 = orders.Query(o => o.items.All(i => i.amount > 0));

            ReadRefTask<Customer> customer  = readOrders.ReadRefByPath<Customer>(".customer");
            ReadRefTask<Customer> customer2 = readOrders.ReadRefByPath<Customer>(".customer");
            AreSame(customer, customer2);
            ReadRefTask<Customer> customer3 = readOrders.ReadRef(o => o.customer);
            AreSame(customer, customer3);
            AreEqual("ReadTask<Order> #ids: 1 > .customer", customer.ToString());

            Exception e;
            e = Throws<TaskNotSyncedException>(() => { var _ = customer.Id; });
            AreEqual("ReadRefTask.Id requires Sync(). ReadTask<Order> #ids: 1 > .customer", e.Message);
            e = Throws<TaskNotSyncedException>(() => { var _ = customer.Result; });
            AreEqual("ReadRefTask.Result requires Sync(). ReadTask<Order> #ids: 1 > .customer", e.Message);

            e = Throws<TaskNotSyncedException>(() => { var _ = hasOrderCamera.Results; });
            AreEqual("QueryTask.Result requires Sync(). QueryTask<Order> filter: .items.Any(i => i.name == 'Camera')", e.Message);
            e = Throws<TaskNotSyncedException>(() => { var _ = hasOrderCamera["arbitrary"]; });
            AreEqual("QueryTask[] requires Sync(). QueryTask<Order> filter: .items.Any(i => i.name == 'Camera')", e.Message);

            var producerEmployees = producersTask.ReadArrayRefs(p => p.employeeList);
            AreEqual("QueryTask<Article> filter: true > .producer > .employees[*]", producerEmployees.ToString());


            await store.Sync(); // -------- Sync --------
            AreEqual(1,                 ordersWithCustomer1.Results.Count);
            NotNull(ordersWithCustomer1["order-1"]);
            
            AreEqual(1,                 ordersAnyAmountLower2.Results.Count);
            NotNull(ordersAnyAmountLower2["order-1"]);
            
            AreEqual(1,                 ordersAllAmountGreater0.Results.Count);
            NotNull(ordersAllAmountGreater0["order-1"]);


            TaskEntityException te;
            te = Throws<TaskEntityException>(() => { var _ = allArticles.Results; });
            AreEqual(ArticleError, te.Message);
            AreEqual(1, te.errors.Count);
            
            te = Throws<TaskEntityException>(() => { var _ = allArticles.Results["article-galaxy"]; });
            AreEqual(ArticleError, te.Message);
            AreEqual(1, te.errors.Count);
            
            AreEqual(1,                 hasOrderCamera.Results.Count);
            AreEqual(3,                 hasOrderCamera["order-1"].items.Count);
    
            AreEqual("customer-1",      customer.Id);
            AreEqual("Smith Ltd.",      customer.Result.name);
                
            te = Throws<TaskEntityException>(() => { var _ = producersTask.Results; });
            AreEqual(ArticleError, te.Message);
            AreEqual(1, te.errors.Count);
                
            te = Throws<TaskEntityException>(() => { var _ = producerEmployees.Results; });
            AreEqual(ArticleError, te.Message);
            AreEqual(1, te.errors.Count);
        }
        
        private static async Task AssertReadTask(PocStore store) {
            var orders = store.orders;
            var readOrders = orders.Read();
            var order1Task = readOrders.Find("order-1");
            await store.Sync();
            
            // schedule ReadRefs on an already synced Read operation
            Exception e;
            e = Throws<TaskAlreadySyncedException>(() => { readOrders.ReadRefByPath<Article>("customer"); });
            AreEqual("Task already synced. ReadTask<Order> #ids: 1", e.Message);
            e = Throws<TaskAlreadySyncedException>(() => { readOrders.ReadRefsByPath<Article>("items[*].article"); });
            AreEqual("Task already synced. ReadTask<Order> #ids: 1", e.Message);
            
            // todo add Read() without ids 

            readOrders = orders.Read();
            readOrders.Find("order-1");
            ReadRefsTask<Article> articleRefsTask  = readOrders.ReadRefsByPath<Article>(".items[*].article");
            ReadRefsTask<Article> articleRefsTask2 = readOrders.ReadRefsByPath<Article>(".items[*].article");
            AreSame(articleRefsTask, articleRefsTask2);
            
            ReadRefsTask<Article> articleRefsTask3 = readOrders.ReadArrayRefs(o => o.items.Select(a => a.article));
            AreSame(articleRefsTask, articleRefsTask3);
            AreEqual("ReadTask<Order> #ids: 1 > .items[*].article", articleRefsTask.ToString());

            e = Throws<TaskNotSyncedException>(() => { var _ = articleRefsTask["article-1"]; });
            AreEqual("ReadRefsTask[] requires Sync(). ReadTask<Order> #ids: 1 > .items[*].article", e.Message);
            e = Throws<TaskNotSyncedException>(() => { var _ = articleRefsTask.Results; });
            AreEqual("ReadRefsTask.Results requires Sync(). ReadTask<Order> #ids: 1 > .items[*].article", e.Message);

            ReadRefsTask<Producer> articleProducerTask = articleRefsTask.ReadRefs(a => a.producer);
            AreEqual("ReadTask<Order> #ids: 1 > .items[*].article > .producer", articleProducerTask.ToString());

            var readTask = store.articles.Read();
            var duplicateId = "article-galaxy"; // support duplicate ids
            var galaxy      = readTask.Find(duplicateId);
            var article2    = readTask.Find("article-2");
            var articleSet  = readTask.FindRange(new [] {duplicateId, duplicateId, "article-ipad"});

            await store.Sync(); // -------- Sync --------
        
            AreEqual(2,                 articleRefsTask.Results.Count);
            AreEqual("Changed name",    articleRefsTask["article-1"].name);
            
            AreEqual(1,                 articleProducerTask.Results.Count);
            AreEqual("Canon",           articleProducerTask["producer-canon"].name);

            TaskEntityException te;
            te = Throws<TaskEntityException>(() => { var _ = articleSet.Results; });
            AreEqual(ArticleError, te.Message);
            AreEqual(1, te.errors.Count);
            
            te = Throws<TaskEntityException>(() => { var _ = galaxy.Result; });
            AreEqual(ArticleError, te.Message);
            AreEqual(1, te.errors.Count);
            
            te = Throws<TaskEntityException>(() => { var _ = readTask.Results; });
            AreEqual(ArticleError, te.Message);
            AreEqual(1, te.errors.Count);
        }
    }
}
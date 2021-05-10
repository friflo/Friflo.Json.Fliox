using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using UnityEngine.TestTools;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph
{
    public class TestStoreErrors : LeakTestsFixture
    {
        [UnityTest] public IEnumerator FileUseCoroutine() { yield return RunAsync.Await(FileUse()); }
        [Test]      public async Task  FileUseAsync() { await FileUse(); }
        
        private async Task FileUse() {
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var testDatabase = new TestDatabase(fileDatabase))
            using (var useStore     = new PocStore(testDatabase)) {
                AddSimulationErrors(testDatabase);
                await TestStoresErrors(useStore);
            }
        }
        
        [UnityTest] public IEnumerator RemoteUseCoroutine() { yield return RunAsync.Await(RemoteUse()); }
        [Test]      public async Task  RemoteUseAsync() { await RemoteUse(); }
        
        private async Task RemoteUse() {
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var testDatabase = new TestDatabase(fileDatabase))
            using (var hostDatabase = new RemoteHost(testDatabase, "http://+:8080/")) {
                AddSimulationErrors(testDatabase);
                await TestStore.RunRemoteHost(hostDatabase, async () => {
                    using (var remoteDatabase   = new RemoteClient("http://localhost:8080/"))
                    using (var useStore         = new PocStore(remoteDatabase)) {
                        await TestStoresErrors(useStore);
                    }
                });
            }
        }
        
        private static void AddSimulationErrors(TestDatabase testDatabase) {
            var articles = testDatabase.GetTestContainer("Article");
            articles.readErrors.Add(Article2JsonError, @"{""invalidJson"" XXX}");
            articles.readErrors.Add(Article1ReadError,      Simulate.ReadEntityError);
            
            var customers = testDatabase.GetTestContainer("Customer");
            customers.readErrors.Add(ReadTaskException,     Simulate.ReadTaskException);
            //customers.readErrors.Add(ReadEntityError,       Simulate.ReadEntityError);
            customers.readErrors.Add(ReadTaskError,         Simulate.ReadTaskError);
            
            customers.writeErrors.Add(DeleteEntityError,    Simulate.WriteEntityError);

            customers.queryErrors.Add(".id == 'query-task-exception'",  Simulate.QueryTaskException); // == Query(c => c.id == "query-task-exception")
            customers.queryErrors.Add(".id == 'query-task-error'",      Simulate.QueryTaskError);     // == Query(c => c.id == "query-task-error")
            
            customers.writeErrors.Add(CreateTaskError,      Simulate.WriteTaskError);
            customers.writeErrors.Add(UpdateTaskError,      Simulate.WriteTaskError);
            customers.writeErrors.Add(DeleteTaskError,      Simulate.WriteTaskError);

            customers.writeErrors.Add(CreateTaskException,  Simulate.WriteTaskException);
            customers.writeErrors.Add(UpdateTaskException,  Simulate.WriteTaskException);
            customers.writeErrors.Add(DeleteTaskException,  Simulate.WriteTaskException);
        }

        /// following strings are used as entity ids to invoke a handled <see cref="TaskError"/> via <see cref="TestContainer"/>
        private const string Article1ReadError      = "article-1";
        private const string Article2JsonError      = "article-2"; 
     // private const string ReadEntityError        = "read-entity-error"; 
        private const string DeleteEntityError      = "delete-entity-error";

        private const string ReadTaskError          = "read-task-error";
        private const string CreateTaskError        = "create-task-error";
        private const string UpdateTaskError        = "update-task-error";
        private const string DeleteTaskError        = "delete-task-error";
            
        /// following strings are used as entity ids to invoke an <see cref="TaskErrorType.UnhandledException"/> via <see cref="TestContainer"/>
        /// These test assertions ensure that all unhandled exceptions (bugs) are caught in a <see cref="EntityContainer"/> implementation.
        private const string ReadTaskException      = "read-task-exception"; // throws an exception also for a Query
        private const string CreateTaskException    = "create-task-exception";
        private const string UpdateTaskException    = "update-task-exception";
        private const string DeleteTaskException    = "delete-task-exception";
        

        private static async Task TestStoresErrors(PocStore useStore) {
            await AssertQueryTask       (useStore);
            await AssertReadTask        (useStore);
            await AssertTaskExceptions  (useStore);
            await AssertTaskError       (useStore);
            await AssertEntityWrite     (useStore);
        }

        private const string ArticleError = @"Task failed by entity errors. Count: 2
| ReadError - Article 'article-1', simulated read error
| ParseError - Article 'article-2', JsonParser/JSON error: Expected ':' after key. Found: X path: 'invalidJson' at position: 16";
        
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


            var taskEntityError = (TaskEntityError)allArticles.GetTaskError();
            AreEqual(2, taskEntityError.entityErrors.Count);
            AreEqual("type: EntityErrors, message: Task failed by entity errors", taskEntityError.ToString());
            
            
            TaskResultException te;
            te = Throws<TaskResultException>(() => { var _ = allArticles.Results; });
            AreEqual(ArticleError, te.Message);
            AreEqual(2, te.entityErrors.Count);
            
            te = Throws<TaskResultException>(() => { var _ = allArticles.Results["article-galaxy"]; });
            AreEqual(ArticleError, te.Message);
            AreEqual(2, te.entityErrors.Count);
            
            AreEqual(1,                 hasOrderCamera.Results.Count);
            AreEqual(3,                 hasOrderCamera["order-1"].items.Count);
    
            AreEqual("customer-1",      customer.Id);
            AreEqual("Smith Ltd.",      customer.Result.name);
                
            te = Throws<TaskResultException>(() => { var _ = producersTask.Results; });
            AreEqual(ArticleError, te.Message);
            AreEqual(2, te.entityErrors.Count);
                
            te = Throws<TaskResultException>(() => { var _ = producerEmployees.Results; });
            AreEqual(ArticleError, te.Message);
            AreEqual(2, te.entityErrors.Count);
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

            var readTask        = store.articles.Read();
            var duplicateId     = "article-galaxy"; // support duplicate ids
            var galaxy          = readTask.Find(duplicateId);
                                  readTask.FindRange(new [] {Article1ReadError, Article2JsonError});
            var articleSet      = readTask.FindRange(new [] {duplicateId, duplicateId, "article-ipad"});

            await store.Sync(); // -------- Sync --------
        
            AreEqual(2,                 articleRefsTask.Results.Count);
            
            AreEqual(0,                 articleProducerTask.Results.Count);

            TaskResultException te;
            te = Throws<TaskResultException>(() => { var _ = articleSet.Results; });
            AreEqual(ArticleError, te.Message);
            AreEqual(2, te.entityErrors.Count);
            
            te = Throws<TaskResultException>(() => { var _ = galaxy.Result; });
            AreEqual(ArticleError, te.Message);
            AreEqual(2, te.entityErrors.Count);
            
            te = Throws<TaskResultException>(() => { var _ = readTask.Results; });
            AreEqual(ArticleError, te.Message);
            AreEqual(2, te.entityErrors.Count);
        }

        private static async Task AssertTaskExceptions(PocStore store) {
            var customers = store.customers;

            var readCustomers = customers.Read();
            var customerRead = readCustomers.Find(ReadTaskException);

            var customerQuery = customers.Query(c => c.id == "query-task-exception");

            var createError = customers.Create(new Customer{id = CreateTaskException});
            
            var updateError = customers.Update(new Customer{id = UpdateTaskException});
            
            var deleteError = customers.Delete(new Customer{id = DeleteTaskException});
            
            Exception e;
            e = Throws<TaskNotSyncedException>(() => { var _ = createError.Success; });
            AreEqual("SyncTask.Success requires Sync(). CreateTask<Customer> id: create-task-exception", e.Message);
            e = Throws<TaskNotSyncedException>(() => { var _ = createError.GetTaskError(); });
            AreEqual("SyncTask.GetTaskError() requires Sync(). CreateTask<Customer> id: create-task-exception", e.Message);
            e = Throws<TaskNotSyncedException>(() => { var _ = createError.GetEntityErrors(); });
            AreEqual("SyncTask.GetEntityErrors() requires Sync(). CreateTask<Customer> id: create-task-exception", e.Message);


            await store.Sync(); // -------- Sync --------
            
            TaskResultException te;
            te = Throws<TaskResultException>(() => { var _ = customerRead.Result; });
            AreEqual("Task failed. type: UnhandledException, message: SimulationException: EntityContainer read exception", te.Message);
            AreEqual(TaskErrorType.UnhandledException, te.taskError.type);
            AreEqual("type: UnhandledException, message: SimulationException: EntityContainer read exception", te.taskError.ToString());

            te = Throws<TaskResultException>(() => { var _ = customerQuery.Results; });
            AreEqual("Task failed. type: UnhandledException, message: SimulationException: EntityContainer query exception", te.Message);
            AreEqual(TaskErrorType.UnhandledException, te.taskError.type);

            IsFalse(createError.Success);
            AreEqual(TaskErrorType.UnhandledException, createError.GetTaskError().type);
            AreEqual("SimulationException: EntityContainer write exception", createError.GetTaskError().message);
            
            IsFalse(updateError.Success);
            AreEqual(TaskErrorType.UnhandledException, updateError.GetTaskError().type);
            AreEqual("SimulationException: EntityContainer write exception", updateError.GetTaskError().message);
            
            IsFalse(deleteError.Success);
            AreEqual(TaskErrorType.UnhandledException, deleteError.GetTaskError().type);
            AreEqual("SimulationException: EntityContainer write exception", deleteError.GetTaskError().message);
        }
        
        private static async Task AssertTaskError(PocStore store) {
            var customers = store.customers;

            var readCustomers = customers.Read();
            var customerRead = readCustomers.Find(ReadTaskError);
            
            var customerQuery = customers.Query(c => c.id == "query-task-error");
            
            var createError = customers.Create(new Customer{id = CreateTaskError});
            
            var updateError = customers.Update(new Customer{id = UpdateTaskError});
            
            var deleteError = customers.Delete(new Customer{id = DeleteTaskError});

            await store.Sync(); // -------- Sync --------

            TaskResultException te;
            te = Throws<TaskResultException>(() => { var _ = customerRead.Result; });
            AreEqual("Task failed. type: DatabaseError, message: simulated read error", te.Message);
            AreEqual(TaskErrorType.DatabaseError, customerRead.GetTaskError().type);
            
            te = Throws<TaskResultException>(() => { var _ = customerQuery.Results; });
            AreEqual("Task failed. type: DatabaseError, message: simulated query error", te.Message);
            AreEqual(TaskErrorType.DatabaseError, customerQuery.GetTaskError().type);
            
            IsFalse(createError.Success);
            AreEqual(TaskErrorType.DatabaseError, createError.GetTaskError().type);
            AreEqual("simulated write error", createError.GetTaskError().message);
            
            IsFalse(updateError.Success);
            AreEqual(TaskErrorType.DatabaseError, updateError.GetTaskError().type);
            AreEqual("simulated write error", updateError.GetTaskError().message);
            
            IsFalse(deleteError.Success);
            AreEqual(TaskErrorType.DatabaseError, deleteError.GetTaskError().type);
            AreEqual("simulated write error", deleteError.GetTaskError().message);
        }
        
        private static async Task AssertEntityWrite(PocStore store) {
            var customers = store.customers;
            
            var deleteError = customers.Delete(new Customer{id = DeleteEntityError});
            
            await store.Sync(); // -------- Sync --------
            
            IsFalse(deleteError.Success);
            var deleteErrors = deleteError.GetEntityErrors();
            AreEqual(1,     deleteErrors.Count);
            AreEqual("WriteError - Customer 'delete-entity-error', simulated write entity error", deleteErrors[DeleteEntityError].ToString());
        }
    }
}
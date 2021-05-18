using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Database.Remote;
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

// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph
{
    public class TestStoreErrors : LeakTestsFixture
    {
        /// withdraw from allocation detection by <see cref="LeakTestsFixture"/> => init before tracking starts
        static TestStoreErrors() { SyncTypeStore.Init(); }

        
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
        
        [UnityTest] public IEnumerator LoopbackUseCoroutine() { yield return RunAsync.Await(LoopbackUse()); }
        [Test]      public async Task  LoopbackUseAsync() { await LoopbackUse(); }
        
        private async Task LoopbackUse() {
            using (var fileDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var testDatabase     = new TestDatabase(fileDatabase))
            using (var loopbackDatabase = new LoopbackDatabase(testDatabase))
            {
                AddSimulationErrors(testDatabase);
                using (var useStore         = new PocStore(loopbackDatabase)) {
                    await TestStoresErrors(useStore);
                }
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
            customers.writeErrors.Add(CreateEntityError,    Simulate.WriteEntityError);
            customers.writeErrors.Add(UpdateEntityError,    Simulate.WriteEntityError);
            customers.writeErrors.Add(PatchWriteEntityError,Simulate.WriteEntityError);
            customers.readErrors. Add(PatchReadEntityError, Simulate.ReadEntityError);

            customers.queryErrors.Add(".id == 'query-task-exception'",  Simulate.QueryTaskException); // == Query(c => c.id == "query-task-exception")
            customers.queryErrors.Add(".id == 'query-task-error'",      Simulate.QueryTaskError);     // == Query(c => c.id == "query-task-error")
            
            customers.writeErrors.Add(CreateTaskError,      Simulate.WriteTaskError);
            customers.writeErrors.Add(UpdateTaskError,      Simulate.WriteTaskError);
            customers.writeErrors.Add(DeleteTaskError,      Simulate.WriteTaskError);
            customers.writeErrors.Add(PatchTaskError,       Simulate.WriteTaskError);

            customers.writeErrors.Add(CreateTaskException,  Simulate.WriteTaskException);
            customers.writeErrors.Add(UpdateTaskException,  Simulate.WriteTaskException);
            customers.writeErrors.Add(DeleteTaskException,  Simulate.WriteTaskException);
            customers.writeErrors.Add(PatchTaskException,   Simulate.WriteTaskException);
        }

        /// following strings are used as entity ids to invoke a handled <see cref="TaskError"/> via <see cref="TestContainer"/>
        private const string Article1ReadError      = "article-1";
        private const string Article2JsonError      = "article-2"; 
     // private const string ReadEntityError        = "read-entity-error"; 
        private const string DeleteEntityError      = "delete-entity-error";
        private const string CreateEntityError      = "create-entity-error";
        private const string UpdateEntityError      = "update-entity-error";
        
        private const string PatchReadEntityError   = "patch-read-entity-error";
        private const string PatchWriteEntityError  = "patch-write-entity-error";

        private const string ReadTaskError          = "read-task-error";
        private const string CreateTaskError        = "create-task-error";
        private const string UpdateTaskError        = "update-task-error";
        private const string DeleteTaskError        = "delete-task-error";
        private const string PatchTaskError         = "patch-task-error";
            
        /// following strings are used as entity ids to invoke an <see cref="TaskErrorType.UnhandledException"/> via <see cref="TestContainer"/>
        /// These test assertions ensure that all unhandled exceptions (bugs) are caught in a <see cref="EntityContainer"/> implementation.
        private const string ReadTaskException      = "read-task-exception"; // throws an exception also for a Query
        private const string CreateTaskException    = "create-task-exception";
        private const string UpdateTaskException    = "update-task-exception";
        private const string DeleteTaskException    = "delete-task-exception";
        private const string PatchTaskException     = "patch-task-exception";
        

        private static async Task TestStoresErrors(PocStore useStore) {
            await AssertQueryTask       (useStore);
            await AssertReadTask        (useStore);
            await AssertTaskExceptions  (useStore);
            await AssertTaskError       (useStore);
            await AssertEntityWrite     (useStore);
            await AssertEntityPatch     (useStore);
        }

        private const string ArticleError = @"Task failed by entity errors. Count: 2
| ReadError: Article 'article-1', simulated read entity error
| ParseError: Article 'article-2', JsonParser/JSON error: Expected ':' after key. Found: X path: 'invalidJson' at position: 16";
        
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

            var orderCustomer = orders.MemberRef(o => o.customer);
            ReadRefTask<Customer> customer  = readOrders.ReadRefPath(orderCustomer);
            ReadRefTask<Customer> customer2 = readOrders.ReadRefPath(orderCustomer);
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

            var sync = await store.TrySync(); // -------- Sync --------
            IsFalse(sync.Success);
            AreEqual("tasks: 7, failed: 1", sync.ToString());
            AreEqual(7, sync.tasks.Count);
            AreEqual(1, sync.failed.Count);
            const string msg = @"Sync() failed with task errors. Count: 1
| QueryTask<Article> filter: true - Task failed by entity errors. Count: 2
| | ReadError: Article 'article-1', simulated read entity error
| | ParseError: Article 'article-2', JsonParser/JSON error: Expected ':' after key. Found: X path: 'invalidJson' at position: 16";
            AreEqual(msg, sync.Message);
            
            AreEqual(1,                 ordersWithCustomer1.Results.Count);
            NotNull(ordersWithCustomer1["order-1"]);
            
            AreEqual(1,                 ordersAnyAmountLower2.Results.Count);
            NotNull(ordersAnyAmountLower2["order-1"]);
            
            AreEqual(1,                 ordersAllAmountGreater0.Results.Count);
            NotNull(ordersAllAmountGreater0["order-1"]);


            var taskEntityError = allArticles.Error;
            AreEqual(2, taskEntityError.entityErrors.Count);
            AreEqual("type: EntityErrors, message: Task failed by entity errors, entityErrors: 2", taskEntityError.ToString());
            
            
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
            var orderCustomer = orders.MemberRef(o => o.customer);
            e = Throws<TaskAlreadySyncedException>(() => { readOrders.ReadRefPath(orderCustomer); });
            AreEqual("Task already synced. ReadTask<Order> #ids: 1", e.Message);
            var itemsArticle = orders.MemberRefs(o => o.items.Select(a => a.article));
            e = Throws<TaskAlreadySyncedException>(() => { readOrders.ReadRefsPath(itemsArticle); });
            AreEqual("Task already synced. ReadTask<Order> #ids: 1", e.Message);
            
            // todo add Read() without ids 

            readOrders = orders.Read();
            readOrders.Find("order-1");
            ReadRefsTask<Article> articleRefsTask  = readOrders.ReadRefsPath(itemsArticle);
            ReadRefsTask<Article> articleRefsTask2 = readOrders.ReadRefsPath(itemsArticle);
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
            var article1And2    = readTask.FindRange(new [] {Article1ReadError, Article2JsonError});
            var articleSet      = readTask.FindRange(new [] {duplicateId, duplicateId, "article-ipad"});
            
            var readTask2       = store.articles.Read(); // separate Read without errors
            var galaxy2         = readTask2.Find(duplicateId);

            try {
                // test throwing exception in case of errors
                await store.Sync(); // -------- Sync --------
                Fail("Sync() intended to fail - code cannot be reached");
            } catch (SyncResultException sre) {
                AreEqual(1, sre.failed.Count);
                const string expect = @"Sync() failed with task errors. Count: 1
| ReadTask<Article> #ids: 4 - Task failed by entity errors. Count: 2
| | ReadError: Article 'article-1', simulated read entity error
| | ParseError: Article 'article-2', JsonParser/JSON error: Expected ':' after key. Found: X path: 'invalidJson' at position: 16";
                AreEqual(expect, sre.Message);
            }
        
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
            
            te = Throws<TaskResultException>(() => { var _ = article1And2.Results; });
            AreEqual(ArticleError, te.Message);
            AreEqual(2, te.entityErrors.Count);
            
            AreEqual("Galaxy S10", galaxy2.Result.name); 
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
            AreEqual("SyncTask.Success requires Sync(). CreateTask<Customer> #ids: 1", e.Message);
            e = Throws<TaskNotSyncedException>(() => { var _ = createError.Error; });
            AreEqual("SyncTask.Error requires Sync(). CreateTask<Customer> #ids: 1", e.Message);

            var sync = await store.TrySync(); // -------- Sync --------
            AreEqual("tasks: 5, failed: 5", sync.ToString());
            
            TaskResultException te;
            te = Throws<TaskResultException>(() => { var _ = customerRead.Result; });
            AreEqual("Task failed. type: UnhandledException, message: SimulationException: simulated read task exception", te.Message);
            AreEqual(TaskErrorType.UnhandledException, te.taskError);
            AreEqual("Task failed. type: UnhandledException, message: SimulationException: simulated read task exception", te.Message);

            te = Throws<TaskResultException>(() => { var _ = customerQuery.Results; });
            AreEqual("Task failed. type: UnhandledException, message: SimulationException: simulated query exception", te.Message);
            AreEqual(TaskErrorType.UnhandledException, te.taskError);

            IsFalse(createError.Success);
            AreEqual(TaskErrorType.UnhandledException, createError.Error.type);
            AreEqual("SimulationException: simulated write task exception", createError.Error.message);
            
            IsFalse(updateError.Success);
            AreEqual(TaskErrorType.UnhandledException, updateError.Error.type);
            AreEqual("SimulationException: simulated write task exception", updateError.Error.message);
            
            IsFalse(deleteError.Success);
            AreEqual(TaskErrorType.UnhandledException, deleteError.Error.type);
            AreEqual("SimulationException: simulated write task exception", deleteError.Error.message);
        }
        
        private static async Task AssertTaskError(PocStore store) {
            var customers = store.customers;

            var readCustomers = customers.Read();
            var customerRead = readCustomers.Find(ReadTaskError);
            
            var customerQuery = customers.Query(c => c.id == "query-task-error");
            
            var createError = customers.Create(new Customer{id = CreateTaskError});
            
            var updateError = customers.Update(new Customer{id = UpdateTaskError});
            
            var deleteError = customers.Delete(new Customer{id = DeleteTaskError});
            
            var sync = await store.TrySync(); // -------- Sync --------
            AreEqual("tasks: 5, failed: 5", sync.ToString());

            TaskResultException te;
            te = Throws<TaskResultException>(() => { var _ = customerRead.Result; });
            AreEqual("Task failed. type: DatabaseError, message: simulated read task error", te.Message);
            AreEqual(TaskErrorType.DatabaseError, customerRead.Error.type);
            
            te = Throws<TaskResultException>(() => { var _ = customerQuery.Results; });
            AreEqual("Task failed. type: DatabaseError, message: simulated query error", te.Message);
            AreEqual(TaskErrorType.DatabaseError, customerQuery.Error.type);
            
            IsFalse(createError.Success);
            AreEqual(TaskErrorType.DatabaseError, createError.Error.type);
            AreEqual("simulated write task error", createError.Error.message);
            
            IsFalse(updateError.Success);
            AreEqual(TaskErrorType.DatabaseError, updateError.Error.type);
            AreEqual("simulated write task error", updateError.Error.message);
            
            IsFalse(deleteError.Success);
            AreEqual(TaskErrorType.DatabaseError, deleteError.Error.type);
            AreEqual("simulated write task error", deleteError.Error.message);
        }
        
        private static async Task AssertEntityWrite(PocStore store) {
            var customers = store.customers;
            
            var createError = customers.Create(new Customer{id = CreateEntityError});
            
            var updateError = customers.Update(new Customer{id = UpdateEntityError});
            
            var deleteError = customers.Delete(new Customer{id = DeleteEntityError});

            var sync = await store.TrySync(); // -------- Sync --------
            AreEqual("tasks: 3, failed: 3", sync.ToString());
            
            IsFalse(deleteError.Success);
            var deleteErrors = deleteError.Error.entityErrors;
            AreEqual(1,        deleteErrors.Count);
            AreEqual("WriteError: Customer 'delete-entity-error', simulated write entity error", deleteErrors[DeleteEntityError].ToString());
            
            IsFalse(createError.Success);
            var createErrors = createError.Error.entityErrors;
            AreEqual(1,        createErrors.Count);
            AreEqual("WriteError: Customer 'create-entity-error', simulated write entity error", createErrors[CreateEntityError].ToString());
            
            IsFalse(updateError.Success);
            var updateErrors = updateError.Error.entityErrors;
            AreEqual(1,        updateErrors.Count);
            AreEqual("WriteError: Customer 'update-entity-error', simulated write entity error", updateErrors[UpdateEntityError].ToString());
        }

        private static async Task AssertEntityPatch(PocStore store) {
            var customers = store.customers;
            const string unknownId = "unknown-id";
            
            var patchNotFound       = customers.Patch (new Customer{id = unknownId});
            
            var patchReadError      = customers.Patch (new Customer{id = PatchReadEntityError});
            
            var patchWriteError     = customers.Patch (new Customer{id = PatchWriteEntityError});
            
            var sync = await store.TrySync(); // -------- Sync --------
            AreEqual("tasks: 3, failed: 3", sync.ToString());
            
            {
                IsFalse(patchNotFound.Success);
                AreEqual(TaskErrorType.EntityErrors, patchNotFound.Error.type);
                var patchErrors = patchNotFound.Error.entityErrors;
                AreEqual("PatchError: Customer 'unknown-id', patch target not found", patchErrors[unknownId].ToString());
            } {
                IsFalse(patchReadError.Success);
                AreEqual(TaskErrorType.EntityErrors, patchReadError.Error.type);
                var patchErrors = patchReadError.Error.entityErrors;
                AreEqual("ReadError: Customer 'patch-read-entity-error', simulated read entity error", patchErrors[PatchReadEntityError].ToString());
            } {
                IsFalse(patchWriteError.Success);
                AreEqual(TaskErrorType.EntityErrors, patchWriteError.Error.type);
                var patchErrors = patchWriteError.Error.entityErrors;
                AreEqual("WriteError: Customer 'patch-write-entity-error', simulated write entity error", patchErrors[PatchWriteEntityError].ToString());
            }
            
            // --- test read task error
            {
                var patchTaskReadError = customers.Patch(new Customer {id = ReadTaskError});

                sync = await store.TrySync(); // -------- Sync --------
                AreEqual("tasks: 1, failed: 1", sync.ToString());

                IsFalse(patchTaskReadError.Success);
                AreEqual(TaskErrorType.DatabaseError, patchTaskReadError.Error.type);
                AreEqual("simulated read task error", patchTaskReadError.Error.message);
            }

            // --- test read task exception
            {
                var patchTaskReadException = customers.Patch(new Customer {id = ReadTaskException});

                sync = await store.TrySync(); // -------- Sync --------
                AreEqual("tasks: 1, failed: 1", sync.ToString());

                IsFalse(patchTaskReadException.Success);
                AreEqual(TaskErrorType.UnhandledException, patchTaskReadException.Error.type);
                AreEqual("SimulationException: simulated read task exception", patchTaskReadException.Error.message);
            }
            
            // --- test write task error
            {
                var patchTaskWriteError = customers.Patch(new Customer {id = PatchTaskError});

                sync = await store.TrySync(); // -------- Sync --------
                AreEqual("tasks: 1, failed: 1", sync.ToString());

                IsFalse(patchTaskWriteError.Success);
                AreEqual(TaskErrorType.DatabaseError, patchTaskWriteError.Error.type);
                AreEqual("simulated write task error", patchTaskWriteError.Error.message);
            }
            
            // --- test write task exception
            {
                var patchTaskWriteException = customers.Patch(new Customer {id = PatchTaskException});

                sync = await store.TrySync(); // -------- Sync --------
                AreEqual("tasks: 1, failed: 1", sync.ToString());

                IsFalse(patchTaskWriteException.Success);
                AreEqual(TaskErrorType.UnhandledException, patchTaskWriteException.Error.type);
                AreEqual("SimulationException: simulated write task exception", patchTaskWriteException.Error.message);
            }

        }
    }
}
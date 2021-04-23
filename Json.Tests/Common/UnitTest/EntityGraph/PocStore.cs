using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.EntityGraph;
using Friflo.Json.EntityGraph.Database;
using static NUnit.Framework.Assert;


namespace Friflo.Json.Tests.Common.UnitTest.EntityGraph
{
    // ------------------------------ models ------------------------------
    public class Order : Entity {
        public Ref<Customer>    customer;
        public List<OrderItem>  items = new List<OrderItem>();
    }

    public class OrderItem {
        public Ref<Article>     article;
        public int              amount;
        public string           name;
    }

    public class Article : Entity
    {
        public string           name;
        public Ref<Producer>    producer;
    }

    public class Customer : Entity {
        public string           lastName;
    }
    
    public class Producer : Entity {
        public string           name;
    }

    // --- store containers
    public class PocStore : EntityStore
    {
        public PocStore(EntityDatabase database) : base (database) {
            orders      = new EntitySet<Order>       (this);
            customers   = new EntitySet<Customer>    (this);
            articles    = new EntitySet<Article>     (this);
            producers   = new EntitySet<Producer>     (this);
        }

        public readonly EntitySet<Order>      orders;
        public readonly EntitySet<Customer>   customers;
        public readonly EntitySet<Article>    articles;
        public readonly EntitySet<Producer>   producers;
    }
        
    // --------------------------------------------------------------------
    public static class TestRelationPoC
    {
        public static async Task<PocStore> CreateStore(EntityDatabase database) {
            var store = new PocStore(database);
            var orders      = store.orders;
            var articles    = store.articles;
            var producers   = store.producers;
            
            
            var samsung   = new Producer { id = "producer-samsung", name = "Samsung"};
            var galaxy    = new Article  { id = "article-galaxy", name = "Galaxy S10", producer = samsung};
            articles.Create(galaxy);
            producers.Create(samsung); // todo remove - should be created implicit by galaxy
            
            var apple     = new Producer { id = "producer-apple", name = "Apple"};
            producers.Create(apple);
            articles.Delete("article-iphone"); // delete if exist in database
            

            await store.Sync(); // -------- Sync --------
            
            var order       = new Order { id = "order-1" };
            var cameraCreate    = new Article { id = "article-1", name = "Camera" };
            var createCam1 = articles.Create(cameraCreate);
            var createCam2 = articles.Create(cameraCreate);   // Create() is idempotent
            AreSame(createCam1, createCam2);                       // test redundant create
            AreEqual("article-1", createCam1.ToString());
            
            for (int n = 0; n < 1; n++) {
                var id = $"bulk-article-{n:D4}";
                var newArticle = new Article { id = id, name = id };
                articles.Create(newArticle);
            }

            var cameraUnknown = articles.Read("article-missing");
            var camera =        articles.Read("article-1");
            
            var camForDelete    = new Article { id = "article-delete", name = "Camera-Delete" };
            articles.Create(camForDelete);
            AreEqual("peers: 7, tasks: 2",                          store.ToString());
            AreEqual("peers: 5, tasks: 2 -> create #3, read #2",    articles.ToString());
            AreEqual("peers: 2",                                    producers.ToString());
            
            await store.Sync(); // -------- Sync --------
            
            AreEqual("peers: 6",                                    store.ToString()); // "article-missing" peer removed
            
            cameraCreate.name = "Changed name";
            AreEqual(1, articles.LogEntityChanges(cameraCreate));
            AreEqual(1, articles.LogSetChanges());
            AreEqual(1, store.LogChanges());
            AreEqual(1, store.LogChanges());       // SaveChanges() is idempotent => state did not change

            articles.Delete(camForDelete.id);
            
            await store.Sync(); // -------- Sync --------

            var cameraNotSynced = articles.Read("article-1");
            var e = Throws<TaskNotSyncedException>(() => { var res = cameraNotSynced.Result; });
            AreEqual("ReadTask.Result requires Sync(). ReadTask<Article> id: article-1", e.Message);
            
            IsNull(cameraUnknown.Result);
            AreSame(camera.Result, cameraCreate);
            
            var customer    = new Customer { id = "customer-1", lastName = "Smith" };
            // customers.Create(customer);    // redundant - implicit tracked by order
            
            var item1       = new OrderItem {
                article = camera.Result,
                amount = 1,
                name = "Camera"
            };
            order.items.Add(item1);

            var smartphone    = new Article { id = "article-2", name = "Smartphone" };
            // articles.Create(smartphone);   // redundant - implicit tracked by order
            
            var item2       = new OrderItem {
                article = smartphone,
                amount = 2,
                name = smartphone.name
            };
            order.items.Add(item2);
            
            var item3       = new OrderItem {
                article = camera.Result,
                amount = 3,
                name = "Camera"
            };
            order.items.Add(item3);

            order.customer = customer;
            orders.Create(order);
            AreEqual(1, orders.LogSetChanges());
            AreEqual(3, store.LogChanges());
            AreEqual(3, store.LogChanges());       // SaveChanges() is idempotent => state did not change
            
            await store.Sync(); // -------- Sync --------
            
            return store;
        }
    }
}
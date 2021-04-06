using System;
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
    }

    public class Article : Entity
    {
        public string           name;
    }

    public class Customer : Entity {
        public string           lastName;
    }

    // --- store containers
    public class PocStore : EntityStore
    {
        public PocStore(EntityDatabase database) : base (database) {
            orders      = new EntitySet<Order>       (this);
            customers   = new EntitySet<Customer>    (this);
            articles    = new EntitySet<Article>     (this);
        }

        public readonly EntitySet<Order>      orders;
        public readonly EntitySet<Customer>   customers;
        public readonly EntitySet<Article>    articles;
    }
        
    // --------------------------------------------------------------------
    public static class TestRelationPoC
    {
        public static async Task<PocStore> CreateStore(EntityDatabase database) {
            var store = new PocStore(database);
            var order       = new Order { id = "order-1" };
            
            var cameraCreate    = new Article { id = "article-1", name = "Camera" };
            var createCam1 = store.articles.Create(cameraCreate);
            var createCam2 = store.articles.Create(cameraCreate);   // Create() is idempotent
            AreSame(createCam1, createCam2);                       // test redundant create
            
            for (int n = 0; n < 1; n++) {
                var id = $"bulk-article-{n:D4}";
                var newArticle = new Article { id = id, name = id };
                store.articles.Create(newArticle);
            }

            var cameraUnknown = store.articles.Read("article-unknown");
            var camera =        store.articles.Read("article-1");
            await store.Sync();
            
            cameraCreate.name = "Changed name";
            AreEqual(1, store.articles.LogEntityChanges(cameraCreate));
            AreEqual(1, store.articles.LogSetChanges());
            AreEqual(1, store.LogChanges());
            AreEqual(1, store.LogChanges());       // SaveChanges() is idempotent => state did not change
            await store.Sync();

            var cameraNotSynced = store.articles.Read("article-1");
            var e = Throws<PeerNotSyncedException>(() => { var res = cameraNotSynced.Result; });
            AreEqual("Read().Result requires Sync(). Entity: Article id: article-1", e.Message);
            
            IsNull(cameraUnknown.Result);
            AreSame(camera.Result, cameraCreate);
            
            var customer    = new Customer { id = "customer-1", lastName = "Smith" };
            // store.customers.Create(customer);    // redundant - implicit tracked by order
            
            var item1       = new OrderItem {
                article = camera.Result,
                amount = 1
            };
            order.items.Add(item1);

            var smartphone    = new Article { id = "article-2", name = "Smartphone" };
            // store.articles.Create(smartphone);   // redundant - implicit tracked by order
            
            var item2       = new OrderItem {
                article = smartphone,
                amount = 2
            };
            order.items.Add(item2);
            
            var item3       = new OrderItem {
                article = camera.Result,
                amount = 3
            };
            order.items.Add(item3);

            order.customer = customer;
            store.orders.Create(order);
            AreEqual(1, store.orders.LogSetChanges());
            AreEqual(3, store.LogChanges());
            AreEqual(3, store.LogChanges());       // SaveChanges() is idempotent => state did not change
            await store.Sync();
            return store;
        }
    }
}
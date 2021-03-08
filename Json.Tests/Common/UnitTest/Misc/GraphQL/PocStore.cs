using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Mapper.ER;
using Friflo.Json.Mapper.ER.Database;

namespace Friflo.Json.Tests.Common.UnitTest.Misc.GraphQL
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
        public static async Task<PocStore> CreateStore() {
            var database = new EntityDatabase(); 
            var store = new PocStore(database); 
            var order       = new Order { id = "order-1" };
            
            var customer    = new Customer { id = "customer-1", lastName = "Smith" };
            store.customers.Create(customer);

            var camera    = new Article { id = "article-1", name = "Camera" };
            store.articles.Create(camera);
            
            var item1       = new OrderItem {
                article = camera,
                amount = 1
            };
            order.items.Add(item1);

            var smartphone    = new Article { id = "article-2", name = "Smartphone" };
            store.articles.Create(smartphone);
            
            var item2       = new OrderItem {
                article = smartphone,
                amount = 2
            };
            order.items.Add(item2);
            
            var item3       = new OrderItem {
                article = camera,
                amount = 3
            };
            order.items.Add(item3);

            order.customer = customer;
            store.orders.Create(order);

            await store.Sync(); // todo test without Update()
            return store;
        }
    }
}
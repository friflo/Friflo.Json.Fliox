using System.Collections.Generic;
using Friflo.Json.Mapper.ER;

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

    // --- database containers
    public class PocCache : EntityCache
    {
        public PocCache() {
            AddContainer(orders);
            AddContainer(customers);
            AddContainer(articles);
        }
        public readonly EntityCacheContainer<Order>      orders      = new MemoryCacheContainer<Order>();
        public readonly EntityCacheContainer<Customer>   customers   = new MemoryCacheContainer<Customer>();
        public readonly EntityCacheContainer<Article>    articles    = new MemoryCacheContainer<Article>();
    }
        
    // --------------------------------------------------------------------
    public static class TestRelationPoC
    {
        public static PocCache CreateCache() {
            var store = new PocCache(); 
            var order       = new Order { id = "order-1" };
            store.orders.Add(order);
            
            var customer    = new Customer { id = "customer-1", lastName = "Smith" };
            store.customers.Add(customer);

            var camera    = new Article { id = "article-1", name = "Camera" };
            store.articles.Add(camera);
            
            var item1       = new OrderItem {
                article = camera,
                amount = 1
            };
            order.items.Add(item1);

            var smartphone    = new Article { id = "article-2", name = "Smartphone" };
            store.articles.Add(smartphone);
            
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
            return store;
        }
    }
}
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
    public class PocDatabase : EntityDatabase
    {
        public PocDatabase() {
            AddContainer(orders);
            AddContainer(customers);
            AddContainer(articles);
        }
        public readonly EntityContainer<Order>      orders      = new MemoryContainer<Order>();
        public readonly EntityContainer<Customer>   customers   = new MemoryContainer<Customer>();
        public readonly EntityContainer<Article>    articles    = new MemoryContainer<Article>();
    }
        
    // --------------------------------------------------------------------
    public static class TestRelationPoC
    {
        public static PocDatabase CreateDatabase() {
            var store = new PocDatabase(); 
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
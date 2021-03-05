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
            orders      = new MemoryContainer<Order>(this);
            customers   = new MemoryContainer<Customer>(this);
            articles    = new MemoryContainer<Article>(this);
        }

        public readonly EntityContainer<Order>      orders;
        public readonly EntityContainer<Customer>   customers;
        public readonly EntityContainer<Article>    articles;
    }
        
    // --------------------------------------------------------------------
    public static class TestRelationPoC
    {
        public static PocDatabase CreateDatabase() {
            var store = new PocDatabase(); 
            var order       = new Order { id = "order-1" };
            store.orders.Create(order);
            
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
            store.orders.Update(order); // todo test without Update()
            return store;
        }
    }
}
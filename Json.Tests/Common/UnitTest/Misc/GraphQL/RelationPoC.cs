using System.Collections.Generic;
using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Map;

namespace Friflo.Json.Tests.Common.UnitTest.Misc.GraphQL
{
    // ------------------------------ models ------------------------------
    public class Order : Entity {
        public Customer         customer;
        public List<OrderItem>  items = new List<OrderItem>();
    }

    public class OrderItem {
        public Article          article;
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
    public class PocDatabase : Database
    {
        public PocDatabase() {
            AddContainer(orders);
            AddContainer(customers);
            AddContainer(articles);
        }
        public readonly DatabaseContainer<Order>      orders      = new MemoryContainer<Order>();
        public readonly DatabaseContainer<Customer>   customers   = new MemoryContainer<Customer>();
        public readonly DatabaseContainer<Article>    articles    = new MemoryContainer<Article>();
    }
        
    // --------------------------------------------------------------------
    public static class TestRelationPoC
    {
        public static PocDatabase CreateDB() {
            var db = new PocDatabase();
            var order       = new Order { id = "order-1" };
            db.orders.Add(order);
            
            var customer    = new Customer { id = "customer-1", lastName = "Smith" };
            db.customers.Add(customer);

            var article1    = new Article { id = "article-1", name = "Camera" };
            db.articles.Add(article1);
            var item1       = new OrderItem {
                article = article1,
                amount = 1
            };
            order.items.Add(item1);

            var article2    = new Article { id = "article-2", name = "Smartphone" };
            db.articles.Add(article2);
            var item2       = new OrderItem {
                article = article2,
                amount = 2
            };
            order.items.Add(item2);

            order.customer = customer;
            return db;
        }
    }
}
using System.Collections.Generic;
using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Map;
using NUnit.Framework;

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
    public class TestRelationPoC
    {
        [Test]
        public void Run() {
        }
        
        public static Order CreateOrder(string orderId) {
            var order       = new Order { id = orderId };
            
            var customer    = new Customer { id = "customer-1", lastName = "Smith" };

            var article1    = new Article { id = "article-1", name = "Camera" };
            var item1       = new OrderItem {
                article = article1,
                amount = 1
            };
            order.items.Add(item1);

            var article2    = new Article { id = "article-2", name = "Smartphone" };
            var item2       = new OrderItem {
                article = article2,
                amount = 2
            };
            order.items.Add(item2);

            order.customer = customer;
            return order;
        }
    }
}
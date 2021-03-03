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
    public class PocStore : EntityStore
    {
        public PocStore() {
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
        public static Order CreateOrder() {
            var order       = new Order { id = "order-1" };
            
            var customer    = new Customer { id = "customer-1", lastName = "Smith" };

            var camera    = new Article { id = "article-1", name = "Camera" };
            var item1       = new OrderItem {
                article = camera,
                amount = 1
            };
            order.items.Add(item1);

            var smartphone    = new Article { id = "article-2", name = "Smartphone" };
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
            return order;
        }
    }
}
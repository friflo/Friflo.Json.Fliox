using System;
using System.Collections.Generic;
using Friflo.Json.Mapper;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Misc.GraphQL
{

    public class Database
    {
        private Dictionary<Type, IDatabaseCollection> collection = new Dictionary<Type, IDatabaseCollection>();

        public T CreateEntity<T>(string id) where T : Entity, new () {
            T entity = new T();
            entity.id = id;
            return entity;
        }
    }
    
    public interface IDatabaseCollection {
    }

    
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
        
    // --------------------------------------------------------------------
    public class TestRelationPoC
    {
        [Test]
        public void Run() {
        }
        
        public static Order CreateOrder(string orderId) {
            var db = new Database();

            var order       = db.CreateEntity<Order>(orderId);
            
            var customer    = db.CreateEntity<Customer>("customer-1");
            customer.lastName   = "Smith";

            var article1 = new Article { id = "article-1", name = "Camera" };
            var item1 = new OrderItem {
                article = article1,
                amount = 1
            };
            // assign as reference
            order.items.Add(item1);

            var article2 = new Article { id = "article-2", name = "Smartphone" };
            var item2 = new OrderItem {
                article = article2,
                amount = 2
            };
            // assign as id
            order.items.Add(item2);

            order.customer = customer;      // assign as reference
            return order;
        }
    }
}
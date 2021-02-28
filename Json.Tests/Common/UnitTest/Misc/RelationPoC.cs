using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Misc
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

    public class Entity {
        public string   id;
    }

    public class Ref<T> where T : Entity
    {
        public string   id;
        public T        entity;
        
        public static implicit operator Ref<T>(T entity) {
            var reference = new Ref<T>();
            reference.entity    = entity;
            reference.id        = entity.id;
            return reference;
        }
    }
    
    // ------------------------------ models ------------------------------
    class Order : Entity {
        public Ref<Customer>    customer;
        public List<OrderItem>  items = new List<OrderItem>();
    }

    class OrderItem {
        public Ref<Article>     article;
        public int              amount;
    }

    class Article : Entity
    {
        public string           name;
    }

    class Customer : Entity {
        public string           lastName;
    }
        
    // --------------------------------------------------------------------
    public class TestRelationPoC
    {
        [Test]
        public void Run() {
            var db = new Database();

            var order       = db.CreateEntity<Order>("order-1");
            
            var customer    = db.CreateEntity<Customer>("customer-1");
            customer.lastName   = "Smith";

            var article     = db.CreateEntity<Article>("article-1");
            article.name        = "Camera";
            
            var item = new OrderItem();
            item.article = article;     // assign reference
            order.items.Add(item);

            order.customer = customer;  // assign reference
        }
    }
}
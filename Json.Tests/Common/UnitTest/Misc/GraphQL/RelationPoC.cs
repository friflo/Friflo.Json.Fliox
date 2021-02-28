using System;
using System.Collections.Generic;
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

    public class Entity {
        public string   id;
    }

    public class Ref<T> where T : Entity
    {
        // either id or entity is set. Never both
        public string   id;
        public T        entity;
        
        public static implicit operator Ref<T>(T entity) {
            var reference = new Ref<T>();
            reference.entity    = entity;
            return reference;
        }
        
        public static implicit operator Ref<T>(string id) {
            var reference = new Ref<T>();
            reference.id    = id;
            return reference;
        }
    }


    
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
        
    // --------------------------------------------------------------------
    public class TestRelationPoC
    {
        [Test]
        public void Run() {
            var db = new Database();

            var order       = db.CreateEntity<Order>("order-1");
            
            var customer    = db.CreateEntity<Customer>("customer-1");
            customer.lastName   = "Smith";

            var camera     = db.CreateEntity<Article>("article-1");
            camera.name        = "Camera";

            var item1 = new OrderItem();
            item1.article = camera;         // assign as reference
            item1.amount = 1;
            order.items.Add(item1);
            
            var item2 = new OrderItem();
            item2.article = "article-2";    // assign as id
            item2.amount = 2;
            order.items.Add(item2);

            order.customer = customer;      // assign as reference
        }
    }
}
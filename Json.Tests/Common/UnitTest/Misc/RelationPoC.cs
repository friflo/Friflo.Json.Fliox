using System.Collections.Generic;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Misc
{

    public interface Database {
        
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
        public string           description;
    }

    class Customer : Entity {
        public string           lastName;
    }
        
    // --------------------------------------------------------------------
    public class TestRelationPoC
    {
        [Test]
        public void Run() {
            var order = new Order();
            var customer = new Customer();

            var item = new OrderItem();
            var article = new Article();
            item.article = article;     // assign reference
            order.items.Add(item);

            order.customer = customer;  // assign reference
        }
    }
}
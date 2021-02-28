using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Mapper;
using NUnit.Framework;
namespace Friflo.Json.Tests.Common.UnitTest.Misc.GraphQL
{
    public enum Field {
        None,
        Get,
    }
    
    // ------------------------------ select ------------------------------

    public class SelectOrder {
        public Field            id;
        public SelectCustomer   customer;
        public SelectOrderItem  items;
    }

    public class SelectOrderItem {
        public SelectArticle    article;
        public Field            amount;
    }

    public class SelectArticle
    {
        public Field            id;
        public Field            name;
    }

    public class SelectCustomer {
        public Field            id;
        public Field            lastName;
    }

    
    // --------------------------------------------------------------------
    
    public class Query {
        public static List<Order>       Order(SelectOrder select) {
            return null;
        }
        public static List<Article>     Article(SelectArticle select) {
            return null;
        }
        public static List<Customer>    Customer(SelectCustomer select) {
            return null;
        }
    }
    
    public class TestSelect
    {
        [Test]
        public void Run() {
            var orderSelect = new SelectOrder {
                id = Field.Get,
                customer = new SelectCustomer {
                    id = Field.Get,
                    lastName = Field.Get,
                },
                items = new SelectOrderItem {
                    amount = Field.Get,
                    article = new SelectArticle {
                        id =  Field.Get,
                        name = Field.Get
                    }
                }
            };
        }
        
        [Test]
        public void RunLinq() {
            var order1 = TestRelationPoC.CreateOrder("order-1");
            var order2 = TestRelationPoC.CreateOrder("order-2");
            var orders = new List<Order>();
            orders.Add(order1);
            orders.Add(order2);

            var query =
                from order in orders
                // where order.id == "order-1"
                select new Order {
                    id = order.id,
                    customer =  order.customer
                };

            using (var typeStore = new TypeStore())
            using (var m = new JsonMapper(typeStore)) {
                typeStore.typeResolver.AddGenericTypeMapper(RefMatcher.Instance);
                m.Pretty = true;
                var json = m.Write(orders);
                Console.WriteLine(json);
            }

        }
    }
}










using System.Collections.Generic;
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
            List<Order> orders =  Query.Order(orderSelect);
        }
    }
}










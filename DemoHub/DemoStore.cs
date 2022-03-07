using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;

using Req = Friflo.Json.Fliox.Mapper.Fri.RequiredAttribute;

// ReSharper disable All
namespace Friflo.Json.Fliox.DemoHub
{
    /// <summary>
    /// The <see cref="DemoStore"/> offer two functionalities: <br/>
    /// 1. Defines a database <b>schema</b> by declaring its containers and commands <br/>
    /// 2. Is a database <b>client</b> providing type-safe access to its containers and commands <br/>
    /// <br/>
    /// <i>Info</i>: Use command <b>demo.FakeRecords</b> to create fake records in various containers.
    /// </summary>
    /// <remarks>
    /// Its containers are fields or properties of type <see cref="EntitySet{TKey,T}"/>. <br/>
    /// Its commands are methods returning a <see cref="CommandTask{TResult}"/>. See ./DemoStore-commands.cs <br/>
    /// <see cref="DemoStore"/> instances can be used on server and client side.
    /// </remarks>
    public partial class DemoStore : FlioxClient {
        // --- containers
        public readonly EntitySet <long, Order>       orders;
        public readonly EntitySet <long, Customer>    customers;
        public readonly EntitySet <long, Article>     articles;
        public readonly EntitySet <long, Producer>    producers;
        public readonly EntitySet <long, Employee>    employees;

        public DemoStore(FlioxHub hub) : base (hub) { }
    }
    
    // ------------------------------ entity models ------------------------------
    public class Order {
        [Req]   public  long                id { get; set; }
                public  Ref<long, Customer> customer;
                public  DateTime            created;
                public  List<OrderItem>     items = new List<OrderItem>();
    }

    public class OrderItem {
        [Req]   public  Ref<long, Article>  article;
                public  int                 amount;
                public  string              name;
    }

    public class Article {
        [Req]   public  long                id { get; set; }
        [Req]   public  string              name;
                public  Ref<long, Producer> producer;
                public  DateTime?           created;
    }

    public class Customer {
        [Req]   public  long                id { get; set; }
        [Req]   public  string              name;
                public  DateTime?           created;
    }
    
    public class Producer {
        [Req]   public  long                        id { get; set; }
        [Req]   public  string                      name;
                public  List<Ref<long, Employee>>   employees;
                public  DateTime?                   created;
    }
    
    public class Employee {
        [Req]   public  long                id { get; set; }
        [Req]   public  string              firstName;
                public  string              lastName;
                public  DateTime?           created;
    }
}

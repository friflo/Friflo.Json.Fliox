using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Mapper;

using Req = Friflo.Json.Fliox.Mapper.Fri.RequiredAttribute;

// ReSharper disable All
namespace Friflo.Json.Fliox.DemoHub
{
    /// <summary>
    /// <see cref="DemoStore"/> extends <see cref="FlioxClient"/> to provide two functionalities: <br/>
    /// 1. Defines a database schema by declaring its containers and commands <br/>
    /// 2. Is a database client providing type safe access to its containers and commands <br/>
    /// <br/>
    /// Its containers are fields or properties of type <see cref="EntitySet{TKey,T}"/>. <br/>
    /// Its commands are methods returning a <see cref="CommandTask{TResult}"/>. See ./DemoStore-commands.cs
    /// </summary>
    public partial class DemoStore : FlioxClient
    {
        // --- containers
        public readonly EntitySet <Guid, Order>       orders;
        public readonly EntitySet <Guid, Customer>    customers;
        public readonly EntitySet <Guid, Article>     articles;
        public readonly EntitySet <Guid, Producer>    producers;
        public readonly EntitySet <Guid, Employee>    employees;

        public DemoStore(FlioxHub hub) : base (hub) { }
    }
    
    // ------------------------------ entity models ------------------------------
    public class Order {
        [Req]   public  Guid                    id { get; set; }
                public  Ref<Guid, Customer>     customer;
                public  DateTime                created;
                public  List<OrderItem>         items = new List<OrderItem>();
                        
        public override string                  ToString() => JsonSerializer.Serialize(this);
    }

    public class OrderItem {
        [Req]  public   Ref<Guid, Article>      article;
               public   int                     amount;
               public   string                  name;
                        
        public override string                  ToString() => JsonSerializer.Serialize(this);
    }

    public class Article
    {
        [Req]   public   Guid                   id { get; set; }
        [Req]   public   string                 name;
                public   Ref<Guid, Producer>    producer;
                        
        public override string                  ToString() => JsonSerializer.Serialize(this);
    }

    public class Customer {
        [Req]   public   Guid                   id { get; set; }
        [Req]   public   string                 name;
        
        public override string                  ToString() => JsonSerializer.Serialize(this);
    }
    
    public class Producer {
        [Req]   public  Guid                    id { get; set; }
        [Req]   public  string                  name;
        [Fri.Property (Name =                      "employees")]
                public  List<Ref<Guid, Employee>> employeeList;
                        
        public override string                  ToString() => JsonSerializer.Serialize(this);
    }
    
    public class Employee {
        [Req]   public  Guid                    id { get; set; }
        [Req]   public  string                  firstName;
                public  string                  lastName;
                        
        public override string                  ToString() => JsonSerializer.Serialize(this);
    }
}

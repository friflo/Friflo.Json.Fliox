using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable InconsistentNaming
// ReSharper disable UnassignedReadonlyField
namespace Friflo.Json.Fliox.DemoHub
{
    /// <summary>
    /// Define database schema by adding container fields or properties of type <see cref="EntitySet{TKey,T}"/>. <br/>
    /// Declare database commands by adding methods returning a <see cref="CommandTask{TResult}"/>.
    /// </summary>
    public class DemoStore : FlioxClient
    {
        // --- containers
        public readonly EntitySet <string, Order>       orders;
        public readonly EntitySet <string, Customer>    customers;
        public readonly EntitySet <string, Article>     articles;
        public readonly EntitySet <string, Producer>    producers;
        public readonly EntitySet <string, Employee>    employees;
        
        public DemoStore(FlioxHub hub): base (hub) { }
        
        // --- commands
        public CommandTask<double>    TestAdd (TestAdd add)  => SendCommand<TestAdd, double>(nameof(TestAdd), add);
        /// <summary> command handler for <see cref="TestSub"/> intentionally not implemented by <see cref="DemoHandler"/>. 
        /// Execution results in:<br/>
        /// <code>NotImplemented > no command handler for: 'TestSub' </code></summary>
        public CommandTask<double>    TestSub (TestSub add)  => SendCommand<TestSub, double>(nameof(TestSub), add);
    }

    // ------------------------------ models ------------------------------
    public class Order {
        [Fri.Required]  public  string                  id { get; set; }
                        public  Ref<string, Customer>   customer;
                        public  DateTime                created;
                        public  List<OrderItem>         items = new List<OrderItem>();
                        
        public override         string                  ToString() => JsonSerializer.Serialize(this);
    }

    public class OrderItem {
        [Fri.Required]  public  Ref<string, Article>    article;
                        public  int                     amount;
                        public  string                  name;
                        
        public override         string                  ToString() => JsonSerializer.Serialize(this);
    }

    public class Article
    {
        [Fri.Required]  public  string                  id { get; set; }
        [Fri.Required]  public  string                  name;
                        public  Ref<string, Producer>   producer;
                        
        public override         string                  ToString() => JsonSerializer.Serialize(this);
    }

    public class Customer {
        [Fri.Required]  public  string                  id { get; set; }
        [Fri.Required]  public  string                  name;
        
        public override         string                  ToString() => JsonSerializer.Serialize(this);
    }
    
    public class Producer {
        [Fri.Required]  public  string                      id { get; set; }
        [Fri.Required]  public  string                      name;
        [Fri.Property (Name =                              "employees")]
                        public  List<Ref<string, Employee>> employeeList;
                        
        public override         string                  ToString() => JsonSerializer.Serialize(this);
    }
    
    public class Employee {
        [Fri.Required]  public  string                  id { get; set; }
        [Fri.Required]  public  string                  firstName;
                        public  string                  lastName;
                        
        public override         string                  ToString() => JsonSerializer.Serialize(this);
    }
   
    
    // ------------------------------ command params / results ------------------------------
    public class TestAdd {
        public          double  left;
        public          double  right;
    }
    
    public class TestSub {
        public          double  left;
        public          double  right;
    }
}

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Mapper;

using Req = Friflo.Json.Fliox.Mapper.Fri.RequiredAttribute;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable UnassignedField.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming
// ReSharper disable UnassignedReadonlyField
namespace Friflo.Json.Fliox.DemoHub
{
    /// <summary>
    /// <see cref="DemoStore"/> extends <see cref="FlioxClient"/> to provide two functionalities: <br/>
    /// 1. Defines a database schema be declaring its containers and commands <br/>
    /// 2. Is a database client with type safe access to its containers and commands <br/>  
    /// <br/> 
    /// Its containers are fields or properties of type <see cref="EntitySet{TKey,T}"/>. <br/>
    /// Its commands are methods returning a <see cref="CommandTask{TResult}"/>.
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
        /// <summary> Generate random entities (records) in the containers listed in the <see cref="Fake"/> param </summary> 
        public CommandTask<FakeResult>DemoFake (Fake        param)      => SendCommand<Fake, FakeResult>(nameof(DemoFake), param);
        
        /// <summary> simple command adding two numbers - no database access. </summary>
        public CommandTask<double>    DemoAdd  (Operands    param)      => SendCommand<Operands, double>(nameof(DemoAdd),  param);
        
        /// <summary> simple command multiplying two numbers - no database access. </summary>
        public CommandTask<double>    DemoMul  (Operands    param)      => SendCommand<Operands, double>(nameof(DemoMul),  param);
        
        /// <summary> command handler for <see cref="DemoSub_NotImpl"/> intentionally not implemented by <see cref="DemoHandler"/>. 
        /// Execution results in:
        /// <code>NotImplemented > no command handler for: 'DemoSub_NotImpl' </code></summary>
        public CommandTask<double>    DemoSub_NotImpl (Operands param)  => SendCommand<Operands, double>(nameof(DemoSub_NotImpl), param);
    }

    // ------------------------------ models ------------------------------
    public class Order {
        [Req]   public  string                  id { get; set; }
                public  Ref<string, Customer>   customer;
                public  DateTime                created;
                public  List<OrderItem>         items = new List<OrderItem>();
                        
        public override string                  ToString() => JsonSerializer.Serialize(this);
    }

    public class OrderItem {
        [Req]  public   Ref<string, Article>    article;
               public   int                     amount;
               public   string                  name;
                        
        public override string                  ToString() => JsonSerializer.Serialize(this);
    }

    public class Article
    {
        [Req]   public   string                 id { get; set; }
        [Req]   public   string                 name;
                public   Ref<string, Producer>  producer;
                        
        public override string                  ToString() => JsonSerializer.Serialize(this);
    }

    public class Customer {
        [Req]   public   string                 id { get; set; }
        [Req]   public   string                 name;
        
        public override string                  ToString() => JsonSerializer.Serialize(this);
    }
    
    public class Producer {
        [Req]   public  string                  id { get; set; }
        [Req]   public  string                  name;
        [Fri.Property (Name =                      "employees")]
                public  List<Ref<string, Employee>> employeeList;
                        
        public override string                  ToString() => JsonSerializer.Serialize(this);
    }
    
    public class Employee {
        [Req]   public  string                  id { get; set; }
        [Req]   public  string                  firstName;
                public  string                  lastName;
                        
        public override string                  ToString() => JsonSerializer.Serialize(this);
    }
   
    
    // ------------------------------ command params / results ------------------------------
    public class Operands {
        public  double      left;
        public  double      right;
    }
    
    public class Fake {
        public  int?        orders;
        public  int?        customers;
        public  int?        articles;
        public  int?        producers;
        public  int?        employees;
    }
    
    public class FakeResult {
        public  Fake        counts;
        public  Order[]     orders;
        public  Customer[]  customers;
        public  Article[]   articles;
        public  Producer[]  producers;
        public  Employee[]  employees;
    }
   
}

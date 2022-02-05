// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

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
    public class DemoStore : FlioxClient
    {
        public readonly EntitySet <string, Order>       orders;
        public readonly EntitySet <string, Customer>    customers;
        public readonly EntitySet <string, Article>     articles;
        public readonly EntitySet <string, Producer>    producers;
        public readonly EntitySet <string, Employee>    employees;
        
        public DemoStore(FlioxHub hub): base (hub) { }
        
        [Fri.Command(Name =        "TestCommand")]
        public CommandTask<bool>    Test (TestCommand command)                 => SendCommand<TestCommand, bool>("TestCommand", command);
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
   
    
    // ------------------------------ command values / results ------------------------------
    public class TestCommand {
        public          string  text;

        public override string  ToString() => text;
    }
}

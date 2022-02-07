// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Mapper;

using Req = Friflo.Json.Fliox.Mapper.Fri.RequiredAttribute;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable InconsistentNaming
// ReSharper disable NotAccessedField.Global
// ReSharper disable UnassignedReadonlyField
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    public class PocStore : FlioxClient
    {
        // --- containers
        public readonly EntitySet <string, Order>       orders;
        public readonly EntitySet <string, Customer>    customers;
        public readonly EntitySet <string, Article>     articles;
        public readonly EntitySet <string, Producer>    producers;
        public readonly EntitySet <string, Employee>    employees;
        public readonly EntitySet <string, TestType>    types;
        
        public PocStore(FlioxHub hub): base (hub) { }
        
        // --- commands
        [Fri.Command(Name =        "TestCommand")]
        public CommandTask<bool>    Test (TestCommand command)  => SendCommand<TestCommand, bool>("TestCommand", command);
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
        [Req]   public  Ref<string, Article>    article;
                public  int                     amount;
                public  string                  name;
                        
        public  override string                 ToString() => JsonSerializer.Serialize(this);
    }

    public class Article
    {
        [Req]   public  string                  id { get; set; }
        [Req]   public  string                  name;
                public  Ref<string, Producer>   producer;
                        
        public override string                  ToString() => JsonSerializer.Serialize(this);
    }

    public class Customer {
        [Req]   public  string                  id { get; set; }
        [Req]   public  string                  name;
        
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
    
    // test case: using abstract class containing the id 
    public abstract class PocEntity
    {
        [Req]   public   string                 id { get; set; } // defining as property ensures "id" is first JSON member

        public override string                  ToString() => JsonSerializer.Serialize(this);
    }
    
    public class TestType : PocEntity {
                public  DateTime        dateTime;
                public  DateTime?       dateTimeNull;
                public  BigInteger      bigInt;
                public  BigInteger?     bigIntNull;
        
                public  bool            boolean;
                public  bool?           booleanNull;
        
                public  byte            uint8;
                public  byte?           uint8Null;
                
                public  short           int16;
                public  short?          int16Null;
                
                public  int             int32;
                public  int?            int32Null;
                
                public  long            int64;
                public  long?           int64Null;
                
                public  float           float32;
                public  float?          float32Null;
                
                public  double          float64;
                public  double?         float64Null;
        
                public  PocStruct       pocStruct;
                public  PocStruct?      pocStructNull;

        [Req]   public  List<int>       intArray = new List<int>();
                public  List<int>       intArrayNull;
                public  List<int?>      intNullArray;
        
                public  JsonValue       jsonValue;
        
        [Req]   public  DerivedClass    derivedClass;
                public  DerivedClass    derivedClassNull;
    }
    
    public struct PocStruct {
        public  int                     value;
    }
    
    public class DerivedClass : OrderItem {
        public  int                     derivedVal;
    }
    
    // ------------------------------ command params / results ------------------------------
    public class TestCommand {
        public          string  text;

        public override string  ToString() => text;
    }
}

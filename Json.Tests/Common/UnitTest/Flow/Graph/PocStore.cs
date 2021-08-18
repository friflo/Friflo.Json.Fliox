// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph
{
    public class PocStore : EntityStore
    {
        public readonly EntitySet<Order>      orders;
        public readonly EntitySet<Customer>   customers;
        public readonly EntitySet<Article>    articles;
        public readonly EntitySet<Producer>   producers;
        public readonly EntitySet<Employee>   employees;
        public readonly EntitySet<TestType>   types;
        
        public PocStore(EntityDatabase database, string clientId) : base (database, TestGlobals.typeStore, clientId) {
            orders      = new EntitySet<Order>       (this);
            customers   = new EntitySet<Customer>    (this);
            articles    = new EntitySet<Article>     (this);
            producers   = new EntitySet<Producer>    (this);
            employees   = new EntitySet<Employee>    (this);
            types       = new EntitySet<TestType>    (this);
        }
    }
    
    // ------------------------------ models ------------------------------
    public class Order : Entity {
        public  Ref<Customer>       customer;
        public  DateTime            created;
        public  List<OrderItem>     items = new List<OrderItem>();
    }

    public class OrderItem {
        [Fri.Required]  public  Ref<Article>    article;
                        public  int             amount;
                        public  string          name;
    }

    public class Article : Entity
    {
        [Fri.Required]  public  string          name;
                        public  Ref<Producer>   producer;
    }

    public class Customer : Entity {
        [Fri.Required]  public  string          name;
    }
    
    public class Producer : Entity {
        [Fri.Required]  public  string              name;
        [Fri.Property (Name =                       "employees")]
                        public  List<Ref<Employee>> employeeList;
    }
    
    public class Employee : Entity {
        [Fri.Required]  public  string  firstName;
                        public  string  lastName;
    }
    
    public class TestType : Entity {
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

        [Fri.Required]  public  List<int>       intArray = new List<int>();
                        public  List<int>       intArrayNull;
        
                        public  JsonValue       jsonValue;
        
        [Fri.Required]  public  DerivedClass    derivedClass;
                        public  DerivedClass    derivedClassNull;
    }
    
    public struct PocStruct {
        public  int                 value;
    }
    
    public class DerivedClass : OrderItem {
        public int derivedVal;
    }

    // ------------------------------ messages ------------------------------
    class TestMessage {
        public          string  text;

        public override string  ToString() => text;
    }
}

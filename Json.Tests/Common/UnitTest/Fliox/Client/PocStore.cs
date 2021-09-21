// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Friflo.Json.Fliox.DB.Client;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable UnassignedReadonlyField
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    public class PocStore : EntityStore
    {
        public readonly EntitySet <string, Order>       orders;
        public readonly EntitySet <string, Customer>    customers;
        public readonly EntitySet <string, Article>     articles;
        public readonly EntitySet <string, Producer>    producers;
        public readonly EntitySet <string, Employee>    employees;
        public readonly EntitySet <string, TestType>    types;
        
        public PocStore(EntityDatabase database, TypeStore typeStore, string clientId) : base (database, typeStore,             clientId) {}
        /// <summary>constructor used for tests to reuse global <see cref="TestGlobals.typeStore"/> to enhance performance.</summary> 
        public PocStore(EntityDatabase database, string clientId) :                      this (database, TestGlobals.typeStore, clientId) {}
    }
    
    // ------------------------------ models ------------------------------
    public abstract class PocEntity
    {
        [Fri.Required]  public  string  id { get; set; } // defining as property ensures "id" is first JSON member

        public override     string  ToString() => JsonDebug.ToJson(this, false);
    }
    
    public class Order : PocEntity {
        public  Ref<string, Customer>   customer;
        public  DateTime                created;
        public  List<OrderItem>         items = new List<OrderItem>();
    }

    public class OrderItem {
        [Fri.Required]  public  Ref<string, Article>    article;
                        public  int                     amount;
                        public  string                  name;
    }

    public class Article : PocEntity
    {
        [Fri.Required]  public  string                  name;
                        public  Ref<string, Producer>   producer;
    }

    public class Customer : PocEntity {
        [Fri.Required]  public  string                  name;
    }
    
    public class Producer : PocEntity {
        [Fri.Required]  public  string                      name;
        [Fri.Property (Name =                              "employees")]
                        public  List<Ref<string, Employee>> employeeList;
    }
    
    public class Employee : PocEntity {
        [Fri.Required]  public  string  firstName;
                        public  string  lastName;
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

        [Fri.Required]  public  List<int>       intArray = new List<int>();
                        public  List<int>       intArrayNull;
                        public  List<int?>      intNullArray;
        
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

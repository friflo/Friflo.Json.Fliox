// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Friflo.Json.Fliox.Database;
using Friflo.Json.Fliox.Graph;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Graph
{
    public class PocStore : EntityStore
    {
        public readonly EntitySet <string, Order>       orders;
        public readonly EntitySet <string, Customer>    customers;
        public readonly EntitySet <string, Article>     articles;
        public readonly EntitySet <string, Producer>    producers;
        public readonly EntitySet <string, Employee>    employees;
        public readonly EntitySet <string, TestType>    types;
        
        public PocStore(EntityDatabase database, TypeStore typeStore, string clientId) : base (database, typeStore, clientId) {
            orders      = new EntitySet <string, Order>       (this);
            customers   = new EntitySet <string, Customer>    (this);
            articles    = new EntitySet <string, Article>     (this);
            producers   = new EntitySet <string, Producer>    (this);
            employees   = new EntitySet <string, Employee>    (this);
            types       = new EntitySet <string, TestType>    (this);
        }
        
        public PocStore(EntityDatabase database, string clientId) : this (database, TestGlobals.typeStore, clientId) { }
    }
    
    // ------------------------------ models ------------------------------
    /// <summary>
    /// <see cref="PocEntity"/> can be used as a base class for entity model classes.<br/>
    /// Doing this is optional. If its used it provides the features listed below.
    /// <list type="bullet">
    ///   <item>Enable instant listing of all declared entity models by using IDE tools like: "Find usage" or "Find All References"</item>
    ///   <item>Ensures an entity key (id) is not changed when already assigned by a runtime assertion.</item>
    /// </list>  
    /// </summary>
    public abstract class PocEntity
    {
        // ReSharper disable once InconsistentNaming
        [Fri.Required]
        public  string  id {
            get => _id;
            set {
                if (_id == value)
                    return;
                if (_id == null) {
                    _id = value;
                    return;
                }
                throw new ArgumentException($"Entity id must not be changed. Type: {GetType().Name}, was: '{_id}', assigned: '{value}'");
            }
        }
        
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string  _id;

        public override     string  ToString() => JsonDebug.ToJson(this, false);
    }
    
    public class Order : PocEntity {
        public  Ref<string, Customer>       customer;
        public  DateTime            created;
        public  List<OrderItem>     items = new List<OrderItem>();
    }

    public class OrderItem {
        [Fri.Required]  public  Ref<string, Article>    article;
                        public  int             amount;
                        public  string          name;
    }

    public class Article : PocEntity
    {
        [Fri.Required]  public  string          name;
                        public  Ref<string, Producer>   producer;
    }

    public class Customer : PocEntity {
        [Fri.Required]  public  string          name;
    }
    
    public class Producer : PocEntity {
        [Fri.Required]  public  string              name;
        [Fri.Property (Name =                       "employees")]
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

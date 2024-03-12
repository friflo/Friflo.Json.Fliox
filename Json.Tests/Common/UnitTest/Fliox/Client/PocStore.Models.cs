// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using Friflo.Json.Fliox;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    // ---------------------------------- entity models ----------------------------------
    /// <summary>
    /// Some useful class documentation :)
    /// <code>
    ///     multiline line
    ///     code documentation
    /// </code>
    /// Test type reference '<see cref="OrderItem"/>' </summary>
    public class Order {
        [Key]       public  string          id { get; set; }
                    /// <summary>
                    /// Some <b>useful</b> field documentation 🙂
                    /// Check some new lines
                    /// in documentation
                    /// </summary>
        [Relation(nameof(PocStore.customers))]
                    public  string          customer;
                    /// <summary>single line documentation</summary>
                    public  DateTime        created;
                    /// <summary><code>single line code documentation</code></summary>
                    public  List<OrderItem> items = new List<OrderItem>();
                        
        public override     string          ToString() => JsonSerializer.Serialize(this);
    }

    public class OrderItem {
        [Relation(nameof(PocStore.articles))]
        [Required]  public  string          article;
                    public  int             amount;
                    public  string          name;
                        
        public  override    string          ToString() => JsonSerializer.Serialize(this);
    }

    public class Article
    {
        [Key]       public  string          id { get; set; }
        [Required]  public  string          name;
        [Relation(nameof(PocStore.producers))]
                    public  string          producer;

                        
        public override     string          ToString() => JsonSerializer.Serialize(this);
    }

    public class Customer {
        [Key]       public  string          id { get; set; }
        [Required]  public  string          name;
        
        public override     string          ToString() => JsonSerializer.Serialize(this);
    }
    
    public class Producer {
        [Key]       public  string          id { get; set; }
        [Required]  public  string          name;
        [Relation(nameof(PocStore.employees))]
        [Serialize                        ("employees")]
                    public  List<string>    employeeList;
                        
        public override     string          ToString() => JsonSerializer.Serialize(this);
    }
    
    public class Employee {
        [Key]       public  string          id { get; set; }
        [Required]  public  string          firstName;
                    public  string          lastName;
                        
        public override     string          ToString() => JsonSerializer.Serialize(this);
    }
    
    // test case: using abstract class containing the id 
    public abstract class PocEntity
    {
        [Key]       public  string          id { get; set; } // defining as property ensures "id" is first JSON member

        public override     string          ToString() => JsonSerializer.Serialize(this);
    }
    
    public enum TestEnum
    {
        NONE    = 0,
        e1      = 10,
        e2      = 11
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

        [Required]  public  List<int>       intArray = new List<int>();
                    public  List<int>       intArrayNull;
                    public  List<int?>      intNullArray;
        
                    public  JsonValue       jsonValue;
        
        [Required]  public  DerivedClass    derivedClass;
                    public  DerivedClass    derivedClassNull;
                    
                    public  TestEnum        testEnum;
                    public  TestEnum?       testEnumNull;
    }
    
    public class NonClsType {
        [Key]       public  string          id { get; set; }
        
                    public  sbyte           int8;
                    public  ushort          uint16;
                    public  uint            uint32;
                    public  ulong           uint64;
                    
                    public  sbyte?          int8Null;
                    public  ushort?         uint16Null;
                    public  uint?           uint32Null;
                    public  ulong?          uint64Null;
    }
    
    public struct PocStruct {
        public  int                     value;
    }
    
    public class DerivedClass : OrderItem {
        public  int                     derivedVal;
    }
    
    public class TestKeyName {
        [Key]       public  string          testId;
                    public  string          value;
    }
    
    // ---------------------------- command models - aka DTO's ---------------------------
    public class TestCommand {
        public          string  text;

        public override string  ToString() => text;
    }
}
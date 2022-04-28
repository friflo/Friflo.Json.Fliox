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
    /// <summary>
    /// The <see cref="PocStore"/> offer two functionalities: <br/>
    /// 1. Defines a database <b>schema</b> by declaring its containers, commands and messages<br/>
    /// 2. Is a database <b>client</b> providing type-safe access to its containers, commands and messages <br/>
    /// </summary>
    /// <remarks>
    /// Its containers are fields or properties of type <see cref="EntitySet{TKey,T}"/>. <br/>
    /// Its commands are methods returning a <see cref="CommandTask{TResult}"/>.<br/>
    /// Its messages are methods returning a <see cref="MessageTask"/>.
    /// <see cref="PocStore"/> instances can be used on server and client side.
    /// </remarks>
    [Fri.OpenAPI(Version = "1.0.0",     TermsOfService  = "https://github.com/friflo/Friflo.Json.Fliox",
        ContactName = "Ullrich Praetz", ContactUrl      = "https://github.com/friflo/Friflo.Json.Fliox/issues",
        LicenseName = "AGPL-3.0",       LicenseUrl      = "https://spdx.org/licenses/AGPL-3.0-only.html")]
    [Fri.OpenAPIServer(Description = "test localhost",  Url = "http://localhost:8010/fliox/rest/main_db")]
    [Fri.OpenAPIServer(Description = "test 127.0.0.1",  Url = "http://127.0.0.1:8010/fliox/rest/main_db")]
    public class PocStore : FlioxClient
    {
        // --- containers
        public readonly EntitySet <string, Order>       orders;
        public readonly EntitySet <string, Customer>    customers;
        public readonly EntitySet <string, Article>     articles;
        public readonly EntitySet <string, Producer>    producers;
        public readonly EntitySet <string, Employee>    employees;
        public readonly EntitySet <string, TestType>    types;
        
        
        public PocStore(FlioxHub hub): base (hub) {
            test = new TestCommands(this);
        }
        
        public readonly TestCommands    test;
        
        // --- commands
        [Fri.Command(Name =            "TestCommand")]
        public CommandTask<bool>        Test (TestCommand param)    => SendCommand<TestCommand, bool>("TestCommand",param);
        public CommandTask<string>      SyncCommand (string param)  => SendCommand<string, string>  ("SyncCommand", param);
        public CommandTask<string>      AsyncCommand (string param) => SendCommand<string, string>  ("AsyncCommand",param);
        public CommandTask<string>      Command1 ()                 => SendCommand<string>          ("Command1");
        public CommandTask<int>         CommandInt (int param)      => SendCommand<int>             ("CommandInt");     // test required param (value type)
        
        // --- messages
        public MessageTask              Message1    (string param)  => SendMessage  ("Message1",        param);
        public MessageTask              AsyncMessage(string param)  => SendMessage  ("AsyncMessage",    param);
    }

    // ------------------------------ models ------------------------------
    /// <summary>
    /// Some useful class documentation :)
    /// <code>
    ///     multiline line
    ///     code documentation
    /// </code>
    /// Test type reference '<see cref="OrderItem"/>' </summary>
    public class Order {
        [Req]   public  string                  id { get; set; }
                /// <summary>
                /// Some <b>useful</b> field documentation 🙂
                /// Check some new lines
                /// in documentation
                /// </summary>
                public  Ref<string, Customer>   customer;
                /// <summary>single line documentation</summary>
                public  DateTime                created;
                /// <summary><code>single line code documentation</code></summary>
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
    public class TestCommands : HubMessages
    {
        public TestCommands(FlioxClient client) : base(client) { }
        
        // --- commands
        public CommandTask<string>  Command2 ()                 => SendCommand<string>          ("test.Command2");
        public CommandTask<string>  CommandHello (string param) => SendCommand<string, string>  ("test.CommandHello", param);
        
        public CommandTask<int>     CommandExecutionError ()    => SendCommand<int>             ("test.CommandExecutionError");   // test returning an error

        
        // --- messages
        public MessageTask          Message2 (string param)     => SendMessage                  ("test.Message2",  param);
    }
    
    public class TestCommand {
        public          string  text;

        public override string  ToString() => text;
    }
}

// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph
{
    // ------------------------------ models ------------------------------
    public class Order : Entity {
        public  Ref<Customer>       customer;
        public  DateTime            created; 
        public  List<OrderItem>     items = new List<OrderItem>();
    }

    public class OrderItem {
        public  Ref<Article>        article;
        public  int                 amount;
        public  string              name;
    }

    public class Article : Entity
    {
        public  string              name;
        public  Ref<Producer>       producer;
    }

    public class Customer : Entity {
        public  string              name;
    }
    
    public class Producer : Entity {
        public  string              name;
        [Fri.Property(Name = "employees")]
        public  List<Ref<Employee>> employeeList;
    }
    
    public class Employee : Entity {
        public  string              firstName;
        public  string              lastName;
    }

    // --- store containers
    public class PocStore : EntityStore
    {
        public readonly EntitySet<Order>      orders;
        public readonly EntitySet<Customer>   customers;
        public readonly EntitySet<Article>    articles;
        public readonly EntitySet<Producer>   producers;
        public readonly EntitySet<Employee>   employees;
        
        public PocStore(EntityDatabase database, string clientId) : base (database, TestGlobals.typeStore, clientId) {
            orders      = new EntitySet<Order>       (this);
            customers   = new EntitySet<Customer>    (this);
            articles    = new EntitySet<Article>     (this);
            producers   = new EntitySet<Producer>    (this);
            employees   = new EntitySet<Employee>    (this);
        }
    }
    
    // ------------------------------ messages ------------------------------
    class TestMessage {
        public          string  text;

        public override string  ToString() => text;
    }
}

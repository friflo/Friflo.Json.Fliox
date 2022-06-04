using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox;

// ReSharper disable All
namespace Fliox.DemoHub
{
    // Note: Main property of all model classes
    // They are all POCO's aka Plain Old Class Objects. See https://en.wikipedia.org/wiki/Plain_old_CLR_object
    // As a result integration of these classes in other modules or libraries is comparatively easy.

    // ---------------------------------- entity models ----------------------------------
    public class Article {
        [Required]  public  long            id { get; set; }
        [Required]  public  string          name;
        [Relation(nameof(DemoStore.producers))]
                    public  long            producer;
                    public  DateTime?       created;
    }

    public class Customer {
        [Required]  public  long            id { get; set; }
        [Required]  public  string          name;
                    public  DateTime?       created;
    }
    
    public class Employee {
        [Required]  public  long            id { get; set; }
        [Required]  public  string          firstName;
                    public  string          lastName;
                    public  DateTime?       created;
    }

    public class Order {
        [Required]  public  long            id { get; set; }
        [Relation(nameof(DemoStore.customers))]
                    public  long            customer;
                    public  DateTime        created;
                    public  List<OrderItem> items = new List<OrderItem>();
    }

    public class OrderItem {
        [Relation(nameof(DemoStore.articles))]
        [Required]  public  long            article;
                    public  int             amount;
                    public  string          name;
    }

    public class Producer {
        [Required]  public  long            id { get; set; }
        [Required]  public  string          name;
        [Relation(nameof(DemoStore.employees))]
                    public  List<long>      employees;
                    public  DateTime?       created;
    }
    
    
    // ---------------------------- command models - aka DTO's ---------------------------
    public class Operands {
        public  double      left;
        public  double      right;
    }
    
    public class Fake {
        /// <summary>if false generated entities are nor added to the <see cref="Records"/> result</summary>
        public  bool?       addResults;
        public  int?        articles;
        public  int?        customers;
        public  int?        employees;
        public  int?        orders;
        public  int?        producers;
    }
    
    public class Counts {
        public  int         articles;
        public  int         customers;
        public  int         employees;
        public  int         orders;
        public  int         producers;
    }
    
    public class Records {
        /// <summary>contains a filter that can be used to filter the generated entities in a container</summary>
        public  string      info;
        /// <summary>number of entities generated in each container</summary>
        public  Counts      counts;
        public  Article[]   articles;
        public  Customer[]  customers;
        public  Employee[]  employees;
        public  Order[]     orders;
        public  Producer[]  producers;
    }
}
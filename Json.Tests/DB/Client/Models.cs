using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox;

// ReSharper disable All
namespace Friflo.Json.Tests.DB.Client
{
    // Note: Main property of all model classes
    // They are all POCO's aka Plain Old Class Objects. See https://en.wikipedia.org/wiki/Plain_old_CLR_object
    // As a result integration of these classes in other modules or libraries is comparatively easy.

    // ---------------------------------- entity models ----------------------------------
    public class Article {
        [Key]       public  string          id { get; set; }
        ///<summary> Descriptive article name - may use Unicodes like 👕 🍏 🍓 </summary>
        [Required]  public  string          name;
        [Relation(nameof(TestClient.producers))]
                    public  string          producer;
                    public  DateTime?       created;
    }

    public class Customer {
        [Key]       public  string          id { get; set; }
        [Required]  public  string          name;
                    public  DateTime?       created;
    }
    
    public class Employee {
        [Key]       public  string          id { get; set; }
        [Required]  public  string          firstName;
                    public  string          lastName;
                    public  DateTime?       created;
    }

    public class Order {
        [Key]       public  string          id { get; set; }
        [Relation(nameof(TestClient.customers))]
                    public  string          customer;
                    public  DateTime        created;
                    public  List<OrderItem> items = new List<OrderItem>();
    }

    public class OrderItem {
        [Relation(nameof(TestClient.articles))]
        [Required]  public  string          article;
                    public  int             amount;
                    public  string          name;
    }

    public class Producer {
        [Key]       public  string          id { get; set; }
        [Required]  public  string          name;
        [Relation(nameof(TestClient.employees))]
                    public  List<string>    employees;
                    public  DateTime?       created;
    }
    
    public enum TestEnum {
        e1 = 101,
        e2 = 102
    }
    
    public class TestEnumEntity {
        [Key]       public  string          id { get; set; }
                    public  TestEnum        enumVal;
                    public  TestEnum?       enumValNull;
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox;

// ReSharper disable All
namespace Lab
{
    // ---------------------------------- entity models ----------------------------------
    public class Article {
        [Key]       public  long            id { get; set; }
        ///<summary> Descriptive article name - may use Unicodes like 👕 🍏 🍓 </summary>
        [Required]  public  string          name;
                    public  long            producer;
                    public  DateTime?       created;
    }

}
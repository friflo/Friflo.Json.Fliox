using System;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;

// ReSharper disable All
namespace Fliox.TodoHub
{
    /// <summary>
    /// The <see cref="TodoStore"/> offer two functionalities: <br/>
    /// 1. Defines a database <b>schema</b> by declaring its containers, commands and messages<br/>
    /// 2. Is a database <b>client</b> providing type-safe access to its containers, commands and messages <br/>
    /// </summary>
    public class TodoStore : FlioxClient {
        // --- containers
        public readonly EntitySet <long, Todo>        todos;

        public TodoStore(FlioxHub hub, string dbName) : base (hub, dbName) { }
    }
    
    // ---------------------------------- entity models ----------------------------------
    public class Todo {
        [Key]       public  long        id { get; set; }
        [Required]  public  string      title;
                    public  bool?       completed;
                    public  DateTime?   created;
                    public  string      description;
    }
}

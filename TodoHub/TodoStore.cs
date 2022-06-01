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
    /// <br/>
    /// </summary>
    /// <remarks>
    /// <see cref="TodoStore"/> containers are fields or properties of type <see cref="EntitySet{TKey,T}"/>. <br/>
    /// <see cref="TodoStore"/> instances can be used on client and server side. <br/>
    /// </remarks>
    public class TodoStore : FlioxClient {
        // --- containers
        public readonly EntitySet <long, Todo>        todos;

        public TodoStore(FlioxHub hub) : base (hub) { }
    }
    
    // ------------------------------ entity models ------------------------------
    public class Todo {
        [Required]  public  long                id { get; set; }
        [Required]  public  string              title;
                    public  string              description;
                    public  bool?               completed;
                    public  DateTime?           created;
    }
}

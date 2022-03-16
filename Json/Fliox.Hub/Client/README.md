

# ![logo](../../../docs/images/Json-Fliox.svg)     **Fliox Hub Client**      ![SPLASH](../../../docs/images/paint-splatter.svg)

[![.NET Tests](https://github.com/friflo/Friflo.Json.Fliox/workflows/.NET/badge.svg)](https://github.com/friflo/Friflo.Json.Fliox/actions)


## `FlioxClient`
namespace **`Friflo.Json.Fliox.Hub.Client`**

The intention of `FlioxClient` is extending it by a domain specific class.

``` csharp
    public class ShopStore : FlioxClient
    {
        // --- containers
        public readonly EntitySet <long, Article>     articles;
        
        public ShopStore(FlioxHub hub) : base(hub) { }
    }
    
    public class Article
    {
        public  long        id { get; set; }
        public  string      name;
    }
```

By doing this the `ShopStore` offer two main functionalities:
-   Define a **database schema** by declaring its containers, commands and messages
-   Instances of `ShopStore` are **database clients** providing
    type-safe access to the database containers, commands and messages  

In detail:
- **containers** - are fields or properties of type `EntitySet<TKey,T>`
- **commands**   - are methods returning a `CommandTask<TResult>`
- **messages**   - are methods returning a `MessageTask`

Instances of `ShopStore` can be used on server and client side.

To access a database using the `ShopStore` a `FlioxHub` is required.

``` csharp
    public static async Task AccessDatabase() {
        var database    = new MemoryDatabase(); // or other database like: file-system, SQLite, Postgres, ...
        var hub         = new FlioxHub(database);
        var store       = new ShopStore(hub);
        
        store.articles.Create(new Article() { id = 1, name = "Bread" });
        
        await store.SyncTasks();
    }
```

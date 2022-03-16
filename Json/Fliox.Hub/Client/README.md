

# ![logo](../../../docs/images/Json-Fliox.svg)     **Fliox Hub Client**      ![SPLASH](../../../docs/images/paint-splatter.svg)

[![.NET Tests](https://github.com/friflo/Friflo.Json.Fliox/workflows/.NET/badge.svg)](https://github.com/friflo/Friflo.Json.Fliox/actions)


## `FlioxClient`
namespace **`Friflo.Json.Fliox.Hub.Client`**

The intention of `FlioxClient` is extending it by a domain specific class. See example below.  
Instances of this class are acting as **clients** to access database **containers**
and execute database **commands**.

Additional to using class instances as clients it also defines a **database schema**.  
It can be assigned as a `DatabaseSchema` to an `EntityDatabase` instance for
- **JSON Validation** of entities / records written to a container
- **Code generation** of various programming languages.  
  Built-In supported languages are: **Typescript**, **C#**, **Kotlin**, **JSON Schema** and **HTML**.
- Support of generic database clients like the [**Hub Explorer**](../../../Json/Fliox.Hub.Explorer/)


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

Using this setup the `ShopStore` offer two main functionalities:
-   Define a **database schema** by declaring its containers, commands and messages
-   Instances of `ShopStore` are **clients** providing
    type-safe access to the database containers, commands and messages  

In detail:
- **containers** - are fields or properties of type `EntitySet<TKey,T>`
- **commands**   - are methods returning a `CommandTask<TResult>`
- **messages**   - are methods returning a `MessageTask`

## Containers
Containers are declared as fields or properties of `EntitySet<TKey,T>`.  
Methods of `EntitySet<TKey,T>` provide all **common operations** to access or mutate the
entities / records stored in a container / table. These operations are:
- **Create** entities
- **Read** entities by id - primary key
- **Upsert** entities
- **Delete** entities
- **Query** entities using a LINQ filter - optionally using a cursor to iterate large datasets
- Return entities **referenced by entities** returned in a Read or Query request.  
  This is the analog method to a **JOIN** in **SQL**.
- **Aggregate** / **Count** entities using a **LINQ filter**
- **Subscribe** to entity changes - **create**, **upsert**, **delete** & **patch** - made by other clients


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

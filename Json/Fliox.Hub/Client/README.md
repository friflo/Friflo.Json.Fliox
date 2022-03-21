

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
- Support of generic database clients like the [**Hub Explorer**](../../../Json/Fliox.Hub.Explorer/README.md)


``` csharp
public class ShopStore : FlioxClient
{
    // --- containers
    public readonly EntitySet <long, Article>     articles;

    // --- commands
    public CommandTask<string> Hello (string param) => SendCommand<string, string> ("Hello", param);
    
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

## container operations
Containers are declared as fields or properties of type `EntitySet<TKey,T>`.  
`EntitySet<TKey,T>` methods provide all **common operations** to access or mutate the
entities / records stored in a container / table.  
These container operations are:
- **Create** entities
- **Read** entities by id - primary key
- **Upsert** entities
- **Delete** entities
- **Query** entities using a LINQ filter - optionally using a cursor to iterate large datasets
- Return entities **referenced by entities** returned by a **Read** or **Query** task.  
  This is the analog method to a **JOIN** in **SQL**.
- **Aggregate** / **Count** entities using a **LINQ filter**
- **Subscribe** to entity changes - **create**, **upsert**, **delete** & **patch** - made by other clients


## database commands & messages

A client also offer the possibility to send commands and messages.  
Both accept **none** or a **single** parameter - commonly called `param`.  
The difference between the two is:
- a command return a **result** 
- a message return **void** aka nothing.


## Client usage

Instances of `ShopStore` can be used on server and client side.

To access a database using the `ShopStore` a `FlioxHub` is required.

``` csharp
public static async Task AccessDatabase() {
    var database    = new FileDatabase("shop", new MessageHandler());
    // or other database implementations like: MemoryDatabase, SQLite, Postgres, ...
    var hub         = new FlioxHub(database);
    var store       = new ShopStore(hub);
    
    var hello           = store.Hello("World");
    var createArticle   = store.articles.Upsert(new Article() { id = 1, name = "Bread" });
    var stats           = store.std.Stats(null);

    await store.SyncTasks();
    
    Console.WriteLine(hello.Result);
    // output:  hello World!
    Console.WriteLine($"createArticle.Success: {createArticle.Success}");
    // output:  createArticle.Success: True
    foreach (var container in stats.Result.containers) {
        Console.WriteLine($"{container.name}: {container.count}");
    }
    // output:  articles: 1
}
```

## Schema generation

As mentioned above `ShopStore` also defines a database schema.  
A database schema is the declaration of database **containers**, **commands** and **messages**.  

All declarations as expressed as types in a schema. This principle enables code generation as types
for other programming languages.

Schema generation is integral part of the [HTTP Hub](../Host/README.md#HTTPHostHub).  
So all generated files and their zip archives are available via urls.

Alternatively code can be generated with C# using `SchemaModel.GenerateSchemaModels()`

The following example generate the types for Typescript, C#, Kotlin, JSON Schema and HTML based on the
passed schema type `ShopStore`. The generated code is written to folder `./schema/`

``` csharp
public static void GenerateSchemaModels() {
    var schemaModels = SchemaModel.GenerateSchemaModels(typeof(ShopStore));
    foreach (var schemaModel in schemaModels) {
        var folder = $"./schema/{schemaModel.type}";
        schemaModel.WriteFiles(folder);
    }
}
```





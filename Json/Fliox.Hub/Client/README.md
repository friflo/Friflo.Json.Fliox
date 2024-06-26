

# [![JSON Fliox](../../../docs/images/Json-Fliox.svg)](https://github.com/friflo/Friflo.Json.Fliox)     **Hub Client** ![SPLASH](../../../docs/images/paint-splatter.svg)

[![.NET Tests](https://github.com/friflo/Friflo.Json.Fliox/workflows/.NET/badge.svg)](https://github.com/friflo/Friflo.Json.Fliox/actions)


## `FlioxClient`
namespace **`Friflo.Json.Fliox.Hub.Client`**

**Client** API reference at [**fliox-docs**](https://github.com/friflo/fliox-docs#client)

The intention of `FlioxClient` is extending it by a domain specific class. See example below.  
Instances of this class are acting as **clients** to access database **containers**
and execute database **commands**.

Additional to using class instances as clients it also defines a **database schema**.  
It can be assigned as a `DatabaseSchema` to an `EntityDatabase` instance for
- **JSON Validation** of entities / records written to a container
- **Code generation** of various programming languages.  
  Built-In supported languages are: **Typescript**, **C#**, **Kotlin**, **JSON Schema** / **OpenAPI**, **GraphQL** and **HTML**.
- Support of generic database clients like the [**Hub Explorer**](../../Fliox.Hub.Explorer/README.md)


``` csharp
public class ShopClient : FlioxClient
{
    // --- containers
    public readonly EntitySet <long, Article>     articles;
    
    // --- commands
    /// <summary>return 'hello ...!' with the given <paramref name="param"/></summary>
    public CommandTask<string>  Hello (string param)    => send.Command<string, string> (param);
    
    public ShopClient(FlioxHub hub) : base(hub) { }
}

public class Article
{
    public  long        id { get; set; }
    public  string      name;
}
```

Using this setup the `ShopClient` offer two main functionalities:
-   Define a **database schema** by declaring its containers, commands and messages
-   Instances of `ShopClient` are **clients** providing
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
- **Patch** entities - update only specific entity fields. See [RFC 7386: JSON Merge Patch](https://www.rfc-editor.org/rfc/rfc7386).
- **Query** entities using a LINQ filter - optionally using a cursor to iterate large datasets. See [Query filter](#query-filter).
- **Read relations** - read entities referenced by entities returned by a **Read** or **Query** task.  
  This is the analog method to a **JOIN** in **SQL**.
- **Aggregate** / **Count** entities using a **LINQ filter**
- **Subscribe** to entity changes - **create**, **upsert**, **delete** & **patch** - made by other clients


## database commands & messages

A client also offer the possibility to send and subscribe commands and messages.  
In detail:

- **Send** a message / command by passing its name and an optional parameter - commonly called `param`.  
  The `param` can be any JSON type like: `string`, `number`, `boolean`, `object` or `array`.  
  The difference between command and message is:
  - a **command** return a **result** - a command is primarily used to execute a domain specific operation on the Hub.  
    Therefore a command requires a **message handler** in the `IServiceCommands` assigned to a database.
  - a **message** return **void**     - messages are used to send notifications to the Hub and to other clients connected to the Hub.  
    Adding a **message handler** for a message in the `IServiceCommands` is optional.

- **Subscribe** messages / commands send to a Hub by passing their name and a handler method or lambda.  
  - The Hub forward message / command events **only** to clients which have subscribed.  
    *In other words:* A Hub don't forward message / command events to a client unsolicited.  
  - This approach enables subscribing messages / events send from other clients **without** changing / deploying the Hub.  
    The client user need to be **authorized** to subscribe specific message & command events.


## Client usage

Instances of `ShopClient` can be used on server and client side.

Example of a `IServiceCommands` implementation

``` csharp
public class ShopCommands : IServiceCommands
{
    [CommandHandler]
    private static Result<string> Hello(Param<string> param, MessageContext context) {
        if (!param.GetValidate(out string value, out string error)) {
            return Result.ValidationError(error);
        }
        return $"hello {value}!";
    } 
}
```

To access a database using the `ShopClient` a `FlioxHub` is required.
``` csharp
// -------------------------------- run example as unit tests --------------------------------
public static class TestShopStore
{
    /// <summary>
    /// Execute service commands: Hello and std.Stats <br/>
    /// Execute database command: Upsert
    /// </summary>
    [Test]
    public static async Task AccessDatabase() {
        var database    = new FileDatabase("shop_db", "./shop_db").AddCommands(new ShopCommands());
        // or create other databases like: MemoryDatabase, SQLiteDatabase, PostgreSQLDatabase, ...
        var hub         = new FlioxHub(database);
        var client      = new ShopClient(hub);
        
        var hello           = client.Hello("World");
        var createArticle   = client.articles.Upsert(new Article() { id = 1, name = "Bread" });
        var stats           = client.std.Stats(null);

        await client.SyncTasks();
        
        Console.WriteLine(hello.Result);
        // output:  hello World!
        Console.WriteLine($"createArticle.Success: {createArticle.Success}");
        // output:  createArticle.Success: True
        foreach (var container in stats.Result.containers) {
            Console.WriteLine($"{container.name}: {container.count}");
        }
        // output:  articles: 1
    }
}
```

## Query filter

Query filters are lambda expressions used to extract only the entities from a container that fullfil the specified filter condition.  
They are specified as [**LINQ**](https://en.wikipedia.org/wiki/Language_Integrated_Query) expressions
and used as the counterpart of the **WHERE** clause in **SQL** statements.  
A filter expression is a lambda function which is evaluated for each **`entity`** of a container and return a **`boolean`** result.  
If **`true`** the `entity` is added to the query result set.

General syntax.
```csharp
entity => condition
```

In contrast to **SQL** statements **LINQ** has compiler support and enable code navigation, searching and refactoring in C#.

**C#** example query filter for the `ShopClient` client above
```csharp
store.articles.Query(o => o.name == "Bread")
```


The **same** filter expression can also be used to filter entities of the selected container in the [Hub Explorer](../../Fliox.Hub.Explorer/README.md).
```typescript
o => o.name == "Bread"
```
[![Query filter](../../../docs/images/query-filter.png)](http://ec2-18-215-176-108.compute-1.amazonaws.com)


The syntax of lambda expressions / LINQ filters is an [infix notation](https://en.wikipedia.org/wiki/Infix_notation). Its intention is to be compact and easy to read by humans.  
When using a filter for a container query it is converted into an expression tree. Each node in the tree is an operation.

### Supported **LINQ** query filter operators and methods
| type          | operator / method              |                                                                                                     |
| -----------   | ------------------------------ | --------------------------------------------------------------------------------------------------- |
| **compare**   | ==                             | equals                                                                                              |
|               | !=                             | not equals                                                                                          |
|               | <                              | less than                                                                                           |
|               | <=                             | less than or equals                                                                                 |
|               | >                              | greater than                                                                                        |
|               | >=                             | greater than or equals                                                                              |
| **logical**   | &&                             | and                                                                                                 |
|               | &#124;&#124;                   | or                                                                                                  |
|               | !                              | not                                                                                                 |
| **string**    | value.StartsWith(string)       | determine if value starts with the given string.  Equivalent to LIKE to 'string%'                   |
|               | value.EndsWith(string)         | determine if value ends with the given string.    Equivalent to LIKE to '%string'                   |
|               | value.Contains(string)         | determine if value contains the given string.     Equivalent to LIKE to '%string%'                  |
|               | value.Length()                 | determine if value contains the given string.     Equivalent to SQL LEN()                           |
| **arithmetic**| +                              | add                                                                                                 |
|               | -                              | subtract                                                                                            |
|               | *                              | multiply                                                                                            |
|               | /                              | divide                                                                                              |
|               | %                              | modulo                                                                                              |
|               | Abs(number)                    | absolute value of the specified number                                                              |
|               | Ceiling(number)                | smallest integral value greater or equal to the specified number                                    |
|               | Floor(number)                  | largest integral value less or equal to the specified number                                        |
|               | Exp(number)                    | e raised to the specified power                                                                     |
|               | Log(number)                    | logarithm of a specified number                                                                     |
|               | Sqrt(number)                   | square root of a specified number                                                                   |
| **constants** | PI                             | ratio of the circumference of a circle to its diameter, specified by the constant, π                |
|               | E                              | natural logarithmic base, specified by the constant, e                                              |
|               | Tau                            | number of radians in one turn, specified by the constant, τ                                         |
| **aggregate** | items.Min(i =>     i.Property) | minimum value of a collection                                                                       |
|               | items.Max(i =>     i.Property) | maximum value of a collection                                                                       |
|               | items.Sum(i =>     i.Property) | sum of a values within a collection                                                                 |
|               | items.Average(i => i.Property) | average of values within a collection                                                               |
|               | items.Count()                  | count of elements within a collection                                                               |
|               | items.Count(i => condition)    | count of elements satisfying a condition within a collection                                        |
| **quantify**  | items.All(i => condition)      | determines whether all elements of a collection satisfy a condition or if the collection is *empty* |
|               | items.Any(i => condition )     | determines whether any element of a collection satisfy a condition                                  |

[Supported operations diagram](../../../Json.Tests/assets~/Schema/Markdown/Filter/class-diagram.md)



## Schema generation

As mentioned above `ShopClient` also defines a database schema.  
A database schema is the declaration of database **containers**, **commands** and **messages**.  

All declarations are expressed as types in a schema. This principle enables code generation as types
for other programming languages.

Schema generation is integral part of the [HTTP Hub](../Host/README.md#httphost).  
So all generated files and their zip archives are available via urls.

Alternatively code can be generated with C# using `SchemaModel.GenerateSchemaModels()`

The following example generate the types for Typescript, C#, Kotlin, JSON Schema / OpenAPI, GraphQL and HTML based on the
passed schema type `ShopClient`. The generated code is written to folder `./schema/`

``` csharp
public static class TestSchemaGeneration
{
    /// <summary>
    /// Generate schema model files (HTML, JSON Schema / OpenAPI, Typescript, C#, Kotlin) for <see cref="ShopClient"/>
    /// in the working directory.
    /// </summary>
    [Test]
    public static void GenerateSchemaModels() {
        var schemaModels = SchemaModel.GenerateSchemaModels(typeof(ShopClient));
        foreach (var schemaModel in schemaModels) {
            var folder = $"./schema/{schemaModel.type}";
            schemaModel.WriteFiles(folder);
        }
    }
}
```





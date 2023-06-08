

# [![JSON Fliox](../../../docs/images/Json-Fliox.svg)](https://github.com/friflo/Friflo.Json.Fliox)     **Host** ![SPLASH](../../../docs/images/paint-splatter.svg)

[![.NET Tests](https://github.com/friflo/Friflo.Json.Fliox/workflows/.NET/badge.svg)](https://github.com/friflo/Friflo.Json.Fliox/actions)


## General
There are two general ways to access a database using a [`FlioxClient`](../../Fliox.Hub/Client/README.md)

- **direct** access a database by using a specific `EntityDatabase` implementation like
  `FileDatabase`, `MemoryDatabase` or other implementations using a `FlioxHub`.  
  This approach is very similar to [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
  or [Dapper](https://github.com/DapperLib/Dapper)

- **remote** access a database via [`HttpHost`](../Remote/README.md#httphost) which by itself access a specific `EntityDatabase` directly.  
  Remote access support **HTTP** and **WebSockets**.

``` 
    direct:    FlioxClient                ->  FlioxHub(database)
    remote:    FlioxClient  ->  HttpHost  ->  FlioxHub(database)
                           HTTP
```



# `FlioxHub`
namespace **`Friflo.Json.Fliox.Hub.Host`**

**Host** API reference at [**fliox-docs**](https://github.com/friflo/fliox-docs#host)

A `FlioxHub` instance is the single entry point used to handle **all** requests send by a client.  
E.g. direct/remote via a [`FlioxClient`](../../Fliox.Hub/Client/README.md) or remote-only via an HTTP client - typically a web browser.  
When instantiating a `FlioxHub` an `EntityDatabase` need to be assigned used to execute all
**database operations**, **commands** and **messages** send by a client targeting this database.

Domain specific command / message handler methods can be added to an `EntityDatabase` by creating a class
implementing `IServiceCommands`.  
The handler methods need to be attributed with `[CommandHandler]` or `[MessageHandler]`.  
An instance of this class needs to be added to an `EntityDatabase` using `EntityDatabase.AddCommands().`

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

public static FlioxHub CreateHub() {
    var database = new MemoryDatabase("shop_db").AddCommands(new ShopCommands());
    // or use other databases like: FileDatabase, SQLiteDatabase, PostgreSQLDatabase, ...
    return new FlioxHub(database);
}
```






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

Domain specific commands and messages can be added to an `EntityDatabase` by creating a class
that **extends** `DatabaseService`. By convention this class should be named `<application name>Service`. E.g. `DemoService`

Domain specific commands and messages can be added to the `DemoService` as methods.
An instance of the `DemoService` need to be passed when instantiating an `EntityDatabase`.

The ownership of this setup looks like this:

```
    FlioxHub -> EntityDatabase -> DatabaseService
```




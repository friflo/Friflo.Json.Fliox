

# ![logo](../../../docs/images/Json-Fliox.svg)     **Fliox Hub Host**      ![SPLASH](../../../docs/images/paint-splatter.svg)

[![.NET Tests](https://github.com/friflo/Friflo.Json.Fliox/workflows/.NET/badge.svg)](https://github.com/friflo/Friflo.Json.Fliox/actions)


## General
There are two general ways to access a database using a [`FlioxClient`](../../../Json/Fliox.Hub/Client/)

- **direct** access a database by using a specific `EntityDatabase` implementation like
  `FileDatabase`, `MemoryDatabase` or other implementations using a `FlioxHub`.  
  This approach is very similar to [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
  or [Dapper](https://github.com/DapperLib/Dapper)

- **remote** access a database via `HTTPHostHub` which by itself access a specific `EntityDatabase` directly.  
  Remote access support **HTTP** and **WebSockets**.

```
    direct:   FlioxClient          ->         FlioxHub : database
    remote:   FlioxClient  ->  HTTPHostHub -> FlioxHub : database
                          HTTP
```



## `FlioxHub`

A `FlioxHub` instance is the single entry point used to handle **all** requests send by a client.  
E.g. direct/remote via a [`FlioxClient`](../../../Json/Fliox.Hub/Client/) or remote-only via an HTTP client - typically a web browser.  
When instantiating a `FlioxHub` an `EntityDatabase` need to be assigned used to execute all
**database operations**, **commands** and **messages** send by a client targeting this database.

Domain specific commands and messages can be added to an `EntityDatabase` by creating a class
that **extends** `TaskHandler`. By convention this class should be called `MessageHandler`.

Domain specific commands and messages can be added to the `MessageHandler` as methods.
An instance of the `MessageHandler` need to be passed when instantiating an `EntityDatabase`.

The ownership of this setup looks like this:

```
    FlioxHub -> EntityDatabase -> TaskHandler
```



## `HTTPHostHub`

A `HTTPHostHub` extends `FlioxHub` by two main features:
- provide access to its databases via **HTTP**
- enables hosting **multiple** databases


A `HTTPHostHub` can be integrated by two different HTTP servers:
- [**ASP.NET Core**](https://docs.microsoft.com/en-us/aspnet/core/) /
  [**Kestrel**](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel)
- `HttpListener` - part of the
  [**.NET Base class library**](https://docs.microsoft.com/en-us/dotnet/standard/framework-libraries#base-class-library)


### **HTTP features**

The HTTP Web API is designed to be used by arbitrary HTTP clients.

A generic Web client utilizing all HTTP features is the [**Hub Explorer**](../../../Json/Fliox.Hub.Explorer/).  
The Explorer is a set of static web files - an SPA - which can be hosted by an `HTTPHostHub`.


HTTP features in detail:

- Provide all **common database operations** to query, read, create, update, delete and patch records

- Support of various database types: **memory**, **file-system**, **remote** and **NoSQL** aka key-value or document databases

- Access the HTTP server in three ways:
    - HTTP **POST** via a single path ./ enabling batching multiple tasks in a single request

    - Send batch requests containing multiple tasks via a **WebSocket**

    - Common **REST** API to POST, GET, PUT, DELETE and PATCH with via a path like ./rest/database/container/id

- Enable **Messaging** and **Pub-Sub** to send messages or commands and setup subscriptions by multiple clients

- Support query **cursors** to fetch container records iteratively

- Enable **user authentication** and **authorization of tasks** send by a user

- Access and change user **permissions** and **roles** required for authorization via the extension database: `user_db`

- Expose server **Monitoring** as an extension database to get statistics about requests and tasks executed by users and clients

- Assign a **schema** to a database to:
    - **validate** records written to the database by its schema definition
    
    - create type definitions for various languages: **Typescript**, **C#**, **Kotlin**, **JSON Schema** and **Html**

    - display entities as **table** in Hub Explorer

    - enable JSON **auto completion**, **validation** and reference **links** in Hub Explorer editor


- Add the Hub Explorer to:
    - **browse** databases, containers and entities

    - execute container queries using a **LINQ** filter expression

    - execute standard or custom database **commands**. E.g. std.Echo
    
    - send **batch** requests via HTTP or WebSocket to the Fliox.Hub server using the **Playground**




# [![JSON Fliox](../../../docs/images/Json-Fliox.svg)](https://github.com/friflo/Friflo.Json.Fliox)     **Remote** ![SPLASH](../../../docs/images/paint-splatter.svg)

[![.NET Tests](https://github.com/friflo/Friflo.Json.Fliox/workflows/.NET/badge.svg)](https://github.com/friflo/Friflo.Json.Fliox/actions)


## General
The namespace `Friflo.Json.Fliox.Hub.Remote` contains classes to enable access to databases via **HTTP**, **WebSockets** and **UDP**. 

```
    client          protocol          server
                        |
HttpClientHub         HTTP       HttpServer or ASP.NET -> HttpHost -> FlioxHub(database)
                        |
WebSocketClientHub  WebSocket    HttpServer or ASP.NET -> HttpHost -> FlioxHub(database)
                        |
UdpSocketClientHub     UDP       UdpServer                         -> FlioxHub(database)

    -> depends on
```

The same `FlioxHub` instance can be used by multiple servers.  
So a single process can run multiple servers using the same databases and serve them
with different protocols (HTTP, WebSocket, UDP) at the same time.

# `HttpHost`
namespace **`Friflo.Json.Fliox.Hub.Remote`**

A `HttpHost` provide two main features:
- offer access to its databases via **HTTP**
- enables hosting **multiple** databases


A `HttpHost` can be integrated by two different HTTP servers:
- [**ASP.NET Core**](https://docs.microsoft.com/en-us/aspnet/core/) /
  [**Kestrel**](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel)
- [`HttpListener`](https://docs.microsoft.com/en-us/dotnet/api/system.net.httplistener) - part of the
  [**.NET Base class library**](https://docs.microsoft.com/en-us/dotnet/standard/framework-libraries#base-class-library)

The ownership of this setup looks like this:

```
    HttpHost -> FlioxHub -> EntityDatabase -> DatabaseService
```


## **HTTP features**

The HTTP Web API is designed to be used by arbitrary HTTP clients.

A generic Web client utilizing all HTTP features is the [**Hub Explorer**](../../Fliox.Hub.Explorer/README.md).  
The Explorer is a set of static web files - an SPA - which can be hosted by an `HttpHost`.


HTTP features in detail:

- Provide all **common database operations** to query, read, create, update, delete and patch records

- Support of various database types: **memory**, **file-system**, **remote** and **NoSQL** aka key-value or document databases

- Access the HTTP server in three ways:
    - HTTP **POST** via a single path ./ enabling batching multiple tasks in a single request

    - Send batch requests containing multiple tasks via a **WebSocket**

    - Common **REST** API to POST, GET, PUT, DELETE and PATCH with via a path like ./rest/database/container/id

- Enable sub-millisecond **Messaging** and **Pub-Sub** to send messages or commands and setup subscriptions by multiple clients

- Support query **cursors** to fetch container records iteratively

- Expose administrative data as extension databases:

    - **`cluster`** - [ClusterStore](../DB/Cluster/ClusterStore.cs) -
      Expose information about hosted databases their containers, commands and schema.  

    - **`monitor`** - [MonitorStore](../DB/Monitor/MonitorStore.cs) -
      Expose server **Monitoring** to get statistics about requests and tasks executed by users and clients.  
    
    - **`user_db`** - [UserStore](../DB/UserAuth/UserStore.cs) -
      Access and change user **permissions** and **roles** required for authorization.  

- Enable **user authentication** and **authorization of tasks** send by a user

- Assign a [DatabaseSchema](../Host/DatabaseSchema.cs) to a database to:
    - **validate** records written to the database by its schema definition
    
    - create type definitions for various languages: **Typescript**, **C#**, **Kotlin**, **JSON Schema** / **OpenAPI**, **GraphQL and **Html**

    - display entities as **table** in Hub Explorer

    - enable JSON **auto completion**, **validation** and reference **links** in Hub Explorer editor


- Add the [**Hub Explorer**](../../Fliox.Hub.Explorer/README.md) to:
    - **browse** databases, containers and entities

    - execute container queries using a **LINQ** filter expression

    - execute standard or custom database **commands**. E.g. std.Echo
    
    - send **batch** requests via HTTP or WebSocket to the Fliox.Hub server using the **Playground**


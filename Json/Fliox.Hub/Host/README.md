

# ![logo](../../../docs/images/Json-Fliox.svg)     **Fliox Hub Host**      ![SPLASH](../../../docs/images/paint-splatter.svg)

[![.NET Tests](https://github.com/friflo/Friflo.Json.Fliox/workflows/.NET/badge.svg)](https://github.com/friflo/Friflo.Json.Fliox/actions)

## `FlioxHub`

A `FlioxHub` instance is the single entry point used to handle **all** requests send by a client -
e.g. via `FlioxClient` or remotely via a web browser.
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
- enables hosting multiple databases
- provide access to its database via HTTP


A `HTTPHostHub` can be integrated by two different HTTP servers:
- **ASP.NET Core** / Kestrel
- `HttpListener` - part of the **.NET** Base Class Library - BCL


### **HTTP** features

- Provide all **common database operations** to query, read, create, update, delete and patch records

- Support of various database types: **memory**, **file-system**, **remote** and **NoSQL** aka key-value or document databases

- Expose access to the service in three ways:
    1. HTTP **POST** via a single path ./ enabling batching multiple tasks in a single request

    2. Send batch requests containing multiple tasks via a **WebSocket**

    3. Common **REST** API to POST, GET, PUT, DELETE and PATCH with via a path like ./rest/database/container/id

- Enable **Messaging** and **Pub-Sub** to send messages or commands and setup subscriptions by multiple clients

- Support query **cursors** to fetch container records iteratively

- Enable user **authentication** and **authorization** of tasks requested by a user

- Access and change user **permissions** and **roles** required for authorization via the extension database: user_db

- Expose server **Monitoring** as an extension database to get statistics about requests and tasks executed by users and clients

- Adding a database **schema** to:
    1. **validate** records written to the database by its schema definition

    2. create type definitions for various languages: Typescript, C#, Kotlin, JSON Schema and Html

    3. display entities as **table** in Hub Explorer

    4. enable JSON **auto completion**, **validation** and reference **links** in Hub Explorer editor

- Add the Hub Explorer to:
    1. browse databases, containers and entities

    2. execute container queries using a **LINQ** filter expression

    3. execute standard or custom database commands. E.g. std.Echo
    
    4. send **batch** requests via HTTP or WebSocket to the Fliox.Hub server using the **Playground**


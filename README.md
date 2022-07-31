

# [![JSON Fliox](docs/images/Json-Fliox.svg)](https://github.com/friflo/Friflo.Json.Fliox)    **JSON Fliox**      ![SPLASH](docs/images/paint-splatter.svg)

[![.NET Tests](https://github.com/friflo/Friflo.Json.Fliox/workflows/.NET/badge.svg)](https://github.com/friflo/Friflo.Json.Fliox/actions)


__`SELECT * FROM table1 LEFT JOIN table2 ON 💩 WHERE 💩 💩 💩`__

*Same attitude?* 😉  
*Then you may have a short look at this project*  <br/><br/>


## Description
**JSON Fliox** is a **.NET** library supporting **simple** and **efficient** access to **NoSQL** databases via C# or Web clients.  
Its **ORM** enables **Schema** creation. Its **Hub** serve hosted databases using these schemas via HTTP.

|              | Description                                                               | API  |
| ------------ | ------------------------------------------------------------------------- | ---- |
| ORM Client   | Object Relational Mapper - to access to NoSQL databases with .NET clients | C#   |
| Database Hub | A service hosting a set of NoSQL databases via an **ASP.NET Core** server | HTTP |

*Project classification*: As **JSON Fliox** is an [ORM](https://en.wikipedia.org/wiki/Object-relational_mapping) is has similarities to projects like
- [Entity Framework Core](https://en.wikipedia.org/wiki/Entity_Framework) · C#
- [Dapper ORM](https://en.wikipedia.org/wiki/Dapper_ORM) · C#, SQL
- [Ruby on Rails - Active Record Pattern](https://en.wikipedia.org/wiki/Ruby_on_Rails) · Ruby
- [Django](https://en.wikipedia.org/wiki/Django_(web_framework)) · Python
- [Hibernate](https://de.wikipedia.org/wiki/Hibernate_(Framework)) · Java
- [TypeORM](https://github.com/typeorm/typeorm) · Typescript
- [Prisma](https://github.com/prisma/prisma) · Typescript

*Pronunciation*: **io** in **Fliox** is same as in **Riot** <br/><br/>

### **TL;DR**

A demo server running on AWS - [**DemoHub**](http://ec2-174-129-178-18.compute-1.amazonaws.com/) (EC2 instance: t2-micro, us-east-1)  
The **DemoHub** .NET project is available at the
[friflo/FlioxHub.Demos](https://github.com/friflo/FlioxHub.Demos#-flioxhubdemos-) repository.

<br/>

*Note*: JSON Fliox is **not** a UI library. It is designed for simple integration in UI frameworks like:

| Platform | Frameworks                                                                             |
|:--------:| -------------------------------------------------------------------------------------- |
| **.NET** | ASP.NET Razor, Blazor, MAUI, WinUI, ASP.NET MVC, WPF, WinForms, Xamarin.Forms & Unity. |
| **Web**  | React, Angular, Vue.js, Svelte, Preact, Ember.js, ...                                  |

<br/>

## Features

Compact list of features supported by Clients and Hubs
- ASP.NET Core & HttpListener integration
    - REST API - JSON Schema / OpenAPI
    - GraphQL API
    - Batch API - HTTP & WebSocket
- CRUD
- Queries - LINQ expressions
- Container relations & associations
- Database Schema
- Code generation
    - C#, Typescript & Kotlin
    - JSON Schema / OpenAPI
    - GraphQL Schema
    - Database Schema diagram
- JSON Validation - Records & DTO's
- Messages & Commands using DTO's
- Pub-Sub
- Hub Explorer
- Monitoring
- Authentication / Authorization

The features are explained via a set of `README` files grouped by their topic linked below.  
*Topics*: Demos, Client, Host, Hub Explorer, DB - support databases, Protocol, Schema & Mapper.

<br/>

## Motivation

The main driver of this project is the development of an competitive online multiplayer game -
a still unresolved task in my life todo list.  
The foundation to achieve this is commonly a module called *Netcode* in online multiplayer games.  
The key aspects of *Netcode* are: Synchronizing game state, low latency, high throughput, minimal use of system resources, reliability & easy to use API.
As Unity is selected as the Game engine C# .NET is the way to go.

Another objective is to create an open source software project which has the potential to be popular.  
As I have 15+ years experience as a software developer in enterprise environment - greetings to [HERE Technologies](https://www.here.com/) -
I decided to avoid a Vendor Lock-In to Unity and target for a solution which fits also the needs of enterprise projects.
So development is entirely done with .NET Core while checking Unity compatibility on a regular basis.

The result is a project with a feature set useful in enterprise & gaming projects and targeting for optimal performance.  
The common ground of both areas - enterprise & game development - is the need of databases.
In context of game development the game state (Players, NPC, objects, ...) is represented as an in-memory database.  
In enterprise projects instead databases are used to store any kind of data persistent by using any popular DBMS.

<br/><br/>
<p>             <img src="docs/images/welcome.svg" width="320" height="120" ></p>

## Content

- **Demos**               ❯  [friflo/FlioxHub.Demos](https://github.com/friflo/FlioxHub.Demos#-flioxhubdemos-)

- **Fliox Hub**
    - [Client](#client)                 ❯  [README.md](Json/Fliox.Hub/Client/README.md)
    - [Host](#host)                   ❯  [README.md](Json/Fliox.Hub/Host/README.md)
    - [Explorer](#explorer)             ❯  [README.md](Json/Fliox.Hub.Explorer/README.md)
    - [DB](#db)                      ❯  [README.md](Json/Fliox.Hub/DB/README.md)
    - [Protocol](#protocol)             ❯  [README.md](Json/Fliox.Hub/Protocol/README.md)
- **Fliox**
    - [Schema](#schema)              ❯  [README.md](Json/Fliox/Schema/README.md)
    - [Mapper](#mapper)              ❯  [README.md](Json/Fliox/Mapper/README.md)
- **Testing**
    - [Unit Tests](#unit-tests)           ❯  [README.md](Json.Tests/README.md)
- **Design**
    - [API](#api)                     ❯  [friflo/fliox-docs](https://github.com/friflo/fliox-docs)
    - [Principles](#principles)

<br/>

## **Demos**
📄   [friflo/FlioxHub.Demos](https://github.com/friflo/FlioxHub.Demos#-flioxhubdemos-)

A separate git repository containing two **ready to run** examples showcasing the usage of Fliox Clients and Hubs.  
The examples are **the place to go** showing how to use the C# and the Web API.

[<img src="docs/images/server-log.png" width="647" height="191">](https://github.com/friflo/FlioxHub.Demos#-flioxhubdemos-)  
*screenshot: DemoHub server logs*
<br/><br/>


## **Fliox Hub**

namespace **`Friflo.Json.Fliox.Hub`**

### **Client**
📄   [README.md](Json/Fliox.Hub/Client/README.md)

Fliox clients are strongly typed C# classes used to access NoSQL databases.  
They are implemented by creating a class e.g. `MyClient` extending `FlioxClient`.  
The database containers are represented as properties in the derived class `MyClient`.  

These classes also acts as a database schemas. They can be assigned to databases hosted on the Hub.  
Doing this enables features like:
- JSON validation of entities aka records
- generation of schemas for OpenAPI or GraphQL
- generate a single HTML page with a complete schema documentation
- generate a UML class diagram to visualize a database schema
- generate types declared by the database schema for various programming languages
- implementing generic database explorers like the Hub Explorer

The `MyClient` can be used to declare custom database commands using DTO's as input and result types.


### **Host**
📄   [README.md](Json/Fliox.Hub/Host/README.md)

A `HttpHost` instance is used to host multiple NoSQL databases.  
It is designed to be integrated into HTTP servers like **ASP.NET Core**.  
This enables access to hosted databases via HTTP or WebSocket supporting the following Web API's:
- REST
- GraphQL
- Batch API

A `FlioxHub` instance is used to configure the hosted databases, authentication / authorization and Pub-Sub.  
This `FlioxHub` instance need to be passed to the constructor of the `HttpHost`

### **Explorer**
📄   [README.md](Json/Fliox.Hub.Explorer/README.md)

The Hub Explorer is a generic Web UI - a single page application - used to access
databases, containers and entities hosted by a Fliox Hub.  
The Explorer also enables to execute application specific database commands.

[<img src="docs/images/Fliox-Hub-Explorer.png" width="717" height="278">](Json/Fliox.Hub.Explorer/README.md)  
*screenshot: Hub Explorer*

### **DB**
📄   [README.md](Json/Fliox.Hub/DB/README.md)

Provide a set of support databases used to:
- serve the Hub configuration. Schema:
  [ClusterStore](Json.Tests/assets~/Schema/Markdown/ClusterStore/class-diagram.md)
- serve monitoring data. Schema:
  [MonitorStore](Json.Tests/assets~/Schema/Markdown/MonitorStore/class-diagram.md)
- perform user authentication, authorization and management. Schema:
  [UserStore](Json.Tests/assets~/Schema/Markdown/UserStore/class-diagram.md)

### **Protocol**
📄   [README.md](Json/Fliox.Hub/Protocol/README.md)

The Protocol is the communication interface between a `FlioxClient` and a `FlioxHub`.  
Web clients can use this Protocol to access a Hub using the Batch API via HTTP & JSON.

The Protocol is not intended to be used by C# .NET clients directly.  
Instead they are using a `FlioxClient` that is optimized to transform API calls into the Protocol.

<br/><br/>



## **Fliox**

namespace **`Friflo.Json.Fliox`**


### **Schema**
📄   [README.md](Json/Fliox/Schema/README.md)

This module enables transforming schemas expressed by a set of C# classes into
other programming languages and schema formats like:

| Language / Schema     | used in Hub Explorer by ...                                                                           |
| --------------------- | ----------------------------------------------------------------------------------------------------- |
| C#                    |                                                                                                       |
| Typescript            |                                                                                                       |
| Kotlin                |                                                                                                       |
| HTML                  | [![HTML](docs/images/doc.svg)](Json/Fliox/Schema/README.md#html-documentation) links to documentation |
| JSON Schema / OpenAPI | [![OAS](docs/images/oas.svg)](Json/Fliox/Schema/README.md#swagger-ui) links to Swagger UI             |
| GraphQL               | [![GQL](docs/images/gql.svg)](Json/Fliox/Schema/README.md#graphiql) links to GraphiQL                 |
| Mermaid               | [![CD](docs/images/cd.svg)](Json/Fliox/Schema/README.md#class-diagram) links to class diagram         |

Its main purpose is to generate database schemas and types for various languages of classes extending `FlioxClient`.

The links in the table above navigate to pages utilizing the generated schemas. Like the class diagram below.

[<img src="docs/images/MonitorStore-schema.png" width="739" height="226">](Json/Fliox/Schema/README.md#class-diagram)

*screenshot: MonitorStore schema as class diagram*

[HTML Schema screenshot](Json/Fliox/Schema/README.md#html-documentation)  
[Swagger UI screenshot](Json/Fliox/Schema/README.md#swagger-ui)  
[GraphiQL screenshot](Json/Fliox/Schema/README.md#graphiql)



### **Mapper**
📄   [README.md](Json/Fliox/Mapper/README.md)

This module enables serialization / deserialization of C# .NET objects to / from JSON.  
Its feature set and API is similar to the .NET packages:
- [JamesNK/Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json)
- [System.Text.Json](https://docs.microsoft.com/en-us/dotnet/api/system.text.json)

The module is utilized by the assembly `Friflo.Json.Fliox.Hub` to serialize entities and DTO's.  
Its also used for serialization of the supported protocols: REST, GraphQL and Batch API.

<br/><br/>



## **Testing**

### **Unit Tests**
📄   [README.md](Json.Tests/README.md)

Code coverage: **86%** measured with **JetBrains • docCover**

*summarized logs of test execution*
```yaml
Passed! - Failed:   0, Passed:   6, Skipped:   0, Total:   6, Duration: 2 s -  .../DemoTest.dll
Passed! - Failed:   0, Passed:   7, Skipped:   0, Total:   7, Duration: 1 s -  .../TodoTest.dll
Passed! - Failed:   0, Passed: 347, Skipped:   0, Total: 347, Duration: 15 s - .../Friflo.Json.Tests.dll
```


## **Design**

### **API**

The Fliox **C# .NET** API is [CLS-compliant](https://docs.microsoft.com/en-us/dotnet/api/system.clscompliantattribute#remarks)

The API is available at [**fliox-docs API Reference**](https://github.com/friflo/fliox-docs)

### **Principles**

- dependencies
    - no 3rd party dependencies
    - small size of Fliox assemblies (*.dll) ~ 850 kb in total, 350 kb zipped  
      source code: library 47k LOC, unit tests: 18k LOC
- target for optimal performance
    - maximize throughput, minimize latency, minimize heap allocations and boxing
    - enable task batching as a unit-of-work
    - support bulk operations for CRUD commands
- compact and strongly typed API
    - type safe access to entities and their keys when dealing with containers  
    - type safe access to DTO's when dealing with database commands
    - absence of using `object` as a type
    - absence of utility classes & methods to
        - to use the API in an explicit manner
        - to avoid confusion implementing the same feature in multiple ways
- serialization of entities and messages - request, response & event - are entirely JSON
- Fliox Clients and Hubs are unit testable without mocking
- the **Zero** principles
    - 0 compiler errors and warnings
    - 0 ReSharper errors, warnings, suggestions and hints
    - 0 unit test errors, no flaky tests
    - 0 typos - observed by spell checker
    - no synchronous calls to API's dealing with **IO** like network or disc    
      Instead using `async` / `await`
    - no 3rd party dependencies
    - no heap allocations if possible
    - no noise in `.ToString()` methods while debugging - only relevant state.  
      E.g. instances of `FlioxClient`, `EntitySet<,>`, `FlioxHub` and `EntityDatabase`
    - no surprise of API behavior.  
      See [Principle of least astonishment](https://en.wikipedia.org/wiki/Principle_of_least_astonishment)
    - no automatic C# Code formatting - as no Code Formatter supports the code style of this project.  
      That concerns tabular indentation of fields, properties and variables.      
- extensibility
    - support custom database adapters aka providers
    - support custom code / schema generators for new programming languages
- compatibility
    - **.NET Core 3.1** and higher
    - **Unity 2020.1** and higher 

<br/>

## Credits
|                                                                           |             |                                                                 |
| ------------------------------------------------------------------------- | ----------- | --------------------------------------------------------------- |
| [NUnit](https://nunit.org/)                                               | C#          | unit testing of the JSON Fliox library and the Demos            |
| [FluentAssertions](https://github.com/fluentassertions/fluentassertions)  | C#          | unit testing of the JSON Fliox library                          |
| [GraphQL.NET Parser](https://github.com/graphql-dotnet/parser)            | C#          | used by package: Friflo.Json.Fliox.Hub.GraphQL                  |
| [.NET platform](https://dotnet.microsoft.com/en-us/)                      | C# .NET     | the platform providing compiler, runtime, IDE's & ASP.NET Core  |
| [Swagger](https://swagger.io/)                                            | static JS   | a REST / OpenAPI UI linked by the Hub Explorer                  |
| [GraphiQL](https://github.com/graphql/graphiql)                           | static JS   | a GraphQL UI linked by the Hub Explorer                         |
| [Mermaid](https://github.com/mermaid-js/mermaid)                          | static JS   | class diagram for database schema linked by the Hub Explorer    |
| [Monaco Editor](https://github.com/microsoft/monaco-editor)               | static JS   | used as JSON editor integrated in the Hub Explorer              |
| [WinMerge](https://github.com/WinMerge/winmerge)                          | Application | heavily used in this project                                    |
| [Inscape](https://gitlab.com/inkscape/inkscape)                           | Application | to create SVG's for this project                                |


Happy coding!  
😊 💻

<br/>

## License

This project is licensed under AGPLv3.

Project not published nor released yet.

friflo JSON Fliox  
Copyright © 2022 Ullrich Praetz

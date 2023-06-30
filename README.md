

# [![JSON Fliox](docs/images/Json-Fliox.svg)](https://github.com/friflo/Friflo.Json.Fliox)    **JSON Fliox** ![SPLASH](docs/images/paint-splatter.svg)

[![nuget](https://img.shields.io/nuget/v/Friflo.Json.Fliox.Hub.svg?color=blue)](https://www.nuget.org/packages/Friflo.Json.Fliox.Hub) 
[![CI](https://github.com/friflo/Friflo.Json.Fliox/workflows/CI/badge.svg)](https://github.com/friflo/Friflo.Json.Fliox/actions/workflows/dotnet.yml) 
[![CD](https://github.com/friflo/Friflo.Json.Fliox/workflows/CD/badge.svg)](https://github.com/friflo/Friflo.Json.Fliox/actions/workflows/nuget.yml) 

![new](docs/images/new.svg) • Published database providers for: SQLite, MySQL, MariaDB, PostgreSQL & SQL Server. 

**.NET** library supporting **simple** and **performant** access to **SQL & NoSQL** databases via C# or Web clients.  
Its **ORM** enables **Schema** creation. Its **Hub** serve hosted databases using these schemas via HTTP.

The **ORM** client - Object Relational Mapper - is used to access databases via .NET.  
The **Hub** is a service hosting a set of databases via an **ASP.NET Core** server.

As Fliox is an [ORM](https://en.wikipedia.org/wiki/Object-relational_mapping) it has similarities to projects like
[Entity Framework Core](https://en.wikipedia.org/wiki/Entity_Framework),
[Ruby on Rails](https://en.wikipedia.org/wiki/Ruby_on_Rails),
[Django](https://en.wikipedia.org/wiki/Django_(web_framework)) or
[Hibernate](https://de.wikipedia.org/wiki/Hibernate_(Framework)).  
In case of SQL databases Fliox store entities in JSON columns to avoid [object–relational impedance mismatch](https://en.wikipedia.org/wiki/Object%E2%80%93relational_impedance_mismatch).


**TL;DR**

Try the example Hub online running on AWS - [**DemoHub**](http://ec2-18-215-176-108.compute-1.amazonaws.com/) (EC2 instance: t2-micro, us-east-1)  
The **DemoHub** .NET project is available at
[**🚀 friflo/Fliox.Examples**](https://github.com/friflo/Fliox.Examples/blob/main/README.md#-content).

<br/>

|                    Performance characteristics                            |                                                                                                             |
| ------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------- |
| **RTT**           request / response roundtrip                            | **0.3 ms**                                                                                                  |
| **Pub-Sub delay** send message ➞ subscriber event                        | **sub millisecond latency**                                                                                 |
| **Pub-Sub throughput FIFO** 3 subscribers each using a WebSocket          | **50k messages / sec**                                                                                      |
| **Query**         request 1M rows, each row 124 byte => response 125MB    | **1.3 sec**                                                                                                 |
| **Throughput**    request / response WebSocket, 4 concurrent clients      | **27k requests / sec**                                                                                      |
| **ASP.NET Core**  Hub integration                                         | **1 LOC** [Startup.cs](https://github.com/friflo/Fliox.Examples/blob/main/Demo/Hub/Startup.cs#L24)          |
| **Minimal Client & Server** with: REST, CRUD, Queries, Pub-Sub & Explorer | **60 LOC** [Client](https://github.com/friflo/Fliox.Examples/blob/main/Todo/Client/TodoClient.cs) & [Server](https://github.com/friflo/Fliox.Examples/blob/main/Todo/Hub/Program.cs) |
| &nbsp;           run on Intel(R) Core(TM) i7-4790K CPU 4.00GHz            |                                                                                                |



*Note*: JSON Fliox is **not** a UI library. It is designed for simple integration in .NET and Web UI frameworks.

Published project on GitHub 2022-08

<br/>


## 🚩 Content

- [Features](#-features)
- [Quickstart](#-quickstart)
- [Providers](#-database-providers)
- [Examples](#-examples)           ❯  [🚀 friflo/Fliox.Examples](https://github.com/friflo/Fliox.Examples/blob/main/README.md#-content)
- [Hub](#-hub)
    - [Client](#client)                 ❯  [README.md](Json/Fliox.Hub/Client/README.md)
    - [Host](#host)                   ❯  [README.md](Json/Fliox.Hub/Host/README.md)
    - [Explorer](#explorer)             ❯  [README.md](Json/Fliox.Hub.Explorer/README.md)
    - [DB](#db)                      ❯  [README.md](Json/Fliox.Hub/DB/README.md)
    - [Protocol](#protocol)             ❯  [README.md](Json/Fliox.Hub/Protocol/README.md)
- [Fliox](#-fliox)
    - [Schema](#schema)              ❯  [README.md](Json/Fliox/Schema/README.md)
    - [Mapper](#mapper)              ❯  [README.md](Json/Fliox/Mapper/README.md)
- [Project](#-project)
    - [API](#api)                     ❯  [friflo/fliox-docs](https://github.com/friflo/fliox-docs)
    - [Properties](#properties)
    - [Principles](#principles)
    - [Build](#build)                  ❯  [README.md](Json.Tests/README.md)
- [Motivation](#-motivation)
- [Credits](#-credits)


<br/>

## 🎨 Features

Compact list of features supported by Clients and Hubs
- ASP.NET Core & HttpListener integration
    - REST API - JSON Schema / OpenAPI
    - GraphQL API
    - Batch API - HTTP, WebSocket & UDP
- Database
    - CRUD operations
    - Transactions - Begin, Commit & Rollback - for SQL databases
    - Schema
    - Queries - LINQ expressions
    - Container relations (associations)
    - Entity validation
    - Send messages (event) & commands (RPC) with validation
    - Pub-Sub - entity changes, messages and commands
- Hub Explorer - the admin page
- Monitoring
- Authentication / Authorization
- Code generation
    - C#, Typescript & Kotlin
    - JSON Schema, OpenAPI Schema & GraphQL Schema
    - Schema documentation & class diagram

The features are explained within the topics (= namespaces) below.  
*Topics*: Client, Host, Hub Explorer, DB - support databases, Protocol, Schema & Mapper.

<br/>

## 🌠 **Quickstart**

### **Direct database access**

Create a **Console Application** and add the following dependencies:  
[![nuget](https://img.shields.io/nuget/v/Friflo.Json.Fliox.Hub.svg?label=Hub&color=blue)](https://www.nuget.org/packages/Friflo.Json.Fliox.Hub)
[![nuget](https://img.shields.io/nuget/v/Friflo.Json.Fliox.Hub.SQLite.svg?label=SQLite&color=blue)](https://www.nuget.org/packages/Friflo.Json.Fliox.Hub.SQLite)
```
dotnet add package Friflo.Json.Fliox.Hub
dotnet add package Friflo.Json.Fliox.Hub.SQLite
```

Create a `TodoDB` client to specify the database schema.

📄 `TodoDB.cs`
```csharp
public class TodoDB : FlioxClient
{
    public readonly EntitySet <long, Job>     jobs;

    public TodoDB(FlioxHub hub, string dbName = null) : base(hub, dbName) { }
}

public class Job
{
    public  long        id;
    public  string      name;
    public  bool        completed;
}
```

The following code create / open a <b>SQLite</b> database by using `TodoDB` as the database schema.  
Perform some database operations like: `UpsertRange()` & `Query()`

📄 `Program.cs`
```csharp
    var schema      = DatabaseSchema.Create<TodoDB>();
    var database    = new SQLiteDatabase("todo_db", "Data Source=todo.sqlite3", schema);
    var hub         = new FlioxHub(database);

    var client      = new TodoDB(hub);
    client.jobs.UpsertRange(new[] {
        new Job { id = 1, name = "Buy milk", completed = true },
        new Job { id = 2, name = "Buy cheese", completed = false }
    });
    var jobs = client.jobs.Query(job => job.completed == true);
    await client.SyncTasks(); // execute UpsertRange & Query task
    
    foreach (var job in jobs.Result) {
        Console.WriteLine($"{job.id}: {job.name}");
    }
    // output:  1: Buy milk
```

<br/>

### **Remote database access**

Remote access require two console applications:
1. HTTP Server hosting a single / multiple databases
2. HTTP client to access a hosted database


#### **1. HTTP Server**

Add dependency to **Hub Explorer** to host a Web UI to browse databases  
[![nuget](https://img.shields.io/nuget/v/Friflo.Json.Fliox.Hub.Explorer.svg?label=Hub.Explorer&color=blue)](https://www.nuget.org/packages/Friflo.Json.Fliox.Hub.Explorer)
```
dotnet add package Friflo.Json.Fliox.Hub.Explorer
```

Replace the code in 📄 `Program.cs` above to host a database by an <b>ASP.NET Core</b> server.

📄 `Program.cs` *(server)*
```csharp
    var schema      = DatabaseSchema.Create<TodoDB>();
    var database    = new SQLiteDatabase("todo_db", "Data Source=todo.sqlite3", schema);
    var hub         = new FlioxHub(database);
    
    hub.UseClusterDB(); // required by HubExplorer
    hub.UsePubSub();
    var httpHost    = new HttpHost(hub, "/fliox/");
    httpHost.UseStaticFiles(HubExplorer.Path); // optional: Hub Explorer Web UI
    var app         = WebApplication.Create();
    app.UseWebSockets();
    app.MapHost("/fliox/{*path}", httpHost);
    app.Run();
```

Check the **Hub Explorer** is available at http://localhost:5000/fliox/

The C# documentation of `TodoDB` and other model classes can be made available in the Hub Explorer by adding the following xml snippet to the *.csproj.  
This configuration will copy the *.xml files next to the *.dll files. The server read and add the documentation to schema definition.

```xml
    <!-- Copy XML files from all PackageReferences to output dir -->
    <Target Name="_ResolveCopyLocalNuGetPkgXmls" AfterTargets="ResolveReferences">
        <ItemGroup>
        <ReferenceCopyLocalPaths Include="@(ReferenceCopyLocalPaths->'%(RootDir)%(Directory)%(Filename).xml')" Condition="'%(ReferenceCopyLocalPaths.NuGetPackageId)'!='' and Exists('%(RootDir)%(Directory)%(Filename).xml')" />
        </ItemGroup>
    </Target>
```


#### **2. HTTP Client**

Create a second **Console application** to access the hosted database via HTTP.

Add required nuget dependencies  
[![nuget](https://img.shields.io/nuget/v/Friflo.Json.Fliox.Hub.svg?label=Hub&color=blue)](https://www.nuget.org/packages/Friflo.Json.Fliox.Hub)
```
dotnet add package Friflo.Json.Fliox.Hub
```
Copy 📄 TodoDB.cs from above to Console project.  
*Note:* `TodoDB` and its model classes should be in a separate library project and used by client & server.
But for simplicity create a copy for now.

📄 `Program.cs` *(client)*
```csharp
    var hub     = new WebSocketClientHub("todo_db", "http://localhost:5000/fliox/");
    var client  = new TodoDB(hub);
    var jobs    = client.jobs.Query(job => job.completed == true);
    client.jobs.SubscribeChanges(Change.All, (changes, context) => {
        Console.WriteLine(changes);
    });
    await client.SyncTasks(); // execute Query & SubscribeChanges task
    
    foreach (var job in jobs.Result) {
        Console.WriteLine($"{job.id}: {job.name}");
    }
    // output:  1: Buy milk
```

<br/>

## ⛁ **Database providers**

| Database       | class / nuget        | connection string examples                                               
| -------------- | -------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| in-memory      | `MemoryDatabase`     | *none*                                                                   
| file-system    | `FileDatabase`       | *path of root folder*                                                      
| **SQLite**     | `SQLiteDatabase`     | `"Data Source=test_db.sqlite3"`   
|                | [![nuget](https://img.shields.io/nuget/v/Friflo.Json.Fliox.Hub.SQLite.svg?label=SQLite&color=blue)](https://www.nuget.org/packages/Friflo.Json.Fliox.Hub.SQLite)          | `dotnet add package Friflo.Json.Fliox.Hub.SQLite`
| **MySQL**      | `MySQLDatabase`      | `"Server=localhost;User ID=root;Password=;Database=test_db;"`
|                | [![nuget](https://img.shields.io/nuget/v/Friflo.Json.Fliox.Hub.MySQL.svg?label=MySQL&color=blue)](https://www.nuget.org/packages/Friflo.Json.Fliox.Hub.MySQL)            | `dotnet add package Friflo.Json.Fliox.Hub.MySQL`
| **MariaDB**    | `MariaDBDatabase`    | `"Server=localhost;User ID=root;Password=;Database=test_db;"`
|                | [![nuget](https://img.shields.io/nuget/v/Friflo.Json.Fliox.Hub.MySQL.svg?label=MySQL&color=blue)](https://www.nuget.org/packages/Friflo.Json.Fliox.Hub.MySQL)            | `dotnet add package Friflo.Json.Fliox.Hub.MySQL`
| **PostgreSQL** | `PostgreSQLDatabase` | `"Host=localhost;Username=postgres;Password=postgres;Database=test_db;"`
|                | [![nuget](https://img.shields.io/nuget/v/Friflo.Json.Fliox.Hub.PostgreSQL.svg?label=PostgreSQL&color=blue)](https://www.nuget.org/packages/Friflo.Json.Fliox.Hub.PostgreSQL)  | `dotnet add package Friflo.Json.Fliox.Hub.PostgreSQL`
| **SQL Server** | `SQLServerDatabase`  | `"Data Source=.;Integrated Security=True;Database=test_db"`
|                | [![nuget](https://img.shields.io/nuget/v/Friflo.Json.Fliox.Hub.SQLServer.svg?label=SQLServer&color=blue)](https://www.nuget.org/packages/Friflo.Json.Fliox.Hub.SQLServer)    | `dotnet add package Friflo.Json.Fliox.Hub.SQLServer`

<br/>

## 🚀 **Examples**
📄   [friflo/Fliox.Examples](https://github.com/friflo/Fliox.Examples/blob/main/README.md#-content)

A separate git repository with two small **ready-to-run** examples (70 LOC & 550 LOC) using Fliox Clients and Servers.  
Build and run a server with [**Gitpod**](https://github.com/friflo/Fliox.Examples/blob/main/README.md#-build) using VSCode in the browser without installing anything.

[<img src="docs/images/server-log.png" width="647" height="191">](https://github.com/friflo/Fliox.Examples/blob/main/README.md#-content)  
*screenshot: DemoHub server logs*
<br/><br/>


## 📦 **Hub**

Namespace    Friflo.Json.Fliox.Hub.*  
Assembly     Friflo.Json.Fliox.Hub.dll  <a href="https://www.nuget.org/packages/Friflo.Json.Fliox.Hub">
  <img src="https://img.shields.io/nuget/v/Friflo.Json.Fliox.Hub.svg?label=Hub&color=blue" alt="CI" align = "center"/>
</a>

### **Client**
📄   [README.md](Json/Fliox.Hub/Client/README.md)

Fliox clients are strongly typed C# classes used to access SQL or NoSQL databases.  
They are implemented by creating a class e.g. `MyClient` extending `FlioxClient`.  
The database containers are represented as properties in the derived class `MyClient`.  

These classes also acts as a database schemas. They can be assigned to databases hosted on the Hub.  
Doing this enables features like:
- JSON validation of entities aka records
- generate a JSON Schema, an OpenAPI Schema and a GraphQL Schema
- generate a HTML Schema documentation and a UML class diagram
- generate classes for various programming languages: C#, Typescript & Kotlin

The `MyClient` can be used to declare custom database commands using DTO's as input and result types.


### **Host**
📄   [README.md](Json/Fliox.Hub/Host/README.md)

A `HttpHost` instance is used to host multiple databases.  
It is designed to be integrated into HTTP servers like **ASP.NET Core**.  
This enables access to hosted databases via HTTP, WebSocket or UDP supporting the following Web API's:
- REST
- GraphQL
- Batch API

A `FlioxHub` instance is used to configure the hosted databases, authentication / authorization and Pub-Sub.  
This `FlioxHub` instance need to be passed to the constructor of the `HttpHost`

### **Explorer**
📄   [README.md](Json/Fliox.Hub.Explorer/README.md)  
Assembly     Friflo.Json.Fliox.Hub.Explorer.dll  <a href="https://www.nuget.org/packages/Friflo.Json.Fliox.Hub.Explorer">
  <img src="https://img.shields.io/nuget/v/Friflo.Json.Fliox.Hub.Explorer.svg?label=Hub.Explorer&color=blue" alt="CI" align = "center"/>
</a>

The Hub Explorer is an admin page used to access
databases, containers and entities hosted by a Fliox Hub.  
The Explorer also enables to execute application specific database commands.

[<img src="docs/images/Fliox-Hub-Explorer.png" width="717" height="278">](Json/Fliox.Hub.Explorer/README.md)  
*screenshot: Hub Explorer*

### **DB**
📄   [README.md](Json/Fliox.Hub/DB/README.md)

Provide a set of support databases used to:
- serve the Hub configuration - used by the Hub Explorer. Schema:
  [ClusterStore](Json.Tests/assets~/Schema/Markdown/ClusterStore/class-diagram.md)
- serve monitoring data. Schema:
  [MonitorStore](Json.Tests/assets~/Schema/Markdown/MonitorStore/class-diagram.md)
- perform user authentication, authorization and management. Schema:
  [UserStore](Json.Tests/assets~/Schema/Markdown/UserStore/class-diagram.md)

### **Protocol**
📄   [README.md](Json/Fliox.Hub/Protocol/README.md)

The Protocol is the communication interface between a `FlioxClient` and a `FlioxHub`.  
Web clients can use this Protocol to access a Hub using the Batch API via HTTP & JSON.  
A language specific API - e.g. written in Typescript, Kotlin, ... - is not a requirement.

The Protocol is not intended to be used by C# .NET clients directly.  
Instead they are using a `FlioxClient` that is optimized to transform API calls into the Protocol.

<br/><br/>



## 📦 **Fliox**

Namespace    Friflo.Json.Fliox.*  
Assembly     Friflo.Json.Fliox.dll  <a href="https://www.nuget.org/packages/Friflo.Json.Fliox">
  <img src="https://img.shields.io/nuget/v/Friflo.Json.Fliox.svg?label=Fliox&color=blue" alt="CI" align = "center"/>
</a>


### **Schema**
📄   [README.md](Json/Fliox/Schema/README.md)

This module enables transforming schemas expressed by a set of C# classes into
other programming languages and schema formats like:

- C#, Typescript, Kotlin
- HTML documentation, Schema Class Diagram
- JSON Schema, OpenAPI Schema, GraphQL Schema

Its main purpose is to generate schemas and types for various languages of classes extending `FlioxClient`.  
The screenshots below show Hub pages utilizing the schemas mentioned above.

[<img src="docs/images/MonitorStore-schema.png" width="739" height="226">](Json/Fliox/Schema/README.md#class-diagram)  
*screenshot: MonitorStore schema as class diagram*


[<img src="docs/images/schema-screenshots.png" width="770" height="85">](Json/Fliox/Schema/README.md#html-documentation)  
*screenshots: Schema documentation, Swagger UI & GraphiQL*



### **Mapper**
📄   [README.md](Json/Fliox/Mapper/README.md)

This module enables serialization / deserialization of C# .NET objects to / from JSON.  
Its feature set and API is similar to the .NET packages:
- [JamesNK/Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json)
- [System.Text.Json](https://docs.microsoft.com/en-us/dotnet/api/system.text.json)

The module is utilized by the assembly `Friflo.Json.Fliox.Hub` to serialize entities and DTO's.  
Its also used for serialization of the supported protocols: REST, GraphQL and Batch API.

<br/><br/>



## 🔧 **Project**

### **API**

The Fliox **C# .NET** API is [CLS-compliant](https://docs.microsoft.com/en-us/dotnet/api/system.clscompliantattribute#remarks)  
The API is available at [**fliox-docs API Reference**](https://github.com/friflo/fliox-docs)


### **Properties**

The goal of the library, its components and API is to be easy digestible for software developers.  
The properties describe the characteristics of this project - at least what it aims for.  
These properties are targeted to the needs of users using the library.  
They fit mostly the aspects described in [CUPID-for joyful coding](https://dannorth.net/2022/02/10/cupid-for-joyful-coding/).

Topics of the CUPID properties focused by this project are
- Composable
    - **No 3rd party dependencies**
    - [Compatibility](./docs/compatibility.md)
      **.NET Core 3.1** or higher, **.NET Framework 4.6.2** or higher and **Unity 2020.1** or higher
    - Seamless integration into existing ASP.NET Core applications with a handful lines of code
    - Independence from other parts of existing applications
    - Fliox Clients and Hubs are unit testable without mocking
    - Replace the underlying database without changing application code
    - Add custom code / schema generators without changing the application code
- Predictable
    - API surface is as small as possible
    - API: class, method and property names are short, simple, and easy to pronounce
    - Observable
        - Monitoring is integral part of the Hub
        - The `ToString()` methods of classes show only relevant state to avoid noise in debugging sessions
        - Error and runtime assertion messages are short and expressive
- Domain based
    - Enable implementing compact applications which are easy to read and to maintain

### Principles

A set of rules followed by this project to aim for simplicity and performance. See [Principles](docs/principles.md)

### **Build**
📄   [README.md](Json.Tests/README.md)

The project **Json.Tests** contains a console application and unit tests.  
Build and run instructions for .NET and Unity are in the README file.

**unit tests**  
Code coverage: **86%** measured with **JetBrains • docCover**

```yaml
Passed! - Failed:   0, Passed:   6, Skipped:   0, Total:   6, Duration: 2 s -  .../DemoTest.dll
Passed! - Failed:   0, Passed:   7, Skipped:   0, Total:   7, Duration: 1 s -  .../TodoTest.dll
Passed! - Failed:   0, Passed: 347, Skipped:   0, Total: 347, Duration: 15 s - .../Friflo.Json.Tests.dll
```
*summarized logs of unit test execution* - they are executed in  
<a href="https://github.com/friflo/Friflo.Json.Fliox/actions/workflows/dotnet.yml">
  <img src="https://github.com/friflo/Friflo.Json.Fliox/workflows/CI/badge.svg" alt="CI" align = "center"/>
</a>


<br/>

## 🔥 Motivation

The main driver of this project is the development of an competitive online multiplayer game -
a still unresolved task in my todo list.  
The foundation to achieve this is commonly a module called *Netcode* in online multiplayer games.  
The key aspects of *Netcode* are: Synchronizing game state, messaging, low latency, high throughput,
minimal use of system resources, reliability & easy to use API.  
As Unity is selected as the Game engine C# .NET is the way to go.

Another objective is to create an open source software project which may have the potential to be popular.  
As I have 15+ years experience as a software developer in enterprise environment - Shout-Out to [HERE Technologies](https://www.here.com/) -
I decided to avoid a Vendor Lock-In to Unity and target for a solution which fits also the needs of common .NET projects.  
So development is entirely done with .NET Core while checking Unity compatibility on a regular basis.

The result is a project with a feature set useful in common & gaming projects and targeting for optimal performance.  
The common ground of both areas is the need of databases.  
In context of game development the game state (Players, NPC, objects, ...) is represented as an in-memory database
to enable low latency, high throughput and minimal use of system resources.  
In common projects databases are used to store any kind of data persistent by using a popular DBMS.  
Specific for online gaming is the ability to send messages from one client to another in *real time*.
This is enabled by supporting Pub-Sub with sub millisecond latency on *localhost*.

<br/>


## 🙏 Credits
|                                                                           |             |                                                                      |
| ------------------------------------------------------------------------- | ----------- | -------------------------------------------------------------------- |
| [NUnit](https://nunit.org/)                                               | C#          | unit testing of the library in the CLR and Unity                     |
| [FluentAssertions](https://github.com/fluentassertions/fluentassertions)  | C#          | unit testing of the library                                          |
| [SQLitePCL.raw](https://github.com/ericsink/SQLitePCL.raw)                | C#          | used by DB Provider for SQLite                                       |
| [SqlClient](https://github.com/dotnet/SqlClient)                          | C#          | used by DB Provider for Microsoft SQL Server                         |
| [MySqlConnector](https://github.com/mysql-net/MySqlConnector)             | C#          | used by DB Provider for MySQL, MariaDB and MySQL compatible DBMS     |
| [Npgsql](https://github.com/npgsql/npgsql)                                | C#          | used by DB Provider for PostgreSQL                                   |
| [Microsoft.Azure.Cosmos](https://github.com/Azure/azure-cosmos-dotnet-v3) | C#          | used by DB Provider for CosmosDB                                     |
| [SIPSorcery](https://github.com/sipsorcery-org/sipsorcery)                | C#          | WebRTC - Real-time communication between Web clients without server  |
| [GraphQL.NET Parser](https://github.com/graphql-dotnet/parser)            | C#          | used by package: Friflo.Json.Fliox.Hub.GraphQL                       |
| [MdDocs](https://github.com/ap0llo/mddocs)                                | C#          | for [fliox-docs API Reference](https://github.com/friflo/fliox-docs) |
| [.NET platform](https://dotnet.microsoft.com/en-us/)                      | C# .NET     | the platform providing compiler, runtime, IDE's & ASP.NET Core       |
| [Swagger](https://swagger.io/)                                            | static JS   | a REST / OpenAPI UI linked by the Hub Explorer                       |
| [GraphiQL](https://github.com/graphql/graphiql)                           | static JS   | a GraphQL UI linked by the Hub Explorer                              |
| [Mermaid](https://github.com/mermaid-js/mermaid)                          | static JS   | class diagram for database schema linked by the Hub Explorer         |
| [Monaco Editor](https://github.com/microsoft/monaco-editor)               | static JS   | used as JSON editor integrated in the Hub Explorer                   |
| [WinMerge](https://github.com/WinMerge/winmerge)                          | Application | heavily used in this project                                         |
| [Inscape](https://gitlab.com/inkscape/inkscape)                           | Application | to create SVG's for this project                                     |

<br/>

💖 *Like this project?*  
*Leave a* ⭐ at  [friflo/Friflo.Json.Fliox](https://github.com/friflo/Friflo.Json.Fliox)

Happy coding!  

<br/>

## License

This project is licensed under AGPLv3.  
Published project on GitHub 2022-08  

friflo JSON Fliox  
Copyright © 2022   Ullrich Praetz



# ![logo](docs/images/Json-Fliox.svg)     **JSON Fliox**      ![SPLASH](docs/images/paint-splatter.svg)

[![.NET Tests](https://github.com/friflo/Friflo.Json.Fliox/workflows/.NET/badge.svg)](https://github.com/friflo/Friflo.Json.Fliox/actions)


__`SELECT * FROM table1 LEFT JOIN table2 ON 💩 WHERE 💩 💩 💩`__

*Same attitude?* 😉  
*Then you may have a short look at this project*  <br/><br/>


## Description
**JSON Fliox** is **.NET** library supporting **simple** and **efficient** access to **NoSQL** databases via C# or Web clients.  
Its **ORM** enables **Schema** creation. Its **Hub** serve hosted databases with these schemas via HTTP.

|              | Description                                                               | API  |
| ------------ | ------------------------------------------------------------------------- | ---- |
| ORM          | Object Relational Mapper - to access to NoSQL databases with .NET clients | C#   |
| Database Hub | A service hosting a set of NoSQL databases via an **ASP.NET Core** server | HTTP |

As **JSON Fliox** is an [ORM](https://en.wikipedia.org/wiki/Object-relational_mapping) is has similarities to projects like
- [Entity Framework Core](https://en.wikipedia.org/wiki/Entity_Framework) · C#
- [Dapper ORM](https://en.wikipedia.org/wiki/Dapper_ORM) · C#, SQL
- [Ruby on Rails - Active Record Pattern](https://en.wikipedia.org/wiki/Ruby_on_Rails) · Ruby
- [Django](https://en.wikipedia.org/wiki/Django_(web_framework)) · Python
- [Hibernate](https://de.wikipedia.org/wiki/Hibernate_(Framework)) · Java
- [Prisma](https://www.prisma.io/) · Typescript

*Info*: Pronunciation of **io** in **Fliox** is same as in **Riot** <br/><br/>

### **TL;DR**

A demo server running on AWS - [**DemoHub**](http://ec2-174-129-178-18.compute-1.amazonaws.com/) (EC2 instance: t2-micro, us-east-1)

The **DemoHub** C# project and a compact features list available at the
[FlioxHub.Demos](https://github.com/friflo/FlioxHub.Demos#features) repository.

<br/>

## Features

Detailed feature descriptions are explained by a set of `README` files linked below.  
*Topics*: Demos, Client, Host, Hub Explorer, DB - support databases, Protocol, Schema, Mapper & Unit Tests.

<br/>

## Content

- **Fliox Hub**
    - [Demos](#demos)               ❯  [friflo/FlioxHub.Demos](https://github.com/friflo/FlioxHub.Demos/blob/main/README.md)
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



## **Fliox Hub**

### **Demos**

📄   [README.md](https://github.com/friflo/FlioxHub.Demos/blob/main/README.md)


### **Client**
📄   [README.md](Json/Fliox.Hub/Client/README.md)


### **Host**
📄   [README.md](Json/Fliox.Hub/Host/README.md)


### **Explorer**
📄   [README.md](Json/Fliox.Hub.Explorer/README.md)


### **DB**
📄   [README.md](Json/Fliox.Hub/DB/README.md)


### **Protocol**
📄   [README.md](Json/Fliox.Hub/Protocol/README.md)

<br/><br/>



## **Fliox**

### **Schema**
📄   [README.md](Json/Fliox/Schema/README.md)


### **Mapper**
📄   [README.md](Json/Fliox/Mapper/README.md)

<br/><br/>



## **Testing**

### **Unit Tests**
📄   [README.md](Json.Tests/README.md)

<br/><br/>


## **Design**

### **API**

The Fliox **C# .NET** API is [CLS-compliant](https://docs.microsoft.com/en-us/dotnet/api/system.clscompliantattribute)

The API is available at [**JSON Fliox - API Reference**](https://github.com/friflo/fliox-docs)

### **Principles**

- dependencies
    - no 3rd party dependencies
    - small size of Fliox assemblies (*.dll) ~ 850 kb in total, 350 kb zipped
- target for optimal performance
    - maximize throughput, minimize latency, minimize heap allocations and boxing
    - enable task batching aka a unit of work
    - support bulk operations for CRUD commands
- provide compact and strongly typed API
    - type safe access to entities and their keys when dealing with containers  
    - type safe access to DTO's when dealing with database commands
    - absence of using `object` as a type
- serialization of entities and messages - request, response & event - are entirely JSON
- Fliox Clients and Hubs are unit testable without mocking
- extensibility
    - support custom database adapters aka providers
    - support custom code / schema generators for new programming languages
- compatibility
    - **.NET Core 3.1** and higher
    - **Unity 2020.1** and higher 

<br/>

## Credits
- [NUnit](https://nunit.org/)                                   · used for unit testing of the Demos and the JSON Fliox library
- [Swagger](https://swagger.io/)                                · as a REST / OpenAPI UI used by the Hub Explorer
- [GraphiQL](https://github.com/graphql/graphiql)               · as a GraphQL UI used by the Hub Explorer
- [Monaco Editor](https://github.com/microsoft/monaco-editor)   · to edit JSON in the Hub Explorer
- [.NET guys](https://dotnet.microsoft.com/en-us/)              · the platform providing compiler, runtime, IDE's & ASP.NET Core
- [WinMerge](https://github.com/WinMerge/winmerge)              · heavily used in this project
- [Inscape](https://gitlab.com/inkscape/inkscape)               · to create SVG's for this project


Happy coding!  
😊 💻

<br/>

## License

This project is licensed under AGPLv3.

Project not published nor released yet.

friflo JSON Fliox  
Copyright © 2022 Ullrich Praetz

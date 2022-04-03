

# ![logo](../docs/images/Json-Fliox.svg)     **Fliox DemoHub**      ![SPLASH](../docs/images/paint-splatter.svg)



## General

The **Fliox DemoHub** is used to demonstrate the features of
[**JSON Fliox Hub**](https://github.com/friflo/Friflo.Json.Fliox) .NET library via a Web project.  
This library is an **ORM** used to create a **schema** for **NoSQL databases** (key-value or document) by
declaring a set of model classes.  
The classes define the types stored in each database container / table.
*In short* - a **Code First** approach to define a database schema.

For a simple setup the server **is also the database** storing records (entities) **in-memory** or in the **file-system**.  
This enables running the server **without** any configuration or installation of a third party DBMS (database management system).


**TL;DR**  
[**Fliox DemoHub**](http://ec2-174-129-178-18.compute-1.amazonaws.com/) running on **AWS** using **t2-micro** instance


## DemoStore

The key class when running a HTTP server using **JSON Fliox Hub** is [**DemoStore.cs**](DemoStore.cs).  
This class provide two fundamental functionalities:
1. It is a **database client** providing type-safe access to its containers, commands and messages
2. It defines a **database schema** by declaring its containers, commands and messages.  
  The schema is used by host for **record validation** and exposing the schema in various formats:  
  **JSON Schema**, **OpenAPI**, **HTML**, **Typescript**, **C#** & **Kotlin**.


## Features
The main features of a [**HTTP Fliox Hub**](https://github.com/friflo/Friflo.Json.Fliox/blob/main/Json/Fliox.Hub/Host/README.md#httphosthub) are:
- provide **JSON based Web API**s to access **key-value** or **document** databases.
- assign a **database schema** to each databases as they typically don't have a build-in solution for schemas
- aim for near optimal request performance regarding **low latency** and **high throughput**
- enable simple and efficient TDD as database access can be tested with **in-memory** or **file-system** based databases.
- host a single-page Web App to browse database containers / tables and execute domain specific commands.
  See [**Hub Explorer**](https://github.com/friflo/Friflo.Json.Fliox/blob/main/Json/Fliox.Hub.Explorer/README.md)
- expose an [**OpenAPI interface**](https://spec.openapis.org/oas/v3.0.0) describing the **REST API** and
  host [**Swagger UI**](https://swagger.io/tools/swagger-ui/) to explore the API
  

## Files

|                      file                     |                   description                             
|-----------------------------------------------|-----------------------------------------------
|[DemoStore.cs](DemoStore.cs)                   | is the database client <br/> declares database containers
|[DemoStore_commands.cs](DemoStore_commands.cs) | declares database commands
|[MessageHandler.cs](MessageHandler.cs)         | implement custom database commands by utilizing **DemoStore** clients
|[Program.cs](Program.cs)                       | bootstrapping & configuration of host
|[Startup.cs](Startup.cs)                       | **ASP.NET Core** configuration and host integration
|[Utils.cs](Utils.cs)                           | utilize [Bogus](https://github.com/bchavez/Bogus) to generate fake records







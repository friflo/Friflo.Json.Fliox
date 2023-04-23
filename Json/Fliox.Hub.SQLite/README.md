# [![JSON Fliox](https://raw.githubusercontent.com/friflo/Friflo.Json.Fliox/main/docs/images/Json-Fliox.svg)](https://github.com/friflo/Friflo.Json.Fliox)    **Friflo.Json.Fliox.Hub.SQLite** ![splash](https://raw.githubusercontent.com/friflo/Friflo.Json.Fliox/main/docs/images/paint-splatter.svg)

## Package

This package is part of the project described below.

**Content**  
Contains the database provider for [SQLite](https://sqlite.org/)


## Unity

* Add **`SQLITE`** to  
  **Unity > Edit > Player | Other Settings | Script Compilation | Scripting Define Symbols**

* Add SQLite dependencies to Plugins folder:

|  dependency                           | nuget                                                             |
|-------------------------------------- | ----------------------------------------------------------------- |
| `SQLitePCLRaw.core.dll`               | https://www.nuget.org/packages/SQLitePCLRaw.core                  |
| `SQLitePCLRaw.provider.e_sqlite3.dll` | https://www.nuget.org/packages/SQLitePCLRaw.provider.e_sqlite3    |
| `e_sqlite3.dll`                       | https://www.nuget.org/packages/SQLitePCLRaw.lib.e_sqlite3         |

## Project

**JSON Fliox** is a **.NET** library supporting **simple** and **efficient** access to **NoSQL** databases via C# or Web clients.  
Its **ORM** enables **Schema** creation. Its **Hub** serve hosted databases using these schemas via HTTP.

The **ORM** client - Object Relational Mapper - is used to access NoSQL databases via .NET.  
The **Hub** is a service hosting a set of NoSQL databases via an **ASP.NET Core** server.


## Links

- [Homepage](https://github.com/friflo/Friflo.Json.Fliox)
- [NuGet Package](https://www.nuget.org/packages/Friflo.Json.Fliox.Hub.Cosmos)
- [License](https://github.com/friflo/Friflo.Json.Fliox/blob/main/LICENSE)
- [Stack Overflow](https://stackoverflow.com/questions/tagged/fliox)
# [![JSON Fliox](https://raw.githubusercontent.com/friflo/Friflo.Json.Fliox/main/docs/images/Json-Fliox.svg)](https://github.com/friflo/Friflo.Json.Fliox)    **Friflo.Json.Fliox.Hub.SQLite** ![splash](https://raw.githubusercontent.com/friflo/Friflo.Json.Fliox/main/docs/images/paint-splatter.svg)

## Package

This package is part of the project described below.

**Content**  
Contains the database provider for [SQLite](https://sqlite.org/)

This package depends on the nuget packages of **SQLitePCLRaw**.  
[![nuget](https://img.shields.io/nuget/v/SQLitePCLRaw.core.svg?label=SQLitePCLRaw.core&color=blue)](https://www.nuget.org/packages/SQLitePCLRaw.core)  
[![nuget](https://img.shields.io/nuget/v/SQLitePCLRaw.provider.e_sqlite3.svg?label=SQLitePCLRaw.provider.e_sqlite3&color=blue)](https://www.nuget.org/packages/SQLitePCLRaw.provider.e_sqlite3)  
[![nuget](https://img.shields.io/nuget/v/SQLitePCLRaw.lib.e_sqlite3.svg?label=SQLitePCLRaw.lib.e_sqlite3&color=blue)](https://www.nuget.org/packages/SQLitePCLRaw.lib.e_sqlite3)  


The **connection string** parameters used in SQLiteDatabase() are:

| Parameter     | Description                                                  |
| ------------- | ------------------------------------------------------------ |
| `Data Source` | Path to SQLite file. Use `:memory:` to use in-memory storage |

Examples:  
`"Data Source=test_db.sqlite3"`  
`"Data Source=:memory:"`

The class `SQLiteConnectionStringBuilder` can be used to parse or create a connection string.

<br/>

## Unity

* Add **`SQLITE`** to  
  **Unity > Edit > Player | Other Settings | Script Compilation | Scripting Define Symbols**

* Add the **SQLitePCLRaw** dependencies listed above to Plugins folder:

<br/>

## Project

**JSON Fliox** is a .NET library supporting simple and performant access to SQL & NoSQL databases via .NET or Web clients. 

<br/>

## Links

- [Homepage](https://github.com/friflo/Friflo.Json.Fliox)
- [NuGet Package](https://www.nuget.org/packages/Friflo.Json.Fliox.Hub.Cosmos)
- [License](https://github.com/friflo/Friflo.Json.Fliox/blob/main/LICENSE)
- [Stack Overflow](https://stackoverflow.com/questions/tagged/fliox)
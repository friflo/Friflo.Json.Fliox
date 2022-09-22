

# [![JSON Fliox](../docs/images/Json-Fliox.svg)](https://github.com/friflo/Friflo.Json.Fliox)     **Json.Tests** ![SPLASH](../docs/images/paint-splatter.svg)

[![CI](https://github.com/friflo/Friflo.Json.Fliox/workflows/CI/badge.svg)](https://github.com/friflo/Friflo.Json.Fliox/actions/workflows/dotnet.yml) 

namespace **`Friflo.Json.Tests`**

The project contains a Console application and Unit tests.  
Execution of both is described below.  

The solution and its projects can be build, tested and executed on **Windows**, **Linux**, **macOS** and **Unity**.  
It can be used with the IDE's: **VSCode**, **Rider** & **Visual Studio 2022**.

*Note*: In order to build and run the examples the [**.NET 6.0 SDK**](https://dotnet.microsoft.com/en-us/download) is required.

Or use **Gitpod** to build and run the server using a browser without installing anything.  Workspace available in 30 sec.  
<a href="https://gitpod.io/#https://github.com/friflo/Friflo.Json.Fliox">
  <img
    src="https://img.shields.io/badge/Build%20with-Gitpod-908a85?logo=gitpod"
    alt="Build with Gitpod"
  />
</a>

clone the repository and open its directory - leave out this step when using Gitpod.
```cmd
git clone https://github.com/friflo/Friflo.Json.Fliox.git
cd Friflo.Json.Fliox
```

build the library, the unit tests and the console application with
```cmd
dotnet build
```

<br/><br/>

# 💻 Console application

The Console application is used:

- to run a Test server
```
cd Json.Tests
dotnet run --module TestServer -c Release
```
- to run performance checks.  
  The performance checks are used to measure the throughput of `SyncTasks()` calls in various scenarios.  
  - remote HTTP vs WebSocket vs Loopback vs in-process 
  - file-system vs in-memory database
  - number of concurrent clients. Default: 4

each run show a representative sample [requests / sec]
```
dotnet run --module MemoryDbThroughput    -c Release
⏩ requests: 680796 / sec  # CPU-bound

dotnet run --module FileDbThroughput      -c Release
⏩ requests: 6251 / sec    # disc-bound

dotnet run --module WebsocketDbThroughput -c Release
⏩ requests: 27221 / sec   # network-bound

dotnet run --module HttpDbThroughput      -c Release
⏩ requests: 12753 / sec   # network-bound

dotnet run --module LoopbackDbThroughput  -c Release
⏩ requests: 172433 / sec  # CPU-bound
```

<br/><br/>

# 🧪 Unit tests

The current result of the unit test are available as **CI tests** at
[Github actions](https://github.com/friflo/Friflo.Json.Fliox/actions).

The project is using [NUnit](https://nunit.org/) for unit testing. Execute them locally by running:
```
cd Json.Tests
dotnet test -c Release -l "console;verbosity=detailed"
```
Unit test execution will finish in about 7 seconds.

<br/>

The unit tests can be executed in the **Test Explorer** of **VSCode**, **Rider** & **Visual Studio 2022**.  
For VSCode [.NET Core Test Explorer](https://marketplace.visualstudio.com/items?itemName=formulahendry.dotnet-test-explorer) can be used.

By using **NUnit** the unit tests can be executed in the Unity **Test Runner** in the [Unity Editor](https://unity.com/)
(Window > General > Test Runner) as **EditMode** tests.

<br/>

**Memory assertions**

Additional to common unit testing of expected behavior, some tests also check heap allocations with assertions:
- **No (0) allocations** occur on the heap while running a parser or serializer a couple of times.
- **No leaks of `native containers`** are left over after tear down a unit test.  
  This is relevant only when using the library in Unity compiled with **JSON_BURST** - it is not relevant when running in CLR

<br/><br/>

# 🕹️ Unity

clone the repository in the `Assets` folder
```
cd Assets
git clone https://github.com/friflo/Friflo.Json.Fliox.git
```

## Build Settings
Tested with: Unit Menu > Edit > Project Settings ... > Player > Other Settings

|  Unity version  |  Scripting Backend  |  API Compatibility Level  |
| --------------- | ------------------- | ------------------------- |
|  2020.1.15f1    |  Mono               | .NET 4.x                  |
|                 |  IL2CPP             | .NET 4.x                  |
|  2021.3.9f1     |  Mono               | .NET Standard 2.1         |
|                 |  IL2CPP             | .NET Standard 2.1         |


Run the unit tests in the **Test Runner** in the [Unity Editor](https://unity.com/)
(Window > General > Test Runner) as **EditMode** tests.

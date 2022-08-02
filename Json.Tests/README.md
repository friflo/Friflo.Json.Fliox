

# [![JSON Fliox](../docs/images/Json-Fliox.svg)](https://github.com/friflo/Friflo.Json.Fliox)     **Json.Tests** ![SPLASH](../docs/images/paint-splatter.svg)

[![.NET Tests](https://github.com/friflo/Friflo.Json.Fliox/workflows/.NET/badge.svg)](https://github.com/friflo/Friflo.Json.Fliox/actions)

namespace **`Friflo.Json.Tests`**

The project contains a Console application and Unit tests.  
Execution of both is described below.  

The solution and its projects can be build, tested and executed on **Windows**, **Linux**, and **macOS**.  
It can be used with the IDE's: **VSCode**, **Rider** & **Visual Studio 2022**.

*Note*: In order to build and run the examples the [**.NET 6.0 SDK**](https://dotnet.microsoft.com/en-us/download) is required.

clone the repository and open its directory
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
⏩ requests: 680796 / sec

dotnet run --module FileDbThroughput      -c Release
⏩ requests: 6251 / sec

dotnet run --module WebsocketDbThroughput -c Release
⏩ requests: 27221 / sec

dotnet run --module HttpDbThroughput      -c Release
⏩ requests: 12753 / sec

dotnet run --module LoopbackDbThroughput  -c Release
⏩ requests: 172433 / sec
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


## NUnit
The unit tests can be executed in the **Test Explorer** of **VSCode**, **Rider** & **Visual Studio 2022**.  
For VSCode [.NET Core Test Explorer](https://marketplace.visualstudio.com/items?itemName=formulahendry.dotnet-test-explorer) can be used.

By using NUnit the unit tests can be executed in the Unity **Test Runner** in the [Unity Editor](https://unity.com/)
(Window > General > Test Runner) as `EditMode` tests.

## Memory assertions
Additional to common unit testing of expected behavior, some tests also check heap allocations with assertions:
- **No (0) allocations** occur on the heap while running a parser or serializer a couple of times.
- **No leaks of `native containers`** are left over after tear down a unit test.  
  This is relevant only when using the library in Unity compiled with **JSON_BURST** - it is not relevant when running in CLR


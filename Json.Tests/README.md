

# [![JSON Fliox](../docs/images/Json-Fliox.svg)](https://github.com/friflo/Friflo.Json.Fliox)     **Json.Tests** ![SPLASH](../docs/images/paint-splatter.svg)

[![.NET Tests](https://github.com/friflo/Friflo.Json.Fliox/workflows/.NET/badge.svg)](https://github.com/friflo/Friflo.Json.Fliox/actions)

namespace **`Friflo.Json.Tests`**

<br/>

# Console application

The project contains unit tests and a Console application used
- to run a Test server
```
dotnet run --module TestServer -c Release --no-build
```
- to run performance checks.  
  The performance checks are used to measure the throughput of `SyncTasks()` calls in various scenarios.  
  - remote (HTTP / WebSocket) vs in-process 
  - file-system vs in-memory database
  - number of concurrent clients
```
dotnet run --module MemoryDbThroughput    -c Release --no-build
dotnet run --module FileDbThroughput      -c Release --no-build
dotnet run --module WebsocketDbThroughput -c Release --no-build
dotnet run --module HttpDbThroughput      -c Release --no-build
dotnet run --module LoopbackDbThroughput  -c Release --no-build
```

<br/><br/>

# Unit tests

The current result of the unit test are available as **CI tests** at
[Github actions](https://github.com/friflo/Friflo.Json.Fliox/actions).

The project is using [NUnit](https://nunit.org/) for unit testing. Execute them locally by running:
```
dotnet test -c Release -l "console;verbosity=detailed"
```
The unit tests can be executed also within various IDEs. [Visual Studio](https://visualstudio.microsoft.com/),
[Rider](https://www.jetbrains.com/rider/) and [Visual Studio Code](https://visualstudio.microsoft.com/).

By using NUnit the unit tests can be executed via the Test Runner in the [Unity Editor](https://unity.com/)
(Window > General > Test Runner) as `EditMode` tests.

Additional to common unit testing of expected behavior, the test also ensure the following principles
with additional assertions:
- **No (0) allocations** occur on the heap while running a parser or serializer a couple of times.
- **No leaks of `native containers`** are left over after tear down a unit test.  
  This is relevant only when using the library in Unity compiled with **JSON_BURST** - it is not relevant when running in CLR


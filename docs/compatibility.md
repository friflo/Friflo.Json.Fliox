
# **Compatibility**

# .NET Standard 2.0

Current target framework configuration

```xml
<TargetFrameworks>netcoreapp3.1;netstandard2.0;netstandard2.1</TargetFrameworks>
```

Goal is to maintain compatibility to [.NET Standard 2.0](https://learn.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-0)
to support **.NET Framework 4.6.1** or higher

This requires conditional compilation depending on `NETSTANDARD2_0` and `NETSTANDARD2_1`.  
**CLR**   preprocessor symbols [Target frameworks in SDK-style projects](https://learn.microsoft.com/en-us/dotnet/standard/frameworks)  
**Unity** preprocessor symbols [Unity Manual - Conditional Compilation](https://docs.unity3d.com/Manual/PlatformDependentCompilation.html)

Mainly all all affected code is related to performance focused code using `Span<>`'s or `ReadOnlySpan<>`'s.

Majority of `NETSTANDARD2_0` implementation is in [Standard-Extensions.cs](https://github.com/friflo/Friflo.Json.Fliox/blob/main/Json/Burst/Standard-Extensions.cs)


## BCL dependencies

Following nuget packages of BCL - Base Class Library - are required for TargetFrameworks:
<b>netstandard2.0</b> and <b>netstandard2.1</b>

[System.Memory](https://www.nuget.org/packages/System.Memory)

[System.ComponentModel.Annotations](https://www.nuget.org/packages/System.ComponentModel.Annotations)

[System.Threading.Channels](https://www.nuget.org/packages/System.Threading.Channels)

Some implementation specific for <b>netstandard2.0</b> require heap allocations.  
These implementation are marked with
```c#
// NETSTANDARD2_0_ALLOC
```
## 3rd Party dependencies

[GraphQL-Parser](https://github.com/graphql-dotnet/parser) -
[nuget](https://www.nuget.org/packages/SIPSorcery/)
used by [Fliox GraphQL](https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox.Hub.GraphQL)
uses:

```xml
<TargetFrameworks>netstandard2.0;netstandard2.1;net6</TargetFrameworks>
```

[SIPSorcery](https://github.com/sipsorcery-org/sipsorcery) -
[nuget](https://www.nuget.org/packages/GraphQL-Parser)
used by [Fliox WebRTC](https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox.Hub.WebRTC)
uses:

```xml
<TargetFrameworks>netstandard2.0;netstandard2.1;netcoreapp3.1;net461;net5.0;net6.0;</TargetFrameworks>
```


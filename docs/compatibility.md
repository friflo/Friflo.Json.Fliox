
# **Compatibility**

# .NET Standard 2.0

Current target framework configuration

```xml
<TargetFrameworks>netcoreapp3.1;netstandard2.0;netstandard2.1</TargetFrameworks>
```

Goal is to maintain compatibility to [.NET Standard 2.0](https://learn.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-0)
to support **.NET Framework 4.6.1** or higher

This requires conditional compilation depending on `NETSTANDARD2_0` and `NETSTANDARD2_1`.  
See preprocessor symbols at [Target frameworks in SDK-style projects](https://learn.microsoft.com/en-us/dotnet/standard/frameworks)

Mainly all all affected code is related to performance focused code using `ReadOnlySpan<>`'s


## BCL dependencies

[System.Memory](https://www.nuget.org/packages/System.Memory)

[System.ComponentModel.Annotations](https://www.nuget.org/packages/System.ComponentModel.Annotations)



## 3rd Party dependencies

[GraphQL-Parser](https://github.com/graphql-dotnet/parser) at
[nuget](https://www.nuget.org/packages/SIPSorcery/) uses:

```xml
<TargetFrameworks>netstandard2.0;netstandard2.1;net6</TargetFrameworks>
```

[SIPSorcery](https://github.com/sipsorcery-org/sipsorcery) at
[nuget](https://www.nuget.org/packages/GraphQL-Parser)
uses:
```xml
<TargetFrameworks>netstandard2.0;netstandard2.1;netcoreapp3.1;net461;net5.0;net6.0;</TargetFrameworks>
```


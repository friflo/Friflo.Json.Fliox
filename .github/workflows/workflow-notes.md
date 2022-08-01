

# 2022-07-16  Build error

`error MSB4018: The "GenerateDepsFile" task failed unexpectedly. ...`

https://github.com/friflo/Friflo.Json.Fliox/runs/7384710919

```
...
/home/runner/.dotnet/sdk/3.1.417/Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.Sdk.targets(194,5): error MSB4018: The "GenerateDepsFile" task failed unexpectedly. [/home/runner/work/Friflo.Json.Fliox/Friflo.Json.Fliox/Demos~/Todo/Hub/TodoHub.csproj]
/home/runner/.dotnet/sdk/3.1.417/Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.Sdk.targets(194,5): error MSB4018: System.IO.IOException: The process cannot access the file '/home/runner/work/Friflo.Json.Fliox/Friflo.Json.Fliox/Demos~/Todo/Hub/bin/Debug/netcoreapp3.1/TodoHub.deps.json' because it is being used by another process. [/home/runner/work/Friflo.Json.Fliox/Friflo.Json.Fliox/Demos~/Todo/Hub/TodoHub.csproj]
...
Build FAILED.
```

Similar to:
[GenerateDepsFile: The process cannot access the file '...\MyProject.deps.json' because it is being used by another process.](https://github.com/dotnet/sdk/issues/2902#issuecomment-460742123)

## Solution

- [Github Actions - analyse: GenerateDepsFile error](https://github.com/friflo/Fliox.Examples/commit/1a6fefc26a1b5d60c43a1f9eb7c389fc0e46dfed)

remove `--framework netcoreapp3.1`  
in yml ->
```
    - name: Build
      run: |
        dotnet build --no-restore
```

Info: Observe further builds to check if solution fixes this Build error.




# 2022-07-18  Build warnings (10)

`CSC : warning CS8032: An instance of analyzer Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator cannot be created ...`
https://github.com/friflo/Friflo.Json.Fliox/runs/7387439120

```
...
CSC : warning CS8032: An instance of analyzer Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator cannot be created from /home/runner/.nuget/packages/microsoft.extensions.logging.abstractions/6.0.0/analyzers/dotnet/roslyn3.11/cs/Microsoft.Extensions.Logging.Generators.dll : Could not load file or assembly 'Microsoft.CodeAnalysis, Version=3.11.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'. The system cannot find the file specified.. [/home/runner/work/Friflo.Json.Fliox/Friflo.Json.Fliox/Json/Fliox.Hub.AspNetCore/Friflo.Json.Fliox.Hub.AspNetCore.csproj]
...
Build FAILED.
```



## Tries without success

- [sourcegenerators - C# Source Generator cannot be created - Stack Overflow](https://stackoverflow.com/questions/68273070/c-sharp-source-generator-cannot-be-created)
- [Source Generator cannot be created · Issue #54710 · dotnet/roslyn](https://github.com/dotnet/roslyn/issues/54710#issuecomment-879258612)

Set dependency specific version for `Microsoft.CodeAnalysis` in `Directory.Build.props`
```
    <!-- see Build warning documented at: ./.github/workflows/workflow-notes.md -->
    <PackageReference Include="Microsoft.CodeAnalysis" Version="3.9.0"  PrivateAssets="All" />
```

## Solution

Finally warning disappeared by upgrading to `dotnet-version: 6.0.x` in `dotnet.yml`


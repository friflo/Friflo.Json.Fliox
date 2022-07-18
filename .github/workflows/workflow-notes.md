

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

[Github Actions - analyse: GenerateDepsFile error](https://github.com/friflo/FlioxHub.Demos/commit/1a6fefc26a1b5d60c43a1f9eb7c389fc0e46dfed)

remove `--framework netcoreapp3.1`  
in yml ->
```
    - name: Build
      run: |
        dotnet build --no-restore
```

Info: Observe further builds to check if solution fixes this Build error.
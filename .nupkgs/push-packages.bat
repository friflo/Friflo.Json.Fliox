
rem dotnet pack -p:PackageVersion=0.2.2 /p:Version=0.2.2 /p:FileVersion=0.2.2 /p:AssemblyVersion=0.2.2 --output .nupkgs -c Release -p:IncludeSymbols=true -p:IncludeSymbols=true
rem cd .nupkgs
rem push-packages.bat
rem [IncludeSource and --include-source are ignored if SymbolPackageFormat is set to snupkg · Issue #8589 · NuGet/Home] https://github.com/NuGet/Home/issues/8589

dotnet nuget push Friflo.Json.Burst.0.2.0.nupkg                 --source https://api.nuget.org/v3/index.json -k %PUSH_JSON_FLIOX%
dotnet nuget push Friflo.Json.Fliox.0.2.0.nupkg                 --source https://api.nuget.org/v3/index.json -k %PUSH_JSON_FLIOX%
dotnet nuget push Friflo.Json.Fliox.Annotation.0.2.0.nupkg      --source https://api.nuget.org/v3/index.json -k %PUSH_JSON_FLIOX%
dotnet nuget push Friflo.Json.Fliox.Hub.0.2.0.nupkg             --source https://api.nuget.org/v3/index.json -k %PUSH_JSON_FLIOX%
dotnet nuget push Friflo.Json.Fliox.Hub.AspNetCore.0.2.0.nupkg  --source https://api.nuget.org/v3/index.json -k %PUSH_JSON_FLIOX%
dotnet nuget push Friflo.Json.Fliox.Hub.Cosmos.0.2.0.nupkg      --source https://api.nuget.org/v3/index.json -k %PUSH_JSON_FLIOX%
dotnet nuget push Friflo.Json.Fliox.Hub.Explorer.0.2.0.nupkg    --source https://api.nuget.org/v3/index.json -k %PUSH_JSON_FLIOX%
dotnet nuget push Friflo.Json.Fliox.Hub.GraphQL.0.2.0.nupkg     --source https://api.nuget.org/v3/index.json -k %PUSH_JSON_FLIOX%

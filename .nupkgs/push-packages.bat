
rem batch file is obsolete
rem packages are now created by creating a git tag which starts the CD pipeline:
rem   https://github.com/friflo/Friflo.Json.Fliox/actions/workflows/nuget.yml

rem dotnet pack -p:PackageVersion=0.2.4 /p:Version=0.2.4 /p:FileVersion=0.2.4 /p:AssemblyVersion=0.2.4 --output .nupkgs -c Release -p:SymbolPackageFormat=snupkg
rem cd .nupkgs
rem push-packages.bat

dotnet nuget push Friflo.Json.Burst.0.2.0.nupkg                 --source https://api.nuget.org/v3/index.json -k %PUSH_JSON_FLIOX%
dotnet nuget push Friflo.Json.Fliox.0.2.0.nupkg                 --source https://api.nuget.org/v3/index.json -k %PUSH_JSON_FLIOX%
dotnet nuget push Friflo.Json.Fliox.Annotation.0.2.0.nupkg      --source https://api.nuget.org/v3/index.json -k %PUSH_JSON_FLIOX%
dotnet nuget push Friflo.Json.Fliox.Hub.0.2.0.nupkg             --source https://api.nuget.org/v3/index.json -k %PUSH_JSON_FLIOX%
dotnet nuget push Friflo.Json.Fliox.Hub.AspNetCore.0.2.0.nupkg  --source https://api.nuget.org/v3/index.json -k %PUSH_JSON_FLIOX%
dotnet nuget push Friflo.Json.Fliox.Hub.Cosmos.0.2.0.nupkg      --source https://api.nuget.org/v3/index.json -k %PUSH_JSON_FLIOX%
dotnet nuget push Friflo.Json.Fliox.Hub.Explorer.0.2.0.nupkg    --source https://api.nuget.org/v3/index.json -k %PUSH_JSON_FLIOX%
dotnet nuget push Friflo.Json.Fliox.Hub.GraphQL.0.2.0.nupkg     --source https://api.nuget.org/v3/index.json -k %PUSH_JSON_FLIOX%

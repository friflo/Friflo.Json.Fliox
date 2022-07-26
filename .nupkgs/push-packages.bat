
rem dotnet pack -p:PackageVersion=0.0.16 /p:Version=0.0.16 /p:FileVersion=0.0.16 /p:AssemblyVersion=0.0.16 --output .nupkgs -c Release
rem cd .nupkgs
rem push-packages.bat

dotnet nuget push Friflo.Json.Burst.0.0.16.nupkg                 --source "github"
dotnet nuget push Friflo.Json.Fliox.0.0.16.nupkg                 --source "github"
dotnet nuget push Friflo.Json.Fliox.Annotation.0.0.16.nupkg      --source "github"
dotnet nuget push Friflo.Json.Fliox.Hub.0.0.16.nupkg             --source "github"
dotnet nuget push Friflo.Json.Fliox.Hub.AspNetCore.0.0.16.nupkg  --source "github"
dotnet nuget push Friflo.Json.Fliox.Hub.Cosmos.0.0.16.nupkg      --source "github"
dotnet nuget push Friflo.Json.Fliox.Hub.Explorer.0.0.16.nupkg    --source "github"
dotnet nuget push Friflo.Json.Fliox.Hub.GraphQL.0.0.16.nupkg     --source "github"

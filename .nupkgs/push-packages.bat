
rem dotnet pack -p:PackageVersion=0.1.2 /p:Version=0.1.2 /p:FileVersion=0.1.2 /p:AssemblyVersion=0.1.2 --output .nupkgs -c Release
rem cd .nupkgs
rem push-packages.bat

dotnet nuget push Friflo.Json.Burst.0.1.2.nupkg                 --source "github"
dotnet nuget push Friflo.Json.Fliox.0.1.2.nupkg                 --source "github"
dotnet nuget push Friflo.Json.Fliox.Annotation.0.1.2.nupkg      --source "github"
dotnet nuget push Friflo.Json.Fliox.Hub.0.1.2.nupkg             --source "github"
dotnet nuget push Friflo.Json.Fliox.Hub.AspNetCore.0.1.2.nupkg  --source "github"
dotnet nuget push Friflo.Json.Fliox.Hub.Cosmos.0.1.2.nupkg      --source "github"
dotnet nuget push Friflo.Json.Fliox.Hub.Explorer.0.1.2.nupkg    --source "github"
dotnet nuget push Friflo.Json.Fliox.Hub.GraphQL.0.1.2.nupkg     --source "github"

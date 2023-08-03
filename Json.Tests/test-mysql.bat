dotnet build -c Release -f net6.0 --verbosity:quiet


dotnet test -c Release -f net6.0 --consoleloggerparameters:ErrorsOnly --filter "FullyQualifiedName=Friflo.Json.Tests.Provider.Perf.TestPerf.Perf_Read_One" --no-build -e TEST_DB_PROVIDER=mysql_rel
dotnet test -c Release -f net6.0 --consoleloggerparameters:ErrorsOnly --filter "FullyQualifiedName=Friflo.Json.Tests.Provider.Perf.TestPerf.Perf_Read_One" --no-build -e TEST_DB_PROVIDER=mysql


dotnet test -c Release -f net6.0 --consoleloggerparameters:ErrorsOnly --filter "FullyQualifiedName=Friflo.Json.Tests.Provider.Perf.TestPerf.Perf_Read_One" --no-build -e TEST_DB_PROVIDER=mariadb_rel
dotnet test -c Release -f net6.0 --consoleloggerparameters:ErrorsOnly --filter "FullyQualifiedName=Friflo.Json.Tests.Provider.Perf.TestPerf.Perf_Read_One" --no-build -e TEST_DB_PROVIDER=mariadb


pause

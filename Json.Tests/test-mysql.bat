dotnet build -c Release -f net8.0 --verbosity:quiet


dotnet test -c Release --consoleloggerparameters:ErrorsOnly --filter TestCategory=test_db --no-build -e TEST_DB_PROVIDER=mysql
dotnet test -c Release --consoleloggerparameters:ErrorsOnly --filter TestCategory=test_db --no-build -e TEST_DB_PROVIDER=mysql_rel

dotnet test -c Release --consoleloggerparameters:ErrorsOnly --filter TestCategory=test_db --no-build -e TEST_DB_PROVIDER=mariadb
dotnet test -c Release --consoleloggerparameters:ErrorsOnly --filter TestCategory=test_db --no-build -e TEST_DB_PROVIDER=mariadb_rel

pause

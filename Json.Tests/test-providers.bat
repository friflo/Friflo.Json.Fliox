
dotnet build -c Release --verbosity:quiet

dotnet test -c Release --consoleloggerparameters:ErrorsOnly --filter TestCategory=test_db --no-build --environment TEST_DB_PROVIDER=file
dotnet test -c Release --consoleloggerparameters:ErrorsOnly --filter TestCategory=test_db --no-build --environment TEST_DB_PROVIDER=sqlite
dotnet test -c Release --consoleloggerparameters:ErrorsOnly --filter TestCategory=test_db --no-build --environment TEST_DB_PROVIDER=postgres
dotnet test -c Release --consoleloggerparameters:ErrorsOnly --filter TestCategory=test_db --no-build --environment TEST_DB_PROVIDER=sqlserver
dotnet test -c Release --consoleloggerparameters:ErrorsOnly --filter TestCategory=test_db --no-build --environment TEST_DB_PROVIDER=mysql
dotnet test -c Release --consoleloggerparameters:ErrorsOnly --filter TestCategory=test_db --no-build --environment TEST_DB_PROVIDER=mariadb
dotnet test -c Release --consoleloggerparameters:ErrorsOnly --filter TestCategory=test_db --no-build --environment TEST_DB_PROVIDER=cosmos

pause
dotnet build -c Release -f net8.0 --verbosity:quiet


dotnet test -c Release --consoleloggerparameters:ErrorsOnly --filter TestCategory=test_db --no-build -e TEST_DB_PROVIDER=sqlserver
dotnet test -c Release --consoleloggerparameters:ErrorsOnly --filter TestCategory=test_db --no-build -e TEST_DB_PROVIDER=sqlserver_rel

pause

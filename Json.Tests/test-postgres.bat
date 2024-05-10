dotnet build -c Release -f net8.0 --verbosity:quiet


dotnet test -c Release --consoleloggerparameters:ErrorsOnly --filter TestCategory=test_db --no-build -e TEST_DB_PROVIDER=postgres
dotnet test -c Release --consoleloggerparameters:ErrorsOnly --filter TestCategory=test_db --no-build -e TEST_DB_PROVIDER=postgres_rel

pause

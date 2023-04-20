
# Provider Tests

Unit tests in this folder / namespace are intended to validate specific database implementations aka Providers.

The reference implementation for all unit tests is a `MemoryDatabase`.


Every test method is attributed with two `[TestCase]` attributes to enabled comparing
reference behavior with a specific database implementation.

```csharp
[TestCase(memory_db, Category = memory_db)]
[TestCase(test_db,   Category = test_db)]
public static async Task TestDatabaseBehavior(string db) {
    ...
}
```

# Test structure

The unit test structure aims to support a complete implementation for a specific database.  
The intended order of a database provider:

1. Create / Open a database
2. Create new table / container in the database
3. Upsert entities to a container
4. Query container entities without access to entities fields. Pure query operator tests.
5. Query container entities including access to entities fields. Test query operators on data.
6. Delete entities


# Test environment

By default - environment variable `TEST_DB` not present - a `FileDatabase` is used for methods attributed with
```csharp
[TestCase(test_db, Category = test_db)]
```

To run tests using a specific database implementation set the environment variable `TEST_DB`. E.g.  
`TEST_DB=cosmos`.

## Rider
Run unit tests for a specific database implementation add an entry at  
**File | Settings | Build, Execution, Deployment | Unit Testing | Test Runner**  
- Environment Variables
    - TEST_DB=cosmos

## Unit test
```
dotnet test -c Release -l "console;verbosity=detailed" --filter TestCategory=test_db --environment TEST_DB=cosmos
```
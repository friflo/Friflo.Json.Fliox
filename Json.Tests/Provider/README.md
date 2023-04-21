
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
The intended order to implement a database provider:

| database command                                                                              | unit test class                               |
|-----------------------------------------------------------------------------------------------|---------------------------------------------- |
| 1. Create / open database                                                                     |                                               |
| 2. Create table / container in the database                                                   |                                               |
| 3. Upsert entities to a container                                                             |                                               |
| 4. Read entities from a container.                                                            | [TestRead](Test/TestRead.cs)                  |
| 5. Query container entities without access to entities fields. Pure query operator tests.     | [TestQueryOps](Test/TestQueryOps.cs)          |
| 6. Query container entities including access to entities fields. Test query operators on data.| [TestQueryFields](Test/TestQueryFields.cs)    |
| 7. Delete container entities                                                                  |                                               |


# Test environment

By default - environment variable `TEST_DB_PROVIDER` not present - a `FileDatabase` is used for methods attributed with
```csharp
[TestCase(test_db, Category = test_db)]
```

To run tests using a specific database implementation set the environment variable `TEST_DB_PROVIDER`. E.g.  
`TEST_DB_PROVIDER=cosmos`.

## Rider
Run unit tests for a specific database implementation add an entry at  
**File | Settings | Build, Execution, Deployment | Unit Testing | Test Runner**  
- Environment Variables
    - TEST_DB_PROVIDER=cosmos

## Unit test
```
dotnet test -c Release -l "console;verbosity=detailed" --filter TestCategory=test_db --environment TEST_DB_PROVIDER=cosmos
```
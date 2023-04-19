
# Tests DB

Unit tests in this folder / namespace are intended to validate specific database implementations.

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

## Test environment

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
## Console

```
dotnet test -c Release -l "console;verbosity=detailed" --filter TestCategory=test_db --environment TEST_DB=cosmos
```
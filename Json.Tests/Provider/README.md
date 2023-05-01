
# Provider Tests

Unit tests in this folder / namespace are intended to implement and validate specific database providers.

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

The unit test structure aims to guide implementation and validation of full functional provider for a specific database.  
The intended order to implement a database provider:

| database command                                                                              | unit test class                                   |
|-----------------------------------------------------------------------------------------------|-------------------------------------------------- |
|    Create / open database                                                                     |                                                   |
|    Create table / container in the database                                                   |                                                   |
| 1. Mutation > Upsert, Create and Delete and entities in a container                           | [Test_1_Mutation](Test/Test_1_Mutation.cs)        |
| 2. Read entities from a container.                                                            | [Test_2_Read](Test/Test_2_Read.cs)                |
| 3. Query container entities without access to entities fields. Pure query operator tests.     | [Test_3_QueryOps](Test/Test_3_QueryOps.cs)        |
| 4. Query container entities including access to entities fields. Test query operators on data.| [Test_4_QueryFields](Test/Test_4_QueryFields.cs)  |
| 5. Query cursor                                                                               | [Test_5_QueryCursor](Test/Test_5_QueryCursor.cs)  |
| 6. Aggregate entities > Count                                                                 | [Test_6_Aggregate](Test/Test_6_Aggregate.cs)      |


## Query filter implementation

In case of query filter a provider has two implementation options:

1. Implement a filter conversion method which converts the given `FilterOperation` into a database specific filter.  
   This is typically a filter predicate used in a `WHERE` clause.  
   This is recommended as the database has the opportunity to utilize in table indices.

2. Use the query (list) command of the database driver and filter all entities with an `EntityFilterContext`.  
   This is not recommended for large datasets as all records of a container need to be processed by the provider.


# Test environment

By default - environment variable **`TEST_DB_PROVIDER`** not present - a `FileDatabase` is used for methods attributed with
```csharp
[TestCase(test_db, Category = test_db)]
```

To run tests using a specific database implementation set the environment variable `TEST_DB_PROVIDER`.

Valid  **`TEST_DB_PROVIDER`** values:
```
file
sqlite
postgres
mysql
mariadb
cosmos
```


## Rider
Run unit tests for a specific database implementation add an entry at  
**File | Settings | Build, Execution, Deployment | Unit Testing | Test Runner**  
- Environment Variables
    - TEST_DB_PROVIDER=cosmos

## Unit test
```
dotnet test -c Release -l "console;verbosity=detailed" --filter TestCategory=test_db --environment TEST_DB_PROVIDER=mysql
```
![.NET Tests](https://github.com/friflo/Friflo.Json.Burst/workflows/.NET/badge.svg)

# friflo Json.Burst

A JSON parser/serializer and object mapper trimmed towards performance.  
The implementation strives towards maximizing CPU utilization and minimizing memory footprint.  
An **extra requirement** is to enable the JSON parser/serializer running in a
[Unity Burst Job](https://docs.unity3d.com/Packages/com.unity.burst@1.5/manual/docs/QuickStart.html).  
This supports running the parser/serializer within a separate thread of a bursted Job while leaving
CPU/memory resources to the main thread being the critical path in game loops.

![Logo](docs/images/Friflo.Json.Burst-128.png) 
 



# Features

- **JSON parser/serializer**
    - [**JsonParser**](Json/Burst/JsonParser.cs) / [**JsonSerializer**](Json/Burst/JsonSerializer.cs)
      in namespace: **`Friflo.Json.Burst`**
    - Optimized for performance and low memory footprint
        - No (0) allocations after a few iterations by using a few internal byte & int buffers
        - Support reusing of parser instance to avoid allocations on the heap
    - Skipping of JSON object members and elements (array elements and values on root)  
        Provide statistics (counts) about skipped JSON entries:
        arrays, objects, strings, integers, numbers, booleans and nulls
    - Clear/Compact API
    - Don't throw exceptions in case of invalid JSON. Provide a concept to return gracefully in application code.
    - No heap allocation in case of invalid JSON when creating an error message
    - Support JSON objects, arrays and values (string, number, boolean and null) on root level
    - Compatible to [Unity Burst Jobs](https://docs.unity3d.com/Packages/com.unity.burst@1.5/manual/docs/QuickStart.html)
      which requires using a
      [subset of C#/.NET language](https://docs.unity3d.com/Packages/com.unity.burst@1.5/manual/docs/CSharpLanguageSupport_Types.html)
      in the parser implementation.  
      In short this is the absense of using the heap in any way.
      This exclude the usage of managed types like classes, strings, arrays or exceptions.  
      To support this subset the library need to be compiled with `JSON_BURST`.  
      The default implementation is a little less restrict: arrays (`byte` & `int`) are used.

- **Object Mapper**
    - [**JsonReader**](Json/Managed/JsonReader.cs) / [**JsonWriter**](Json/Managed/JsonWriter.cs)
      in namespace: **`Friflo.Json.Managed`**
    - Support deserialization in two ways:
        - Created new objects and deserialize to them which is the common practice of many object mapper implementations.
        - Deserialize to passed object instances while reusing also their child objects referenced by fields,
          arrays and `List`'s. Right now `Dictionary` (maps) entries are not reused.  
          This avoids object allocation on the heap for the given instance and all its child objects
    - Support polymorphism
    - Optimized for performance and low memory footprint
        - Create an immutable type description for each `Type` to invoke only the minimum required
          reflection calls while de-/serializing
        - Support reusing of object mapper instance to avoid allocations on the heap
    - Support the container types: arrays, `List`, `IList`, `Dictionary` & `IDictionary`
    - Uses internally the JSON parser mentioned above
- UTF-8 support
- Compatible to .NET Standard.
    That is: .Net Core, .NET 5, .NET Framework, Mono, Xamarin (iOS, Mac, Android), UWP, Unity
- No dependencies to 3rd party libraries
- Expressive error messages when parsing invalid JSON. E.g.  
  `JsonParser error - unexpected character > expect key. Found: v path: 'map.key1' at position: 23`
- Small library (Friflo.Json.Burst.dll - 70kb )


# Unit test / Performance

The project is using [NUnit](https://nunit.org/) for unit testing. Execute them locally by running:
```
dotnet test -c Release -l "console;verbosity=detailed"
```
The units can be executed also within various IDEs. [Visual Studio](https://visualstudio.microsoft.com/),
[Rider](https://www.jetbrains.com/rider/) and [Visual Studio Code](https://visualstudio.microsoft.com/).

By using NUnit the unit tests can be executed via the Test Runner in the [Unity Editor](https://unity.com/)
(Window > General > Test Runner) as `EditMode` tests.

Additional to common unit testing of expected behavior, the test also ensure the following principles
with additional assertions:
- No (exact 0) allocations occur on the heap while running a parser or serializer a couple of times.
- No leaks of `native containers` are left over after tear down a unit test.  
  This is relevant only when using the library in Unity compiled with **JSON_BURST** - it is not relevant when running in CLR

## Performance .NET CLR (Common Language Runtime)

The test cases contain also JSON parser performance tests. Various JSON examples files are parsed by iteration them from begin to end. The parser results like JSON keys and values like strings, numbers, booleans are nulls are ready to be consumed.  
To reduce side effects in measurement by NUnit of throughput increase `impliedThroughput`
at [TestParserPerformance.cs](Json.Tests/Common/TestParserPerformance.cs)

On the used development system (Intel Core i7-4790k 4Ghz, Windows 10) the throughput of the example JSON files
within the CLR are at **200-550 MB/sec**.

## Performance Unity

- With **JSON_BURST** in Unity Editor  
Running the performance inside the Unity Editor in `Edit Mode` or in the `Test Runner` show weak performance numbers.
The reason is using `native container`'s within the Editor are a bottleneck. Throughput: **6-13 MB/sec**.
Imho - this is an acceptable development scenario.

- Without **JSON_BURST** in Unity Editor  
It is faster than *'with JSON_BURST in Unity Editor'* because in this scenario managed container are used instead of `native container`s. Throughput: **25-88 MB/sec**.  
*Note*: In this mode the parser & serializer cannot be used in Burst Jobs.

- With **JSON_BURST** in a Unity Build  
When building a game as a binary for deployment the numbers are okay. There is mainly no difference between
the `Scripting Backend` `Mono 2x` and `IL2CPP` which can be used for builds. Throughput: **56-116 MB/sec**






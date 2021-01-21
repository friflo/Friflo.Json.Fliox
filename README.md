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
    - Clear/Compact API: `Iterator API` for parser - `Appender API` for serializer.
    - Skipping of JSON object members and elements (array elements and values on root)  
        Provide statistics (counts) about skipped JSON entries:
        arrays, objects, strings, integers, numbers, booleans and nulls
    - Don't throw exceptions in `Release` build in any case - e.g. of invalid JSON. Provide a concept to return gracefully in application code.
    - Throw exceptions in `Debug` build to notice applications errors when using the library.
    - No heap allocation in case of invalid JSON when creating an error message
    - Support parsing/serializing of JSON objects, arrays and values (string, number, boolean and null) on root level
    - Optimization principles:
        - Minimize **memory footprint**
            - No (0) allocations after a few iterations by using a few internal byte & int buffers
            - Support reusing parser & serializer instances to avoid allocations on the heap
        - Minimize **CPU load**
            - Using only struct's, no classes (a requirement of Unity/Burst) enabling high memory locality to reduce page misses.  
                As a result the complete parser/serializer state lives on the stack.
            - Pass method parameters of struct's - a value type in .NET - always by `ref`.
            - No string copy or memcpy
    - Compatible to [Unity Burst Jobs](https://docs.unity3d.com/Packages/com.unity.burst@1.5/manual/docs/QuickStart.html)
      which requires using a
      [subset of C#/.NET language](https://docs.unity3d.com/Packages/com.unity.burst@1.5/manual/docs/CSharpLanguageSupport_Types.html)
      in the parser implementation.  
      In short this is the absense of using the heap in any way.
      This exclude the usage of managed types like classes, strings, arrays or exceptions.  
      To support this subset the library need to be compiled with `JSON_BURST`.  
      The default implementation is a little less restrict: arrays (`byte` & `int`) are used.

- **Object Mapper reader/writer**
    - [**JsonReader**](Json/Mapper/JsonReader.cs) / [**JsonWriter**](Json/Mapper/JsonWriter.cs)
      in namespace: **`Friflo.Json.Managed`**
    - Support deserialization in two ways:
        - Create new object instances and deserialize by using `Read()` to them which is the common practice of many object mapper implementations.
        - Deserialize to passed object instances by using ReadTo() while reusing also their child objects referenced by fields,
          arrays and `List`'s. Right now `Dictionary` (maps) entries are not reused.  
          This avoids object allocation on the heap for the given instance and all its child objects
    - Support polymorphism: Currently by a discriminator name `$type` as the first member: e.g. `{ "$type": "Tiger", ... }`
    - `JsonReader` support two error handling modes while parsing and deserialization (unmarshalling) -
      e.g. JSON validation errors.  
      By avoiding exceptions performance increases by the fact that throwing exceptions is an expensive operation because of object creation the heap. The error mode is set via `JsonReader.ThrowException`:
        1. Don't throw any exception and provide the error state via a boolean and a message.
        2. Throw exception in error case - which is useful for debugging.
    - Error messages are created without heap allocation to avoid vulnerability to DDoS attacks simply by flooding a service with invalid JSON.
    - Optimized for performance and low memory footprint
        - Create an immutable type description for each `Type` to invoke only the minimum required
          reflection calls while de-/serializing
        - Reusing of object mapper instance to avoid allocations on the heap
        - Avoid boxing/unboxing of primitive types (e.g. int, float, ...) to minimize heap allocations.
        - No heap allocations are performed when using `ReadTo()` and using a subset of supported types: arrays, `Lists` and classes ensured by [unit test](Json.Tests/Common/UnitTest/Mapper/TestNoAllocation.cs)
    - Supported C#/.NET types:
        - Container types: arrays, `List`, `IList`, `Dictionary` & `IDictionary`
        - Primitive types, `BigInteger` & `DateTime`
        - Support adding custom types as shown at [CustomTypeMapper](Json.Tests/Common/Examples/Mapper/CustomTypeMapper.cs)
    - Uses internally the JSON parser mentioned above

- UTF-8 support
- Compatible to .NET Standard.
    That is: .Net Core, .NET 5, .NET Framework, Mono, Xamarin (iOS, Mac, Android), UWP, Unity
- No unsafe code in CLR library
- Fail safe in case of JSON and application errors
- No dependencies to 3rd party libraries.  
  Except when compiling within Unity with **UNITY_BURST** it requires
  [Unity Collections](https://docs.unity3d.com/Packages/com.unity.collections@0.14/manual/index.html)
  to enable using `NativeArray`, `NativeList`, `FixedString32` & `FixedString128`
- Small library (Friflo.Json.Burst.dll ~ 100kb )
- Expressive error messages when parsing invalid JSON. E.g.  
    ```
    JsonParser/JSON error: unexpected character > expect key. Found: v path: 'map.key1' at position: 23
    ```


# Unit test / Performance

The current result of the unit test are available as CI tests at [Github actions](https://github.com/friflo/Friflo.Json.Burst/actions).

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

## Examples

The unit test also contain a folder explaining single file (self contained) examples illustrating usage and
anti patterns how to use (and how not to use) the `JsonParser` and `JsonSerializer`.

The examples can be found at [Json.Tests/Common/Examples/](Json.Tests/Common/Examples)

## Parser & Serializer

A minimal *Hello world* example showing how to parse a given JSON string via the `JsonParser`

```csharp
        public void HelloWorldParser() {
            string say = "", to = "";
            var p = new JsonParser();
            p.InitParser(new Bytes (@"{""say"": ""Hello"", ""to"": ""World 🌎""}"));
            p.NextEvent();
            var i = p.GetObjectIterator();
            while (p.NextObjectMember(ref i)) {
                if (p.UseMemberStr(ref i, "say"))  { say = p.value.ToString(); }
                if (p.UseMemberStr(ref i, "to"))   { to =  p.value.ToString(); }
            }
            Console.WriteLine($"Output: {say}, {to}");
            // Output: Hello, World 🌎
        }
```

A minimal *Hello world* using the serializer to create JSON via the `JsonSerializer`

```csharp
        public void HelloWorldSerializer() {
            var s = new JsonSerializer();
            s.InitSerializer();
            s.ObjectStart();
                s.MemberStr("say", "Hello");
                s.MemberStr("to",  "World 🌎");
            s.ObjectEnd();
            Console.WriteLine($"Output: {s.dst}");
            // Output: {"say":"Hello","to":"World 🌎"}
        }
```

## Object Mapper - Reader & Writer

An ObjectMapper maps a class to a JSON string and vise vera. Given the following class:

```csharp
        class Message {
            public string say;
            public string to;
        }
```

Use the `JsonReader` to deserialize / unmarshal a JSON string to a class instance.

```csharp
        public void HelloWorldReader() {
            var r = new JsonReader(new TypeStore());
            var msg = r.Read<Message>(new Bytes (@"{""say"": ""Hello 👋"", ""to"": ""World""}"));
            Console.WriteLine($"Output: {msg.say}, {msg.to}");
            // Output: Hello 👋, World
        }
```

Use the `JsonWriter` to serialize / marshal a class instance to a JSON string.

```csharp
        public void HelloWorldWriter() {
            var r = new JsonWriter(new TypeStore());
            r.Write(new Message {say = "Hello 👋", to = "World"});
            Console.WriteLine($"Output: {r.Output}");
            // Output: {"say":"Hello 👋","to":"World"}
        }
```


## Performance .NET CLR (Common Language Runtime)

The test cases contain also JSON parser performance tests.
Various JSON examples files are parsed by iteration them from begin to end.
The parser returns the JSON tree structure via an iterator. The keys and the JSON
values (strings, numbers, booleans are nulls) are ready to be consumed at this stage.

To reduce side effects in measurement by NUnit of throughput increase `impliedThroughput`
at [TestParserPerformance.cs](Json.Tests/Common/UnitTest/Burst/TestParserPerformance.cs)

On the used development system (Intel Core i7-4790k 4Ghz, Windows 10) the throughput of the example JSON files
within the CLR are at **200-550 MB/sec**. All tests are measured on one core.

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






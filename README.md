![.NET Tests](https://github.com/friflo/Friflo.Json.Burst/workflows/.NET/badge.svg)

# friflo Json.Burst
![Logo](docs/images/Friflo.Json.Burst-128.png) 
 

## Compatibility

## Features

- JSON parser - namespace: **Friflo.Json.Burst**
	- Optimized for performance and low memory footprint
		- No (0) allocations after a few iterations by using a few internal byte & int buffers in the parser
		- Support reusing of parser instance to avoid allocations on the heap
	- Skipping of object members and elements (array elements and values on root)  
		Provide statistics (counts) about skipped JSON entries (arrays, objects, strings, integers, numbers, booleans and nulls)
	- Support objects, arrays and values (string, number, boolean and null) on root level
	- Clear/Compact API
- Object Mapper - namespace: **Friflo.Json.Managed**
	- Support deserialization to:
		- newly created objects
		- passed object instances.  
			To avoid object allocation on the heap for the given instance and all its child objects
	- Support polymorphism
	- Optimized for performance and low memory footprint
		- Create an immutable Type description for each Type to invoke only the minimum required reflection calls while de-/serializing
		- Support reusing of object mapper instance to avoid allocations on the heap
	- Support the container types: arrays, List, IList, Dictionary & IDictionary
	- Uses internally the JSON parser mentioned above
- UTF-8 support
- Compatible to .NET Standard.
	That is: .Net Core, .NET 5, .NET Framework, Mono, Xamarin (iOS, Mac, Android), UWP, Unity
- No dependencies to 3rd party libraries
- Allow single quotation marks for strings
- Expressive error messages when parsing invalid JSON
- Compatibly to Unity Burst Jobs
- Small library (Friflo.Json.Burst.dll - 70kb )


# Unit test / Performance

The Project is using **NUnit** for unit testing. Execute them locally by running:
```
dotnet test -c Release -l "console;verbosity=detailed"
```
The units can be executed also within various IDEs. **Visual Studio**, **Rider** and **Visual Studio Code**.

By using NUnit the unit tests can be executed via the Test Runner in the **Unity Editor** (Window > General > Test Runner) as `EditMode` tests.

Additional to common unit testing of expected behavior, the test also ensure the following principles with additional assertions:
- No (exact 0) allocations occur on the heap while running a parser or serializer a couple of times.
- No leaks of `native containers` are left over after tear down a unit test. This is relevant only when using the library in Unity compiled with **JSON_BURST** - it is not relevant when running in CLR

## Performance .NET CLR (Common Language Runtime)

The test cases contain also JSON parser (a JSON iterator) performance test.
To reduce side effects in measurement of throughput increase `impliedThroughput` at [TestParserPerformance.cs](Json.Tests/Common/TestParserPerformance.cs)

On my development system (Intel Core i7-4790k 4Ghz) the throughput of the example JSON files within the CLR are at **200-550 MB/sec**.

## Performance Unity

Running the performance inside the Unity Editor in Edit Mode or in the Test Runner show weak performance numbers.
The reason is that the Editor uses only the Mono runtime in these modes. Throughput: **6-13 MB/sec**.

When building the game as a binary for deployment the numbers are okay. There is mainly no difference between the `Scripting Backend` `Mono 2x` and `IL2CPP` which can be used for builds. Throughput: **56-116 MB/sec**





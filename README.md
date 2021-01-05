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


# Unit tests

Project is using NUnit for unit testing. Execute them locally by running. 
```
dotnet test -c Release -l "console;verbosity=detailed"
```

## Performance
The test cases contain also some performance test.
To reduce side effects in measurement (MB/s) increase `impliedThroughput` at the [performance test](Json.Tests/Common/TestParserPerformance.cs)
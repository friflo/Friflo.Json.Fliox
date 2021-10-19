

# ![logo](../../../docs/images/Json-Fliox.svg)        **JSON Fliox - Schema**



## **`Friflo.Json.Fliox.Schema`**

This namespace contains classes and methods to transform / generate Type Schemas.  
For example to generate a **JSON Schema** from given **C#** model classes.  
The schema transformation can be used for database models and JSON based protocols.

Another objective of this namespace is to enables type validation for JSON payloads.

Currently supported input schemas are:
- C#
- JSON Schema

From these input schemas the following output schemas can be generate:
- C#
- JSON Schema
- Typescript
- Kotlin

## Features:
- Code generators and JSON Validator support all common C#/.Net types like:
    - classes, structs, primitives and enums
    - Nullable structs, primitives and enums
    - container types like: arrays, List<>, Dictionary<>, Queue<>, Stack<>, ...
    - polymorphic classes with discriminator / discriminants.
    - namespace support

- Create clear and concise messages for validation errors. E.g.  
    `Missing required fields: [id, name] at Article > (root), pos: 2`

- The JSON Validator is trimmed towards performance by minimizing GC/ Heap pressure and
  aiming for high memory locality.  
  In case of small JSON payloads validation reaches 1.000.000 validations / second.

- Code generators are designed to be small and easy to maintain ~ 200 LOC / generator.  
  Also their performance reaches 10.000 schema transformations / second for smaller schemas.

## Usage
- Examples for code generation and JSON type validation at:  
  [Schema generators & JSON Validation tests](../../../Json.Tests/Common/UnitTest/Fliox/Schema)

- Schema validation in an `EntityDatabase` is demonstrated at:  
  [FlioxServer](../../../Json.Tests/Main/Program.cs)


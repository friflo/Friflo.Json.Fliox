

# ![logo](../../../docs/images/Json-Flow.svg)        ***friflo*** **JSON Flow Schema**



## **`Friflo.Json.Flow.Schema`**

This namespace contains classes and methods to transform / generate Type Schemas.  
For example to generate a **JSON Schema** from given **C#** model classes.  
The schema transformation can be used for database models and JSON based protocols.

Another objective of this namespace is to enables type validation for JSON payloads.

Currently supported input schemas are:
- C#
- JSON Schema

From these input schema the following output schemas can be generate:
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

- Create clear and concise messages for validation errors. E.g.  
    `Missing required fields: [id, name] at Article > (root), pos: 2`

- The JSON Validator is trimmed towards performance by minimizing GC/ Heap pressure and
  aiming for high memory locality.  
  In case of small JSON payloads validation reaches 1.000.000 validations / second.

- Code generators are designed to be small and easy to maintain ~ 200 LOC / generator.  
  Also their performance reaches 10.000 schema transformations / second for smaller schemas.




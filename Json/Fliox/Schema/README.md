

# ![logo](../../../docs/images/Json-Fliox.svg)        **JSON Fliox - Schema**



## **`Friflo.Json.Fliox.Schema`**

This namespace contains classes and methods to transform / generate Type Schemas.  
For example to generate a **JSON Schema** from given **C#** model classes or vice vera.  
The schema transformation can be used for database schemas and JSON based protocols.

Another objective of this namespace is to enables type validation for JSON payloads.

Currently supported input schemas are:
- C#
- JSON Schema

From these input schemas the following output schemas can be generate:
- C#
- JSON Schema
- HTML
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

- The generated JSON Schema files are compatible to the specification
  [JSON Schema Draft-07](https://json-schema.org/draft-07/json-schema-release-notes.html).  
  This enables using external **JSON Schema validators** to validate JSON files against the generated schemas.  
  E.g. by [Ajv JSON schema validator](https://ajv.js.org/) running in Node.js or in the browser.

- The generated schemas for various languages are directly available via a **Fliox Hub** in the Browser.  
  To retrieve a zip or a single file click on any type link in the [JSON Fliox Explorer](../../Fliox.Hub.Explorer/)
  and follow the link **Typescript, C#, Kotlin, JSON Schema** on the top of the schema page.


## Usage
- Examples for code generation and JSON type validation at:  
  [Schema generators & JSON Validation tests](../../../Json.Tests/Common/UnitTest/Fliox/Schema)

- Schema validation for an `EntityDatabase` by a `JSON.Fliox` server is demonstrated at:  
  [FlioxServer](../../../Json.Tests/Main/Program.cs)


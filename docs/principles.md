## **Principles**

- dependencies
    - no 3rd party dependencies
    - small size of required Fliox assemblies (*.dll) ~ 1250 kb in total, 430 kb zipped  
      source code: library 67k LOC, unit tests: 25k LOC
- target for optimal performance
    - maximize throughput, minimize latency, minimize heap allocations and boxing
    - enable task batching as a unit-of-work
    - support bulk operations for CRUD commands
- compact and strongly typed API
    - type safe access to entities and their keys when dealing with containers  
    - type safe access to DTO's when dealing with database commands
    - absence of using `object` as a type
    - absence of utility classes & methods to
        - to use the API in an explicit manner
        - to avoid confusion implementing the same feature in multiple ways
- serialization of entities and messages - request, response & event - are entirely JSON
- the **Zero** principles
    - 0 compiler errors and warnings
    - 0 ReSharper errors, warnings, suggestions and hints
    - 0 unit test errors, no flaky tests
    - 0 typos - observed by spell checker
    - no synchronous calls to API's dealing with **IO** like network or disc    
      Instead using `async` / `await`
    - no 3rd party dependencies
    - no heap allocations if possible
    - no noise in `.ToString()` methods while debugging - only relevant state.  
      E.g. instances of `FlioxClient`, `EntitySet<,>`, `FlioxHub` and `EntityDatabase`
    - no surprise of API behavior.  
      See [Principle of least astonishment](https://en.wikipedia.org/wiki/Principle_of_least_astonishment)
    - no automatic C# Code formatting - as no Code Formatter supports the code style of this project.  
      That concerns tabular indentation of fields, properties, variables and switch cases.
- extensibility
    - support custom database adapters aka providers
    - support custom code / schema generators for new programming languages

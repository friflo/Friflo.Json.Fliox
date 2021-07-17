

This folder contains the database used by the unit tests located in [Flow/Graph unit tests](../../Common/UnitTest/Flow/Graph).

The schema of the database is describes via the models in [PocStore.cs](../../Common/UnitTest/Flow/Graph/PocStore.cs).

Each `EntitySet<>` field in `PocStore` describe the model used in each folder aka container.
This means that all payloads in a container folder are of a specific type.  
These are `Article`, `Customer`, `Employee`, `Order` and `Producer`.

The schema and its models are only utilized by clients when using an `EntityStore` and `EntitySet<>`.  
The database itself is schema-less. This means all `EntityDatabase` implementations (e.g. a file or memory database)
perform their commands without an schema definition.

By ensuring this principle development of a domain specific application can be realized by pure client development.  
The database implementation (the library) - typically a service - is generic and remain unchanged while the whole
development process.  
This avoid frequent updates of the database services and minimize dependencies between clients and servers.

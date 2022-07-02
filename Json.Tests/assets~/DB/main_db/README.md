
# PocStore database

This folder contains the database used by the unit tests located in [Fliox/Client unit tests](../../../Common/UnitTest/Fliox/Client).

The schema of the database is defined via the models in [PocStore.cs](../../../Common/UnitTest/Fliox/Client/PocStore.cs).

Each `EntitySet<>` field in `PocStore` describe the model used for the entities (files) in each container (folder).
This means that all payloads in a container folder are of a specific type.  
These are `Article`, `Customer`, `Employee`, `Order` and `Producer`.

## FlioxClient / FlioxHub ➞ EntityDatabase
The schema and its models are only utilized by `FlioxClient` and `EntitySet<>`.  
The database itself can be used without a schema. This means all `EntityDatabase` implementations (e.g. a memory, file or remote database)
perform their commands without a schema definition. A schema validation can be optionally assigned to an `EntityDatabase`.

By ensuring this principle development of a domain specific application can be realized by pure client development.  
The database implementation (the library) - typically a service - is generic and remain unchanged while the whole
development process.  
This avoid frequent updates of the database services and minimize dependencies between clients and servers.

## VSCode
To simplify manual editing of entities (files) in VSCode [PocStore - JSON Schema](../../Schema/JSON/PocStore) is used.
The JSON Schema files in this folder are generated from the models by the [Schema Generator](../../../Common/UnitTest/Fliox/Schema/PocStore.cs).

The mapping of **JSON Schema** files via VSCode is explained here:
[Mapping to a schema in the workspace](https://code.visualstudio.com/docs/languages/json#_mapping-to-a-schema-in-the-workspace)

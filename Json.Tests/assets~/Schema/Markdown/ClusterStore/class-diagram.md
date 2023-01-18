[generated-by]: https://github.com/friflo/Friflo.Json.Fliox#schema

```mermaid
classDiagram
direction LR

class ClusterStore:::cssSchema {
    <<Schema>>
    <<abstract>>
    containers  : [id] ➞ DbContainers
    messages    : [id] ➞ DbMessages
    schemas     : [id] ➞ DbSchema
}
ClusterStore *-- "0..*" DbContainers : containers
ClusterStore *-- "0..*" DbMessages : messages
ClusterStore *-- "0..*" DbSchema : schemas

class DbContainers:::cssEntity {
    <<Entity · id>>
    id          : string
    storage     : string
    containers  : string[]
    defaultDB?  : boolean
}

class DbMessages:::cssEntity {
    <<Entity · id>>
    id        : string
    commands  : string[]
    messages  : string[]
}

class DbSchema:::cssEntity {
    <<Entity · id>>
    id           : string
    schemaName   : string
    schemaPath   : string
    jsonSchemas  : string ➞ any
}


```

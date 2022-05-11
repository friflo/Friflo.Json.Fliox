[generated-by]: https://github.com/friflo/Friflo.Json.Fliox#schema

```mermaid
classDiagram
direction LR

class UserStore:::cssSchema {
    <<Schema>>
    <<abstract>>
    credentials  : [id] ➞ UserCredential
    permissions  : [id] ➞ UserPermission
    roles        : [id] ➞ Role
}
UserStore *-- "0..*" UserCredential : credentials
UserStore *-- "0..*" UserPermission : permissions
UserStore *-- "0..*" Role : roles

class UserCredential:::cssEntity {
    <<Entity · id>>
    id     : string
    token? : string
}

class UserPermission:::cssEntity {
    <<Entity · id>>
    id     : string
    roles? : string[]
}
UserPermission o.. "0..*" Role : roles

class Role:::cssEntity {
    <<Entity · id>>
    id           : string
    rights       : Right[]
    description? : string
}
Role *-- "0..*" Right : rights

class Right {
    <<abstract>>
    description? : string
}

Right <|-- AllowRight
class AllowRight {
    type         : "allow"
    database     : string
}

Right <|-- TaskRight
class TaskRight {
    type         : "task"
    database     : string
    types        : TaskType[]
}
TaskRight *-- "0..*" TaskType : types

Right <|-- SendMessageRight
class SendMessageRight {
    type         : "sendMessage"
    database     : string
    names        : string[]
}

Right <|-- SubscribeMessageRight
class SubscribeMessageRight {
    type         : "subscribeMessage"
    database     : string
    names        : string[]
}

Right <|-- OperationRight
class OperationRight {
    type         : "operation"
    database     : string
    containers   : string ➞ ContainerAccess
}
OperationRight *-- "0..*" ContainerAccess : containers

class ContainerAccess {
    operations?       : OperationType[]
    subscribeChanges? : Change[]
}
ContainerAccess *-- "0..*" OperationType : operations
ContainerAccess *-- "0..*" Change : subscribeChanges

class OperationType:::cssEnum {
    <<enumeration>>
    create
    upsert
    delete
    deleteAll
    patch
    read
    query
    aggregate
    mutate
    full
}


Right <|-- PredicateRight
class PredicateRight {
    type         : "predicate"
    names        : string[]
}

class TaskType:::cssEnum {
    <<enumeration>>
    read
    query
    create
    upsert
    patch
    delete
    aggregate
    message
    command
    closeCursors
    subscribeChanges
    subscribeMessage
    reserveKeys
    error
}


class Change:::cssEnum {
    <<enumeration>>
    create
    upsert
    patch
    delete
}



```

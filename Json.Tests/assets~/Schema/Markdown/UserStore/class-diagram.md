```mermaid
classDiagram
direction LR

class UserStore:::cssSchema {
    <<Schema>>
    <<abstract>>
    credentials  : UserCredential[]
    permissions  : UserPermission[]
    roles        : Role[]
}
UserStore "*" --> "1" UserCredential : credentials
UserStore "*" --> "1" UserPermission : permissions
UserStore "*" --> "1" Role : roles

class UserCredential:::cssEntity {
    id     : string
    token? : string | null
}

class UserPermission:::cssEntity {
    id     : string
    roles? : string[] | null
}
UserPermission "*" ..> "1" Role : roles

class Role:::cssEntity {
    id           : string
    rights       : Right[]
    description? : string | null
}
Role "*" --> "1" Right : rights

class Right {
    <<abstract>>
    description? : string | null
}

Right <|-- AllowRight
class AllowRight {
    type         : "allow"
    database?    : string | null
}

Right <|-- TaskRight
class TaskRight {
    type         : "task"
    database?    : string | null
    types        : TaskType[]
}
TaskRight "*" --> "1" TaskType : types

Right <|-- SendMessageRight
class SendMessageRight {
    type         : "sendMessage"
    database?    : string | null
    names        : string[]
}

Right <|-- SubscribeMessageRight
class SubscribeMessageRight {
    type         : "subscribeMessage"
    database?    : string | null
    names        : string[]
}

Right <|-- OperationRight
class OperationRight {
    type         : "operation"
    database?    : string | null
    containers   : ContainerAccess[]
}
OperationRight "*" --> "1" ContainerAccess : containers

class ContainerAccess {
    operations?       : OperationType[] | null
    subscribeChanges? : Change[] | null
}
ContainerAccess "*" --> "1" OperationType : operations
ContainerAccess "*" --> "1" Change : subscribeChanges

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

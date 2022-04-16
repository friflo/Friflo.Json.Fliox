```mermaid
classDiagram
direction RL

class UserStore {
    <<Service>>
    credentials  : UserCredential[]
    permissions  : UserPermission[]
    roles        : Role[]
}

class UserCredential {
    id     : string
    token? : string | null
}

class UserPermission {
    id     : string
    roles? : string[] | null
}

class Role {
    id           : string
    rights       : Right[]
    description? : string | null
}

class Credentials {
    userId  : string
    token   : string
}

class AuthResult {
    isValid  : boolean
}

class DbContainers {
    id          : string
    storage     : string
    containers  : string[]
}

class DbMessages {
    id        : string
    commands  : string[]
    messages  : string[]
}

class DbSchema {
    id           : string
    schemaName   : string
    schemaPath   : string
    jsonSchemas  : any[]
}

class DbStats {
    containers? : ContainerStats[] | null
}

class ContainerStats {
    name   : string
    count  : int64
}

class HostDetails {
    version         : string
    hostName?       : string | null
    projectName?    : string | null
    projectWebsite? : string | null
    envName?        : string | null
    envColor?       : string | null
    routes          : string[]
}

class HostCluster {
    databases  : DbContainers[]
}

class type Right {
    <<abstract>>
    description? : string | null
}

class AllowRight {
    database?    : string | null
}

class TaskRight {
    database?    : string | null
    types        : TaskType[]
}

class SendMessageRight {
    database?    : string | null
    names        : string[]
}

class SubscribeMessageRight {
    database?    : string | null
    names        : string[]
}

class OperationRight {
    database?    : string | null
    containers   : ContainerAccess[]
}

class ContainerAccess {
    operations?       : OperationType[] | null
    subscribeChanges? : Change[] | null
}

class OperationType {
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


class PredicateRight {
    names        : string[]
}

class TaskType {
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


class Change {
    <<enumeration>>
    create
    upsert
    patch
    delete
}



```

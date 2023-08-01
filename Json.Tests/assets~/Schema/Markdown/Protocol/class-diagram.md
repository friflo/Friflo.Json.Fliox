[generated-by]: https://github.com/friflo/Friflo.Json.Fliox#schema

```mermaid
classDiagram
direction LR

class ProtocolMessage {
    <<abstract>>
}

ProtocolMessage <|-- ProtocolRequest
class ProtocolRequest {
    <<abstract>>
    req? : int32
    clt? : string
}

ProtocolRequest <|-- SyncRequest
class SyncRequest {
    msg    : "sync"
    user?  : string
    token? : string
    ack?   : int32
    tasks  : SyncRequestTask[]
    db?    : string
    info?  : any
}
SyncRequest *-- "0..*" SyncRequestTask : tasks

ProtocolMessage <|-- ProtocolResponse
class ProtocolResponse {
    <<abstract>>
    req? : int32
    clt? : string
}

ProtocolResponse <|-- SyncResponse
class SyncResponse {
    msg        : "resp"
    db?        : string
    tasks?     : SyncTaskResult[]
    info?      : any
    authError? : string
}
SyncResponse *-- "0..*" SyncTaskResult : tasks

ProtocolResponse <|-- ErrorResponse
class ErrorResponse {
    msg      : "error"
    message? : string
    type     : ErrorResponseType
}
ErrorResponse *-- "1" ErrorResponseType : type

class ErrorResponseType:::cssEnum {
    <<enumeration>>
    BadRequest
    Exception
    BadResponse
}


ProtocolMessage <|-- ProtocolEvent
class ProtocolEvent {
    <<abstract>>
    clt? : string
}

ProtocolEvent <|-- EventMessage
class EventMessage {
    msg  : "ev"
    seq  : int32
    ev?  : SyncEvent[]
}
EventMessage *-- "0..*" SyncEvent : ev

class SyncEvent {
    usr?   : string
    clt?   : string
    db?    : string
    tasks? : SyncRequestTask[]
}
SyncEvent *-- "0..*" SyncRequestTask : tasks

class References {
    selector    : string
    cont        : string
    orderByKey? : SortOrder
    keyName?    : string
    isIntKey?   : boolean
    references? : References[]
}
References *-- "0..1" SortOrder : orderByKey
References *-- "0..*" References : references

class EntityError {
    id       : string
    type     : EntityErrorType
    message? : string
}
EntityError *-- "1" EntityErrorType : type

class EntityErrorType:::cssEnum {
    <<enumeration>>
    Undefined
    ParseError
    ReadError
    WriteError
    DeleteError
    PatchError
}


class ReferencesResult {
    error?      : string
    cont?       : string
    len?        : int32
    set         : any[]
    errors?     : EntityError[]
    references? : ReferencesResult[]
}
ReferencesResult *-- "0..*" EntityError : errors
ReferencesResult *-- "0..*" ReferencesResult : references

class SyncRequestTask {
    <<abstract>>
    info? : any
}

SyncRequestTask <|-- CreateEntities
class CreateEntities {
    task           : "create"
    cont           : string
    reservedToken? : Guid
    keyName?       : string
    set            : any[]
}

SyncRequestTask <|-- UpsertEntities
class UpsertEntities {
    task     : "upsert"
    cont     : string
    keyName? : string
    set      : any[]
}

SyncRequestTask <|-- ReadEntities
class ReadEntities {
    task        : "read"
    cont        : string
    keyName?    : string
    isIntKey?   : boolean
    ids         : string[]
    references? : References[]
}
ReadEntities *-- "0..*" References : references

class SortOrder:::cssEnum {
    <<enumeration>>
    asc
    desc
}


SyncRequestTask <|-- QueryEntities
class QueryEntities {
    task        : "query"
    cont        : string
    orderByKey? : SortOrder
    keyName?    : string
    isIntKey?   : boolean
    filterTree? : any
    filter?     : string
    references? : References[]
    limit?      : int32
    maxCount?   : int32
    cursor?     : string
}
QueryEntities *-- "0..1" SortOrder : orderByKey
QueryEntities *-- "0..*" References : references

SyncRequestTask <|-- AggregateEntities
class AggregateEntities {
    task        : "aggregate"
    cont        : string
    type        : AggregateType
    filterTree? : any
    filter?     : string
}
AggregateEntities *-- "1" AggregateType : type

class AggregateType:::cssEnum {
    <<enumeration>>
    count
}


SyncRequestTask <|-- MergeEntities
class MergeEntities {
    task     : "merge"
    cont     : string
    keyName? : string
    set      : any[]
}

SyncRequestTask <|-- DeleteEntities
class DeleteEntities {
    task  : "delete"
    cont  : string
    ids?  : string[]
    all?  : boolean
}

SyncRequestTask <|-- SyncMessageTask
class SyncMessageTask {
    <<abstract>>
    name     : string
    param?   : any
    users?   : string[]
    clients? : string[]
    groups?  : string[]
}

SyncMessageTask <|-- SendMessage
class SendMessage {
    task     : "msg"
}

SyncMessageTask <|-- SendCommand
class SendCommand {
    task     : "cmd"
}

SyncRequestTask <|-- CloseCursors
class CloseCursors {
    task     : "closeCursors"
    cont     : string
    cursors? : string[]
}

SyncRequestTask <|-- SubscribeChanges
class SubscribeChanges {
    task     : "subscribeChanges"
    cont     : string
    changes  : EntityChange[]
    filter?  : string
}
SubscribeChanges *-- "0..*" EntityChange : changes

class EntityChange:::cssEnum {
    <<enumeration>>
    create
    upsert
    merge
    delete
}


SyncRequestTask <|-- SubscribeMessage
class SubscribeMessage {
    task    : "subscribeMessage"
    name    : string
    remove? : boolean
}

SyncRequestTask <|-- ReserveKeys
class ReserveKeys {
    task   : "reserveKeys"
    cont   : string
    count  : int32
}

class SyncTaskResult {
    <<abstract>>
}

SyncTaskResult <|-- CreateEntitiesResult
class CreateEntitiesResult {
    task    : "create"
    errors? : EntityError[]
}
CreateEntitiesResult *-- "0..*" EntityError : errors

SyncTaskResult <|-- UpsertEntitiesResult
class UpsertEntitiesResult {
    task    : "upsert"
    errors? : EntityError[]
}
UpsertEntitiesResult *-- "0..*" EntityError : errors

SyncTaskResult <|-- ReadEntitiesResult
class ReadEntitiesResult {
    task        : "read"
    set         : any[]
    notFound?   : string[]
    errors?     : EntityError[]
    references? : ReferencesResult[]
}
ReadEntitiesResult *-- "0..*" EntityError : errors
ReadEntitiesResult *-- "0..*" ReferencesResult : references

SyncTaskResult <|-- QueryEntitiesResult
class QueryEntitiesResult {
    task        : "query"
    cont?       : string
    cursor?     : string
    len?        : int32
    set         : any[]
    errors?     : EntityError[]
    references? : ReferencesResult[]
    sql?        : string
}
QueryEntitiesResult *-- "0..*" EntityError : errors
QueryEntitiesResult *-- "0..*" ReferencesResult : references

SyncTaskResult <|-- AggregateEntitiesResult
class AggregateEntitiesResult {
    task   : "aggregate"
    cont?  : string
    value? : double
}

SyncTaskResult <|-- MergeEntitiesResult
class MergeEntitiesResult {
    task    : "merge"
    errors? : EntityError[]
}
MergeEntitiesResult *-- "0..*" EntityError : errors

SyncTaskResult <|-- DeleteEntitiesResult
class DeleteEntitiesResult {
    task    : "delete"
    errors? : EntityError[]
}
DeleteEntitiesResult *-- "0..*" EntityError : errors

SyncTaskResult <|-- SyncMessageResult
class SyncMessageResult {
    <<abstract>>
}

SyncMessageResult <|-- SendMessageResult
class SendMessageResult {
    task  : "msg"
}

SyncMessageResult <|-- SendCommandResult
class SendCommandResult {
    task    : "cmd"
    result? : any
}

SyncTaskResult <|-- CloseCursorsResult
class CloseCursorsResult {
    task   : "closeCursors"
    count  : int32
}

SyncTaskResult <|-- SubscribeChangesResult
class SubscribeChangesResult {
    task  : "subscribeChanges"
}

SyncTaskResult <|-- SubscribeMessageResult
class SubscribeMessageResult {
    task  : "subscribeMessage"
}

SyncTaskResult <|-- ReserveKeysResult
class ReserveKeysResult {
    task  : "reserveKeys"
    keys? : ReservedKeys
}
ReserveKeysResult *-- "0..1" ReservedKeys : keys

class ReservedKeys {
    start  : int64
    count  : int32
    token  : Guid
}

SyncTaskResult <|-- TaskErrorResult
class TaskErrorResult {
    task        : "error"
    type        : TaskErrorType
    message?    : string
    stacktrace? : string
}
TaskErrorResult *-- "1" TaskErrorType : type

class TaskErrorType:::cssEnum {
    <<enumeration>>
    None
    UnhandledException
    DatabaseError
    FilterError
    ValidationError
    CommandError
    InvalidTask
    NotImplemented
    PermissionDenied
    SyncError
    EntityErrors
    InvalidResponse
}



```

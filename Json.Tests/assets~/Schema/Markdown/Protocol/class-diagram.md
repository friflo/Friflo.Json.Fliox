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
    msg       : "sync"
    user?     : string
    token?    : string
    ack?      : int32
    tasks     : SyncRequestTask[]
    database? : string
    info?     : any
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
    msg         : "resp"
    database?   : string
    tasks?      : SyncTaskResult[]
    containers? : ContainerEntities[]
    info?       : any
}
SyncResponse *-- "0..*" SyncTaskResult : tasks
SyncResponse *-- "0..*" ContainerEntities : containers

class ContainerEntities {
    container  : string
    count?     : int32
    entities   : any[]
    notFound?  : string[]
    errors?    : EntityError[]
}
ContainerEntities *-- "0..*" EntityError : errors

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
    clt  : string
}

ProtocolEvent <|-- EventMessage
class EventMessage {
    msg     : "ev"
    seq     : int32
    events? : SyncEvent[]
}
EventMessage *-- "0..*" SyncEvent : events

class SyncEvent {
    usr?   : string
    clt?   : string
    db     : string
    tasks? : SyncRequestTask[]
}
SyncEvent *-- "0..*" SyncRequestTask : tasks

class References {
    selector    : string
    container   : string
    keyName?    : string
    isIntKey?   : boolean
    references? : References[]
}
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
    container?  : string
    count?      : int32
    ids         : string[]
    references? : ReferencesResult[]
}
ReferencesResult *-- "0..*" ReferencesResult : references

class SyncRequestTask {
    <<abstract>>
    info? : any
}

SyncRequestTask <|-- CreateEntities
class CreateEntities {
    task           : "create"
    container      : string
    reservedToken? : Guid
    keyName?       : string
    entities       : any[]
}

SyncRequestTask <|-- UpsertEntities
class UpsertEntities {
    task       : "upsert"
    container  : string
    keyName?   : string
    entities   : any[]
}

SyncRequestTask <|-- ReadEntities
class ReadEntities {
    task        : "read"
    container   : string
    keyName?    : string
    isIntKey?   : boolean
    ids         : string[]
    references? : References[]
}
ReadEntities *-- "0..*" References : references

SyncRequestTask <|-- QueryEntities
class QueryEntities {
    task        : "query"
    container   : string
    keyName?    : string
    isIntKey?   : boolean
    filterTree? : any
    filter?     : string
    references? : References[]
    limit?      : int32
    maxCount?   : int32
    cursor?     : string
}
QueryEntities *-- "0..*" References : references

SyncRequestTask <|-- AggregateEntities
class AggregateEntities {
    task        : "aggregate"
    container   : string
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
    task       : "merge"
    container  : string
    keyName?   : string
    patches    : any[]
}

SyncRequestTask <|-- DeleteEntities
class DeleteEntities {
    task       : "delete"
    container  : string
    ids?       : string[]
    all?       : boolean
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
    task     : "message"
}

SyncMessageTask <|-- SendCommand
class SendCommand {
    task     : "command"
}

SyncRequestTask <|-- CloseCursors
class CloseCursors {
    task       : "closeCursors"
    container  : string
    cursors?   : string[]
}

SyncRequestTask <|-- SubscribeChanges
class SubscribeChanges {
    task       : "subscribeChanges"
    container  : string
    changes    : EntityChange[]
    filter?    : string
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
    task       : "reserveKeys"
    container  : string
    count      : int32
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
    references? : ReferencesResult[]
}
ReadEntitiesResult *-- "0..*" ReferencesResult : references

SyncTaskResult <|-- QueryEntitiesResult
class QueryEntitiesResult {
    task        : "query"
    container?  : string
    cursor?     : string
    count?      : int32
    ids         : string[]
    references? : ReferencesResult[]
}
QueryEntitiesResult *-- "0..*" ReferencesResult : references

SyncTaskResult <|-- AggregateEntitiesResult
class AggregateEntitiesResult {
    task       : "aggregate"
    container? : string
    value?     : double
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
    task  : "message"
}

SyncMessageResult <|-- SendCommandResult
class SendCommandResult {
    task    : "command"
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
    type        : TaskErrorResultType
    message?    : string
    stacktrace? : string
}
TaskErrorResult *-- "1" TaskErrorResultType : type

class TaskErrorResultType:::cssEnum {
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
}



```

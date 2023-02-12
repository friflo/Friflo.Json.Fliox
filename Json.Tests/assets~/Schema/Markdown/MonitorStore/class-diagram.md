[generated-by]: https://github.com/friflo/Friflo.Json.Fliox#schema

```mermaid
classDiagram
direction LR

class MonitorStore:::cssSchema {
    <<Schema>>
    <<abstract>>
    hosts      : [id] ➞ HostHits
    users      : [id] ➞ UserHits
    clients    : [id] ➞ ClientHits
    histories  : [id] ➞ HistoryHits
}
MonitorStore *-- "0..*" HostHits : hosts
MonitorStore *-- "0..*" UserHits : users
MonitorStore *-- "0..*" ClientHits : clients
MonitorStore *-- "0..*" HistoryHits : histories

class HostHits:::cssEntity {
    <<Entity · id>>
    id      : string
    counts  : RequestCount
}
HostHits *-- "1" RequestCount : counts

class UserHits:::cssEntity {
    <<Entity · id>>
    id       : string
    clients  : string[]
    counts?  : RequestCount[]
}
UserHits o.. "0..*" ClientHits : clients
UserHits *-- "0..*" RequestCount : counts

class ClientHits:::cssEntity {
    <<Entity · id>>
    id                  : string
    user                : string
    counts?             : RequestCount[]
    subscriptionEvents? : SubscriptionEvents
}
ClientHits o.. "1" UserHits : user
ClientHits *-- "0..*" RequestCount : counts
ClientHits *-- "0..1" SubscriptionEvents : subscriptionEvents

class HistoryHits:::cssEntity {
    <<Entity · id>>
    id          : int32
    counters    : int32[]
    lastUpdate  : int32
}

class RequestCount {
    db?       : string
    requests  : int32
    tasks     : int32
}

class SubscriptionEvents {
    seq          : int32
    queued       : int32
    queueEvents  : boolean
    connected    : boolean
    endpoint?    : string
    messageSubs? : string[]
    changeSubs?  : ChangeSubscription[]
}
SubscriptionEvents *-- "0..*" ChangeSubscription : changeSubs

class ChangeSubscription {
    container  : string
    changes    : EntityChange[]
    filter?    : string
}
ChangeSubscription *-- "0..*" EntityChange : changes

class EntityChange:::cssEnum {
    <<enumeration>>
    create
    upsert
    merge
    delete
}



```

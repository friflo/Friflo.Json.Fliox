```mermaid
classDiagram
direction LR

class MonitorStore:::cssSchema {
    <<Schema>>
    <<abstract>>
    hosts      : HostHits[]
    users      : UserHits[]
    clients    : ClientHits[]
    histories  : HistoryHits[]
}
MonitorStore "*" --> "1" HostHits : hosts
MonitorStore "*" --> "1" UserHits : users
MonitorStore "*" --> "1" ClientHits : clients
MonitorStore "*" --> "1" HistoryHits : histories

class HostHits:::cssEntity {
    id      : string
    counts  : RequestCount
}
HostHits "*" --> "1" RequestCount : counts

class UserHits:::cssEntity {
    id       : string
    clients  : string[]
    counts?  : RequestCount[] | null
}
UserHits "*" ..> "1" ClientHits : clients
UserHits "*" --> "1" RequestCount : counts

class ClientHits:::cssEntity {
    id      : string
    user    : string
    counts? : RequestCount[] | null
    event?  : EventDelivery | null
}
ClientHits "*" ..> "1" UserHits : user
ClientHits "*" --> "1" RequestCount : counts
ClientHits "*" --> "1" EventDelivery : event

class HistoryHits:::cssEntity {
    id          : int32
    counters    : int32[]
    lastUpdate  : int32
}

class RequestCount {
    db?       : string | null
    requests  : int32
    tasks     : int32
}

class EventDelivery {
    seq          : int32
    queued       : int32
    messageSubs? : string[] | null
    changeSubs?  : ChangeSubscription[] | null
}
EventDelivery "*" --> "1" ChangeSubscription : changeSubs

class ChangeSubscription {
    container  : string
    changes    : Change[]
    filter?    : string | null
}
ChangeSubscription "*" --> "1" Change : changes

class Change:::cssEnum {
    <<enumeration>>
    create
    upsert
    patch
    delete
}



```

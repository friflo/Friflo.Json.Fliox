[generated-by]: https://github.com/friflo/Friflo.Json.Fliox#schema

```mermaid
classDiagram
direction LR

class GlobalClient:::cssSchema {
    <<Schema>>
    <<abstract>>
    jobs  : [id] ➞ GlobalJob
}
GlobalClient *-- "0..*" GlobalJob : jobs

class GlobalJob:::cssEntity {
    <<Entity · id>>
    id           : int64
    title        : string
    completed?   : boolean
    created?     : DateTime
    description? : string
}


```

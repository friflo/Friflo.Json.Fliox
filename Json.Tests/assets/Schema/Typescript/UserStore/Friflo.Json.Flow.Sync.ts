
export type TaskType =
    | "read"
    | "query"
    | "create"
    | "update"
    | "patch"
    | "delete"
    | "message"
    | "subscribeChanges"
    | "subscribeMessage"
    | "error"
;

export type Change =
    | "create"
    | "update"
    | "patch"
    | "delete"
;


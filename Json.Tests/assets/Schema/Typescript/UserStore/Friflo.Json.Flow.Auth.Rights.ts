import { TaskType } from "./Friflo.Json.Flow.Sync"
import { Change }   from "./Friflo.Json.Flow.Sync"

export type Right_Union =
    | RightAllow
    | RightTask
    | RightMessage
    | RightSubscribeMessage
    | RightDatabase
    | RightPredicate
;

export abstract class Right {
    abstract type:
        | "allow"
        | "task"
        | "message"
        | "subscribeMessage"
        | "database"
        | "predicate"
    ;
    description: string;
}

export class RightAllow extends Right {
    type:        "allow";
    grant:       boolean;
    description: string;
}

export class RightTask extends Right {
    type:        "task";
    types:       TaskType[];
    description: string;
}

export class RightMessage extends Right {
    type:        "message";
    names:       string[];
    description: string;
}

export class RightSubscribeMessage extends Right {
    type:        "subscribeMessage";
    names:       string[];
    description: string;
}

export class RightDatabase extends Right {
    type:        "database";
    containers:  { [key: string]: ContainerAccess };
    description: string;
}

export class ContainerAccess {
    operations:       OperationType[];
    subscribeChanges: Change[];
}

export type OperationType =
    | "create"
    | "update"
    | "delete"
    | "patch"
    | "read"
    | "query"
    | "mutate"
    | "full"
;

export class RightPredicate extends Right {
    type:        "predicate";
    names:       string[];
    description: string;
}


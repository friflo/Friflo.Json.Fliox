import { FilterOperation_Union } from "./Friflo.Json.Flow.Transform"
import { JsonPatch_Union } from "./Friflo.Json.Flow.Transform"

export class DatabaseMessage {
    req:  DatabaseRequest_Union;
    resp: DatabaseResponse_Union;
    ev:   DatabaseEvent_Union;
}

export type DatabaseRequest_Union =
    | SyncRequest
;

export abstract class DatabaseRequest {
    abstract type:
        | "sync"
    ;
    reqId: number;
}

export class SyncRequest extends DatabaseRequest {
    type:     "sync";
    reqId:    number;
    clientId: string;
    eventAck: number;
    token:    string;
    tasks:    DatabaseTask_Union[];
}

export type DatabaseTask_Union =
    | CreateEntities
    | UpdateEntities
    | ReadEntitiesList
    | QueryEntities
    | PatchEntities
    | DeleteEntities
    | SendMessage
    | SubscribeChanges
    | SubscribeMessage
;

export abstract class DatabaseTask {
    abstract task:
        | "create"
        | "update"
        | "read"
        | "query"
        | "patch"
        | "delete"
        | "message"
        | "subscribeChanges"
        | "subscribeMessage"
    ;
}

export class CreateEntities extends DatabaseTask {
    task:      "create";
    container: string;
    entities:  { string: EntityValue };
}

export class EntityValue {
    value: object;
    error: EntityError;
}

export class EntityError {
    type:    EntityErrorType;
    message: string;
}

export type EntityErrorType =
    | "Undefined"
    | "ParseError"
    | "ReadError"
    | "WriteError"
    | "DeleteError"
    | "PatchError"
;

export class UpdateEntities extends DatabaseTask {
    task:      "update";
    container: string;
    entities:  { string: EntityValue };
}

export class ReadEntitiesList extends DatabaseTask {
    task:      "read";
    container: string;
    reads:     ReadEntities[];
}

export class ReadEntities {
    ids:        string[];
    references: References[];
}

export class References {
    selector:   string;
    container:  string;
    references: References[];
}

export class QueryEntities extends DatabaseTask {
    task:       "query";
    container:  string;
    filterLinq: string;
    filter:     FilterOperation_Union;
    references: References[];
}

export class PatchEntities extends DatabaseTask {
    task:      "patch";
    container: string;
    patches:   { string: EntityPatch };
}

export class EntityPatch {
    patches: JsonPatch_Union[];
}

export class DeleteEntities extends DatabaseTask {
    task:      "delete";
    container: string;
    ids:       string[];
}

export class SendMessage extends DatabaseTask {
    task:  "message";
    name:  string;
    value: object;
}

export class SubscribeChanges extends DatabaseTask {
    task:      "subscribeChanges";
    container: string;
    changes:   Change[];
    filter:    FilterOperation_Union;
}

export type Change =
    | "create"
    | "update"
    | "patch"
    | "delete"
;

export class SubscribeMessage extends DatabaseTask {
    task:   "subscribeMessage";
    name:   string;
    remove: boolean;
}

export type DatabaseResponse_Union =
    | SyncResponse
    | ErrorResponse
;

export abstract class DatabaseResponse {
    abstract type:
        | "sync"
        | "error"
    ;
    reqId: number;
}

export class SyncResponse extends DatabaseResponse {
    type:         "sync";
    reqId:        number;
    error:        ErrorResponse;
    tasks:        TaskResult_Union[];
    results:      { string: ContainerEntities };
    createErrors: { string: EntityErrors };
    updateErrors: { string: EntityErrors };
    patchErrors:  { string: EntityErrors };
    deleteErrors: { string: EntityErrors };
}

export class ErrorResponse extends DatabaseResponse {
    type:    "error";
    reqId:   number;
    message: string;
}

export type TaskResult_Union =
    | CreateEntitiesResult
    | UpdateEntitiesResult
    | ReadEntitiesListResult
    | QueryEntitiesResult
    | PatchEntitiesResult
    | DeleteEntitiesResult
    | SendMessageResult
    | SubscribeChangesResult
    | SubscribeMessageResult
    | TaskErrorResult
;

export abstract class TaskResult {
    abstract task:
        | "create"
        | "update"
        | "read"
        | "query"
        | "patch"
        | "delete"
        | "message"
        | "subscribeChanges"
        | "subscribeMessage"
        | "error"
    ;
}

export class CreateEntitiesResult extends TaskResult {
    task:  "create";
    Error: CommandError;
}

export class CommandError {
    message: string;
}

export class UpdateEntitiesResult extends TaskResult {
    task:  "update";
    Error: CommandError;
}

export class ReadEntitiesListResult extends TaskResult {
    task:  "read";
    reads: ReadEntitiesResult[];
}

export class ReadEntitiesResult {
    Error:      CommandError;
    references: ReferencesResult[];
}

export class ReferencesResult {
    error:      string;
    container:  string;
    ids:        string[];
    references: ReferencesResult[];
}

export class QueryEntitiesResult extends TaskResult {
    task:       "query";
    Error:      CommandError;
    container:  string;
    filterLinq: string;
    ids:        string[];
    references: ReferencesResult[];
}

export class PatchEntitiesResult extends TaskResult {
    task:  "patch";
    Error: CommandError;
}

export class DeleteEntitiesResult extends TaskResult {
    task:  "delete";
    Error: CommandError;
}

export class SendMessageResult extends TaskResult {
    task:   "message";
    Error:  CommandError;
    result: object;
}

export class SubscribeChangesResult extends TaskResult {
    task: "subscribeChanges";
}

export class SubscribeMessageResult extends TaskResult {
    task: "subscribeMessage";
}

export class TaskErrorResult extends TaskResult {
    task:       "error";
    type:       TaskErrorResultType;
    message:    string;
    stacktrace: string;
}

export type TaskErrorResultType =
    | "None"
    | "UnhandledException"
    | "DatabaseError"
    | "InvalidTask"
    | "PermissionDenied"
    | "SyncError"
;

export class ContainerEntities {
    container: string;
    entities:  { string: EntityValue };
}

export class EntityErrors {
    container: string;
    errors:    { string: EntityError };
}

export type DatabaseEvent_Union =
    | SubscriptionEvent
;

export abstract class DatabaseEvent {
    abstract type:
        | "subscription"
    ;
    seq:      number;
    targetId: string;
    clientId: string;
}

export class SubscriptionEvent extends DatabaseEvent {
    type:     "subscription";
    seq:      number;
    targetId: string;
    clientId: string;
    tasks:    DatabaseTask_Union[];
}


import { Equal }              from "./Friflo.Json.Flow.Transform.Query.Ops"
import { NotEqual }           from "./Friflo.Json.Flow.Transform.Query.Ops"
import { LessThan }           from "./Friflo.Json.Flow.Transform.Query.Ops"
import { LessThanOrEqual }    from "./Friflo.Json.Flow.Transform.Query.Ops"
import { GreaterThan }        from "./Friflo.Json.Flow.Transform.Query.Ops"
import { GreaterThanOrEqual } from "./Friflo.Json.Flow.Transform.Query.Ops"
import { And }                from "./Friflo.Json.Flow.Transform.Query.Ops"
import { Or }                 from "./Friflo.Json.Flow.Transform.Query.Ops"
import { TrueLiteral }        from "./Friflo.Json.Flow.Transform.Query.Ops"
import { FalseLiteral }       from "./Friflo.Json.Flow.Transform.Query.Ops"
import { Not }                from "./Friflo.Json.Flow.Transform.Query.Ops"
import { Any }                from "./Friflo.Json.Flow.Transform.Query.Ops"
import { All }                from "./Friflo.Json.Flow.Transform.Query.Ops"
import { Contains }           from "./Friflo.Json.Flow.Transform.Query.Ops"
import { StartsWith }         from "./Friflo.Json.Flow.Transform.Query.Ops"
import { EndsWith }           from "./Friflo.Json.Flow.Transform.Query.Ops"
import { Field }              from "./Friflo.Json.Flow.Transform.Query.Ops"
import { StringLiteral }      from "./Friflo.Json.Flow.Transform.Query.Ops"
import { DoubleLiteral }      from "./Friflo.Json.Flow.Transform.Query.Ops"
import { LongLiteral }        from "./Friflo.Json.Flow.Transform.Query.Ops"
import { NullLiteral }        from "./Friflo.Json.Flow.Transform.Query.Ops"
import { Abs }                from "./Friflo.Json.Flow.Transform.Query.Ops"
import { Ceiling }            from "./Friflo.Json.Flow.Transform.Query.Ops"
import { Floor }              from "./Friflo.Json.Flow.Transform.Query.Ops"
import { Exp }                from "./Friflo.Json.Flow.Transform.Query.Ops"
import { Log }                from "./Friflo.Json.Flow.Transform.Query.Ops"
import { Sqrt }               from "./Friflo.Json.Flow.Transform.Query.Ops"
import { Negate }             from "./Friflo.Json.Flow.Transform.Query.Ops"
import { Add }                from "./Friflo.Json.Flow.Transform.Query.Ops"
import { Subtract }           from "./Friflo.Json.Flow.Transform.Query.Ops"
import { Multiply }           from "./Friflo.Json.Flow.Transform.Query.Ops"
import { Divide }             from "./Friflo.Json.Flow.Transform.Query.Ops"
import { Min }                from "./Friflo.Json.Flow.Transform.Query.Ops"
import { Max }                from "./Friflo.Json.Flow.Transform.Query.Ops"
import { Sum }                from "./Friflo.Json.Flow.Transform.Query.Ops"
import { Average }            from "./Friflo.Json.Flow.Transform.Query.Ops"
import { Count }              from "./Friflo.Json.Flow.Transform.Query.Ops"

export type FilterOperation_Union =
    | Equal
    | NotEqual
    | LessThan
    | LessThanOrEqual
    | GreaterThan
    | GreaterThanOrEqual
    | And
    | Or
    | TrueLiteral
    | FalseLiteral
    | Not
    | Any
    | All
    | Contains
    | StartsWith
    | EndsWith
;

export abstract class FilterOperation {
    abstract op:
        | "equal"
        | "notEqual"
        | "lessThan"
        | "lessThanOrEqual"
        | "greaterThan"
        | "greaterThanOrEqual"
        | "and"
        | "or"
        | "true"
        | "false"
        | "not"
        | "any"
        | "all"
        | "contains"
        | "startsWith"
        | "endsWith"
    ;
}

export type Operation_Union =
    | Field
    | StringLiteral
    | DoubleLiteral
    | LongLiteral
    | NullLiteral
    | Abs
    | Ceiling
    | Floor
    | Exp
    | Log
    | Sqrt
    | Negate
    | Add
    | Subtract
    | Multiply
    | Divide
    | Min
    | Max
    | Sum
    | Average
    | Count
    | Equal
    | NotEqual
    | LessThan
    | LessThanOrEqual
    | GreaterThan
    | GreaterThanOrEqual
    | And
    | Or
    | TrueLiteral
    | FalseLiteral
    | Not
    | Any
    | All
    | Contains
    | StartsWith
    | EndsWith
;

export abstract class Operation {
    abstract op:
        | "field"
        | "string"
        | "double"
        | "int64"
        | "null"
        | "abs"
        | "ceiling"
        | "floor"
        | "exp"
        | "log"
        | "sqrt"
        | "negate"
        | "add"
        | "subtract"
        | "multiply"
        | "divide"
        | "min"
        | "max"
        | "sum"
        | "average"
        | "count"
        | "equal"
        | "notEqual"
        | "lessThan"
        | "lessThanOrEqual"
        | "greaterThan"
        | "greaterThanOrEqual"
        | "and"
        | "or"
        | "true"
        | "false"
        | "not"
        | "any"
        | "all"
        | "contains"
        | "startsWith"
        | "endsWith"
    ;
}

export type JsonPatch_Union =
    | PatchReplace
    | PatchAdd
    | PatchRemove
    | PatchCopy
    | PatchMove
    | PatchTest
;

export abstract class JsonPatch {
    abstract op:
        | "replace"
        | "add"
        | "remove"
        | "copy"
        | "move"
        | "test"
    ;
}

export class PatchReplace extends JsonPatch {
    op:    "replace";
    path:  string;
    value: object;
}

export class PatchAdd extends JsonPatch {
    op:    "add";
    path:  string;
    value: object;
}

export class PatchRemove extends JsonPatch {
    op:   "remove";
    path: string;
}

export class PatchCopy extends JsonPatch {
    op:   "copy";
    path: string;
    from: string;
}

export class PatchMove extends JsonPatch {
    op:   "move";
    path: string;
    from: string;
}

export class PatchTest extends JsonPatch {
    op:    "test";
    path:  string;
    value: object;
}


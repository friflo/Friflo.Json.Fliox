
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


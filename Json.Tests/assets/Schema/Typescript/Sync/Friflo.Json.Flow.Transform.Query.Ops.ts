import { Operation }       from "./Friflo.Json.Flow.Transform"
import { FilterOperation } from "./Friflo.Json.Flow.Transform"

export class Equal extends FilterOperation {
    op:    "equal";
    left:  Operation;
    right: Operation;
}

export class Field extends Operation {
    op:   "field";
    name: string;
}

export class StringLiteral extends Operation {
    op:    "string";
    value: string;
}

export class DoubleLiteral extends Operation {
    op:    "double";
    value: number;
}

export class LongLiteral extends Operation {
    op:    "int64";
    value: number;
}

export class NullLiteral extends Operation {
    op: "null";
}

export class Abs extends Operation {
    op:    "abs";
    value: Operation;
}

export class Ceiling extends Operation {
    op:    "ceiling";
    value: Operation;
}

export class Floor extends Operation {
    op:    "floor";
    value: Operation;
}

export class Exp extends Operation {
    op:    "exp";
    value: Operation;
}

export class Log extends Operation {
    op:    "log";
    value: Operation;
}

export class Sqrt extends Operation {
    op:    "sqrt";
    value: Operation;
}

export class Negate extends Operation {
    op:    "negate";
    value: Operation;
}

export class Add extends Operation {
    op:    "add";
    left:  Operation;
    right: Operation;
}

export class Subtract extends Operation {
    op:    "subtract";
    left:  Operation;
    right: Operation;
}

export class Multiply extends Operation {
    op:    "multiply";
    left:  Operation;
    right: Operation;
}

export class Divide extends Operation {
    op:    "divide";
    left:  Operation;
    right: Operation;
}

export class Min extends Operation {
    op:    "min";
    field: Field;
    arg:   string;
    array: Operation;
}

export class Max extends Operation {
    op:    "max";
    field: Field;
    arg:   string;
    array: Operation;
}

export class Sum extends Operation {
    op:    "sum";
    field: Field;
    arg:   string;
    array: Operation;
}

export class Average extends Operation {
    op:    "average";
    field: Field;
    arg:   string;
    array: Operation;
}

export class Count extends Operation {
    op:    "count";
    field: Field;
}

export class NotEqual extends FilterOperation {
    op:    "notEqual";
    left:  Operation;
    right: Operation;
}

export class LessThan extends FilterOperation {
    op:    "lessThan";
    left:  Operation;
    right: Operation;
}

export class LessThanOrEqual extends FilterOperation {
    op:    "lessThanOrEqual";
    left:  Operation;
    right: Operation;
}

export class GreaterThan extends FilterOperation {
    op:    "greaterThan";
    left:  Operation;
    right: Operation;
}

export class GreaterThanOrEqual extends FilterOperation {
    op:    "greaterThanOrEqual";
    left:  Operation;
    right: Operation;
}

export class And extends FilterOperation {
    op:       "and";
    operands: FilterOperation[];
}

export class Or extends FilterOperation {
    op:       "or";
    operands: FilterOperation[];
}

export class TrueLiteral extends FilterOperation {
    op: "true";
}

export class FalseLiteral extends FilterOperation {
    op: "false";
}

export class Not extends FilterOperation {
    op:      "not";
    operand: FilterOperation;
}

export class Any extends FilterOperation {
    op:        "any";
    field:     Field;
    arg:       string;
    predicate: FilterOperation;
}

export class All extends FilterOperation {
    op:        "all";
    field:     Field;
    arg:       string;
    predicate: FilterOperation;
}

export class Contains extends FilterOperation {
    op:    "contains";
    left:  Operation;
    right: Operation;
}

export class StartsWith extends FilterOperation {
    op:    "startsWith";
    left:  Operation;
    right: Operation;
}

export class EndsWith extends FilterOperation {
    op:    "endsWith";
    left:  Operation;
    right: Operation;
}


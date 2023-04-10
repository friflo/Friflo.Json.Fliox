[generated-by]: https://github.com/friflo/Friflo.Json.Fliox#schema

```mermaid
classDiagram
direction LR

class Operation {
    <<abstract>>
}

Operation <|-- FilterOperation
class FilterOperation {
    <<abstract>>
}

FilterOperation <|-- BinaryBoolOp
class BinaryBoolOp {
    <<abstract>>
    left   : Operation
    right  : Operation
}
BinaryBoolOp *-- "1" Operation : left
BinaryBoolOp *-- "1" Operation : right

BinaryBoolOp <|-- Equal
class Equal {
    op     : "equal"
}

Operation <|-- Field
class Field {
    op    : "field"
    name  : string
}

Operation <|-- Literal
class Literal {
    <<abstract>>
}

Literal <|-- StringLiteral
class StringLiteral {
    op     : "string"
    value  : string
}

Literal <|-- DoubleLiteral
class DoubleLiteral {
    op     : "double"
    value  : double
}

Literal <|-- LongLiteral
class LongLiteral {
    op     : "int64"
    value  : int64
}

Literal <|-- NullLiteral
class NullLiteral {
    op  : "null"
}

Literal <|-- PiLiteral
class PiLiteral {
    op  : "PI"
}

Literal <|-- EulerLiteral
class EulerLiteral {
    op  : "E"
}

Literal <|-- TauLiteral
class TauLiteral {
    op  : "Tau"
}

Operation <|-- UnaryArithmeticOp
class UnaryArithmeticOp {
    <<abstract>>
    value  : Operation
}
UnaryArithmeticOp *-- "1" Operation : value

UnaryArithmeticOp <|-- Abs
class Abs {
    op     : "abs"
}

UnaryArithmeticOp <|-- Ceiling
class Ceiling {
    op     : "ceiling"
}

UnaryArithmeticOp <|-- Floor
class Floor {
    op     : "floor"
}

UnaryArithmeticOp <|-- Exp
class Exp {
    op     : "exp"
}

UnaryArithmeticOp <|-- Log
class Log {
    op     : "log"
}

UnaryArithmeticOp <|-- Sqrt
class Sqrt {
    op     : "sqrt"
}

UnaryArithmeticOp <|-- Negate
class Negate {
    op     : "negate"
}

Operation <|-- BinaryArithmeticOp
class BinaryArithmeticOp {
    <<abstract>>
    left   : Operation
    right  : Operation
}
BinaryArithmeticOp *-- "1" Operation : left
BinaryArithmeticOp *-- "1" Operation : right

BinaryArithmeticOp <|-- Add
class Add {
    op     : "add"
}

BinaryArithmeticOp <|-- Subtract
class Subtract {
    op     : "subtract"
}

BinaryArithmeticOp <|-- Multiply
class Multiply {
    op     : "multiply"
}

BinaryArithmeticOp <|-- Divide
class Divide {
    op     : "divide"
}

BinaryArithmeticOp <|-- Modulo
class Modulo {
    op     : "modulo"
}

Operation <|-- BinaryAggregateOp
class BinaryAggregateOp {
    <<abstract>>
    field  : Field
    arg    : string
    array  : Operation
}
BinaryAggregateOp *-- "1" Field : field
BinaryAggregateOp *-- "1" Operation : array

BinaryAggregateOp <|-- Min
class Min {
    op     : "min"
}

BinaryAggregateOp <|-- Max
class Max {
    op     : "max"
}

BinaryAggregateOp <|-- Sum
class Sum {
    op     : "sum"
}

BinaryAggregateOp <|-- Average
class Average {
    op     : "average"
}

Operation <|-- UnaryAggregateOp
class UnaryAggregateOp {
    <<abstract>>
    field  : Field
}
UnaryAggregateOp *-- "1" Field : field

UnaryAggregateOp <|-- Count
class Count {
    op     : "count"
}

BinaryBoolOp <|-- NotEqual
class NotEqual {
    op     : "notEqual"
}

BinaryBoolOp <|-- Less
class Less {
    op     : "less"
}

BinaryBoolOp <|-- LessOrEqual
class LessOrEqual {
    op     : "lessOrEqual"
}

BinaryBoolOp <|-- Greater
class Greater {
    op     : "greater"
}

BinaryBoolOp <|-- GreaterOrEqual
class GreaterOrEqual {
    op     : "greaterOrEqual"
}

FilterOperation <|-- BinaryLogicalOp
class BinaryLogicalOp {
    <<abstract>>
    operands  : FilterOperation[]
}
BinaryLogicalOp *-- "0..*" FilterOperation : operands

BinaryLogicalOp <|-- And
class And {
    op        : "and"
}

BinaryLogicalOp <|-- Or
class Or {
    op        : "or"
}

FilterOperation <|-- TrueLiteral
class TrueLiteral {
    op  : "true"
}

FilterOperation <|-- FalseLiteral
class FalseLiteral {
    op  : "false"
}

FilterOperation <|-- UnaryLogicalOp
class UnaryLogicalOp {
    <<abstract>>
    operand  : FilterOperation
}
UnaryLogicalOp *-- "1" FilterOperation : operand

UnaryLogicalOp <|-- Not
class Not {
    op       : "not"
}

Operation <|-- Lambda
class Lambda {
    op    : "lambda"
    arg   : string
    body  : Operation
}
Lambda *-- "1" Operation : body

FilterOperation <|-- Filter
class Filter {
    op    : "filter"
    arg   : string
    body  : FilterOperation
}
Filter *-- "1" FilterOperation : body

FilterOperation <|-- BinaryQuantifyOp
class BinaryQuantifyOp {
    <<abstract>>
    field      : Field
    arg        : string
    predicate  : FilterOperation
}
BinaryQuantifyOp *-- "1" Field : field
BinaryQuantifyOp *-- "1" FilterOperation : predicate

BinaryQuantifyOp <|-- Any
class Any {
    op         : "any"
}

BinaryQuantifyOp <|-- All
class All {
    op         : "all"
}

Operation <|-- CountWhere
class CountWhere {
    op         : "countWhere"
    field      : Field
    arg        : string
    predicate  : FilterOperation
}
CountWhere *-- "1" Field : field
CountWhere *-- "1" FilterOperation : predicate

BinaryBoolOp <|-- Contains
class Contains {
    op     : "contains"
}

BinaryBoolOp <|-- StartsWith
class StartsWith {
    op     : "startsWith"
}

BinaryBoolOp <|-- EndsWith
class EndsWith {
    op     : "endsWith"
}

Operation <|-- Length
class Length {
    op     : "length"
    value  : Operation
}
Length *-- "1" Operation : value


```

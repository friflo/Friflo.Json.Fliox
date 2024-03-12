[generated-by]: https://github.com/friflo/Friflo.Json.Fliox#schema

```mermaid
classDiagram
direction LR

class PocStore:::cssSchema {
    <<Schema>>
    <<abstract>>
    orders       : [id] ➞ Order
    customers    : [id] ➞ Customer
    articles     : [id] ➞ Article
    articles2    : [id] ➞ Article
    producers    : [id] ➞ Producer
    employees    : [id] ➞ Employee
    types        : [id] ➞ TestType
    nonClsTypes  : [id] ➞ NonClsType
    keyName      : [testId] ➞ TestKeyName
}
PocStore *-- "0..*" Order : orders
PocStore *-- "0..*" Customer : customers
PocStore *-- "0..*" Article : articles
PocStore *-- "0..*" Article : articles2
PocStore *-- "0..*" Producer : producers
PocStore *-- "0..*" Employee : employees
PocStore *-- "0..*" TestType : types
PocStore *-- "0..*" NonClsType : nonClsTypes
PocStore *-- "0..*" TestKeyName : keyName

class Order:::cssEntity {
    <<Entity · id>>
    id        : string
    customer? : string
    created   : DateTime
    items?    : OrderItem[]
}
Order o.. "0..1" Customer : customer
Order *-- "0..*" OrderItem : items

class Customer:::cssEntity {
    <<Entity · id>>
    id    : string
    name  : string
}

class Article:::cssEntity {
    <<Entity · id>>
    id        : string
    name      : string
    producer? : string
}
Article o.. "0..1" Producer : producer

class Producer:::cssEntity {
    <<Entity · id>>
    id         : string
    name       : string
    employees? : string[]
}
Producer o.. "0..*" Employee : employees

class Employee:::cssEntity {
    <<Entity · id>>
    id         : string
    firstName  : string
    lastName?  : string
}

class PocEntity {
    <<abstract>>
    id  : string
}

PocEntity <|-- TestType
class TestType:::cssEntity {
    <<Entity · id>>
    dateTime          : DateTime
    dateTimeNull?     : DateTime
    bigInt            : BigInteger
    bigIntNull?       : BigInteger
    boolean           : boolean
    booleanNull?      : boolean
    uint8             : uint8
    uint8Null?        : uint8
    int16             : int16
    int16Null?        : int16
    int32             : int32
    int32Null?        : int32
    int64             : int64
    int64Null?        : int64
    float32           : float
    float32Null?      : float
    float64           : double
    float64Null?      : double
    pocStruct         : PocStruct
    pocStructNull?    : PocStruct
    intArray          : int32[]
    intArrayNull?     : int32[]
    intNullArray?     : (int32 | null)[]
    jsonValue?        : any
    derivedClass      : DerivedClass
    derivedClassNull? : DerivedClass
    testEnum          : TestEnum
    testEnumNull?     : TestEnum
}
TestType *-- "1" PocStruct : pocStruct
TestType *-- "0..1" PocStruct : pocStructNull
TestType *-- "1" DerivedClass : derivedClass
TestType *-- "0..1" DerivedClass : derivedClassNull
TestType *-- "1" TestEnum : testEnum
TestType *-- "0..1" TestEnum : testEnumNull

class NonClsType:::cssEntity {
    <<Entity · id>>
    id          : string
    int8        : int8
    uint16      : uint16
    uint32      : uint32
    uint64      : uint64
    int8Null?   : int8
    uint16Null? : uint16
    uint32Null? : uint32
    uint64Null? : uint64
}

class TestKeyName:::cssEntity {
    <<Entity · testId>>
    testId  : string
    value?  : string
}

class OrderItem {
    article  : string
    amount   : int32
    name?    : string
}
OrderItem o.. "1" Article : article

class PocStruct {
    value  : int32
}

OrderItem <|-- DerivedClass
class DerivedClass {
    derivedVal  : int32
}

class TestEnum:::cssEnum {
    <<enumeration>>
    NONE
    e1
    e2
}



```

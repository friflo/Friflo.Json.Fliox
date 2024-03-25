// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Schema.JSON;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Fliox.Schema.Validation;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable UnusedVariable
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Schema.Validation
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class PocStoreValidation : LeakTestsFixture
    {
        private static readonly string  JsonSchemaFolder    = CommonUtils.GetBasePath() + "assets~/Schema/JSON/PocStore";
        
        [Test]
        public static void ValidateByJsonSchema() {
            var schemas                 = JsonTypeSchema.ReadSchemas(JsonSchemaFolder);
            var jsonSchema              = new JsonTypeSchema(schemas);
            using (var validator        = new TypeValidator()) {
                var validationSet   = new ValidationSet(jsonSchema);
                var test = new TestTypes {
                    testType    = jsonSchema.TypeAsValidationType<TestType>     (validationSet, "UnitTest.Fliox.Client"),
                    orderType   = jsonSchema.TypeAsValidationType<Order>        (validationSet, "UnitTest.Fliox.Client"),
                    articleType = jsonSchema.TypeAsValidationType<Article>      (validationSet, "UnitTest.Fliox.Client"),
                    nonClsType  = jsonSchema.TypeAsValidationType<NonClsType>   (validationSet, "UnitTest.Fliox.Client"),
                };
                ValidateSuccess(validator, test);
                ValidateFailure(validator, test);
            }
        }
        
        [Test]
        public static void ValidateByTypes() {
            var nativeSchema = NativeTypeSchema.Create(typeof(PocStore));
            using (var validator    = new TypeValidator(qualifiedTypeErrors: true)) { // true -> ensure API available
                var validationSet   = new ValidationSet(nativeSchema);
                validator.qualifiedTypeErrors = false; // ensure API available
                var test = new TestTypes {
                    testType    = nativeSchema.TypeAsValidationType<TestType>   (validationSet),
                    orderType   = nativeSchema.TypeAsValidationType<Order>      (validationSet),
                    articleType = nativeSchema.TypeAsValidationType<Article>    (validationSet),
                    nonClsType  = nativeSchema.TypeAsValidationType<NonClsType> (validationSet),
                };
                ValidateSuccess(validator, test);
                ValidateFailure(validator, test);
            }
        }
        
        private static void ValidateSuccess(TypeValidator validator, TestTypes test)
        {
            var result  = validator.ValidateObject(test.orderValid,         test.orderType, out var err1);
            IsTrue(result);
            
            result      = validator.ValidateObject(test.testTypeValid,      test.testType,  out var err2);
            IsTrue(result);
            
            result      =  validator.ValidateObject(test.testTypeValidNull, test.testType,  out var err3);
            IsTrue(result);
            
            result      = validator.ValidateObject(test.nonClsValid,        test.nonClsType,  out var err4);
            IsTrue(result);
        }
        
        private static void ValidateFailure(TypeValidator validator, TestTypes test)
        {
            IsFalse(validator.ValidateObject("{\"bigInt\": null }",         test.testType, out string error));
            AreEqual("Required property must not be null. at TestType > bigInt, pos: 15", error);
            
            IsFalse(validator.ValidateObject("{\"uint8\": true }",          test.testType, out error));
            AreEqual("Incorrect type. was: true, expect: uint8 at TestType > uint8, pos: 14", error);
            
            IsFalse(validator.ValidateObject("{\"uint8\": \"abc\" }",       test.testType, out error));
            AreEqual("Incorrect type. was: 'abc', expect: uint8 at TestType > uint8, pos: 15", error);

            IsFalse(validator.ValidateObject("{\"uint8\": 1.5 }",           test.testType, out error));
            AreEqual("Invalid integer. was: 1.5, expect: uint8 at TestType > uint8, pos: 13", error);
            
            IsFalse(validator.ValidateObject("{\"uint8\": [] }",            test.testType, out error));
            AreEqual("Incorrect type. was: array, expect: uint8 at TestType > uint8[], pos: 11", error);
            
            IsFalse(validator.ValidateObject("{\"uint8\": {} }",            test.testType, out error));
            AreEqual("Incorrect type. was: object, expect: uint8 at TestType > uint8, pos: 11", error);
            
            IsFalse(validator.ValidateObject("{\"xxx\": {} }",              test.testType, out error));
            AreEqual("Unknown property: 'xxx' at TestType > xxx, pos: 9", error);
            
            IsFalse(validator.ValidateObject("{\"yyy\": null }",            test.testType, out error));
            AreEqual("Unknown property: 'yyy' at TestType > yyy, pos: 12", error);
            
            IsFalse(validator.ValidateObject("{\"zzz\": [] }",              test.testType, out error));
            AreEqual("Unknown property: 'zzz' at TestType > zzz[], pos: 9", error);
            
            // missing T in dateTime. correct: "2021-07-22T06:00:00.000Z"
            IsFalse(validator.ValidateObject("{ \"dateTime\": \"2021-07-22 06:00:00.000Z\" }",   test.testType, out error));
            AreEqual("Invalid DateTime: '2021-07-22 06:00:00.000Z' at TestType > dateTime, pos: 40", error);
            
            IsFalse(validator.ValidateObject("{ \"bigInt\": \"abc\" }",     test.testType, out error));
            AreEqual("Invalid BigInteger: 'abc' at TestType > bigInt, pos: 17", error);
            
            // --- integer types
            IsFalse(validator.ValidateObject("{ \"uint8\": -1 }",                       test.testType, out error));
            AreEqual("Integer out of range. was: -1, expect: uint8 at TestType > uint8, pos: 13", error);
            IsFalse(validator.ValidateObject("{ \"uint8\": 256 }",                      test.testType, out error));
            AreEqual("Integer out of range. was: 256, expect: uint8 at TestType > uint8, pos: 14", error);
            
            IsFalse(validator.ValidateObject("{ \"int16\": -32769 }",                   test.testType, out error));
            AreEqual("Integer out of range. was: -32769, expect: int16 at TestType > int16, pos: 17", error);
            IsFalse(validator.ValidateObject("{ \"int16\": 32768 }",                    test.testType, out error));
            AreEqual("Integer out of range. was: 32768, expect: int16 at TestType > int16, pos: 16", error);
            
            IsFalse(validator.ValidateObject("{ \"int32\": -2147483649 }",              test.testType, out error));
            AreEqual("Integer out of range. was: -2147483649, expect: int32 at TestType > int32, pos: 22", error);
            IsFalse(validator.ValidateObject("{ \"int32\": 2147483648 }",               test.testType, out error));
            AreEqual("Integer out of range. was: 2147483648, expect: int32 at TestType > int32, pos: 21", error);
            
            
            // NON_CLS - integer
            IsFalse(validator.ValidateObject("{ \"int8\": -129 }",                      test.nonClsType, out error));
            AreEqual("Integer out of range. was: -129, expect: int8 at NonClsType > int8, pos: 14", error);
            IsFalse(validator.ValidateObject("{ \"int8\": 128 }",                       test.nonClsType, out error));
            AreEqual("Integer out of range. was: 128, expect: int8 at NonClsType > int8, pos: 13", error);
            
            IsFalse(validator.ValidateObject("{ \"uint16\": -1 }",                      test.nonClsType, out error));
            AreEqual("Integer out of range. was: -1, expect: uint16 at NonClsType > uint16, pos: 14", error);
            IsFalse(validator.ValidateObject("{ \"uint16\": 65536 }",                   test.nonClsType, out error));
            AreEqual("Integer out of range. was: 65536, expect: uint16 at NonClsType > uint16, pos: 17", error);
            
            IsFalse(validator.ValidateObject("{ \"uint32\": -1 }",                      test.nonClsType, out error));
            AreEqual("Integer out of range. was: -1, expect: uint32 at NonClsType > uint32, pos: 14", error);
            IsFalse(validator.ValidateObject("{ \"uint32\": 4294967296 }",              test.nonClsType, out error));
            AreEqual("Integer out of range. was: 4294967296, expect: uint32 at NonClsType > uint32, pos: 22", error);
            
            IsFalse(validator.ValidateObject("{ \"uint64\": -1 }",                      test.nonClsType, out error));
            AreEqual("Invalid integer. was: -1, expect: uint64 at NonClsType > uint64, pos: 14", error);
            IsFalse(validator.ValidateObject("{ \"uint64\": 18446744073709551616 }",    test.nonClsType, out error));
            AreEqual("Invalid integer. was: 18446744073709551616, expect: uint64 at NonClsType > uint64, pos: 32", error);
            
            // --- Article
            IsFalse(validator.ValidateObject("{ \"id\": \"article\" }",                 test.articleType, out error));
            AreEqual("Missing required fields: [name] at Article > (root), pos: 19", error);
            
            IsFalse(validator.ValidateObject("{ \"id\": \"article\", \"name\": null }", test.articleType, out error));
            AreEqual("Required property must not be null. at Article > name, pos: 31", error);
        }
        
        private class TestTypes {
            internal    ValidationType  testType;
            internal    ValidationType  orderType;
            internal    ValidationType  articleType;
            internal    ValidationType  nonClsType;
            
            internal    readonly string orderValid      = AsJson("{ 'id': 'order-1', 'created': '2021-07-22T06:00:00.000Z' }");
            
            
            internal    readonly string testTypeValid   = AsJson(
@"{
    'id': 'type-1',
    'dateTime': '2021-07-22T06:00:00.000Z',
    'bigInt': '0',
    'boolean': false,
    'uint8': 0,
    'int16': 0,
    'int32': 0,
    'int64': 0,
    'float32': 0.0,
    'float64': 0.0,
    'pocStruct': {
        'value': 0
    },
    'intArray': [
    ],
    'derivedClass': {
        'derivedVal': 0,
        'article': 'article-1',
        'amount': 0
    },
    'testEnum': 'e1'
}");
            
            internal    readonly string testTypeValidNull = AsJson(
@"{
    'id': 'type-1',
    'dateTime': '2021-07-22T06:00:00.000Z',
    'dateTimeNull': null,
    'bigInt': '0',
    'bigIntNull': null,
    'boolean': false,
    'booleanNull': null,
    'uint8': 0,
    'uint8Null': null,
    'int16': 0,
    'int16Null': null,
    'int32': 0,
    'int32Null': null,
    'int64': 0,
    'int64Null': null,
    'float32': 0.0,
    'float32Null': null,
    'float64': 0.0,
    'float64Null': null,
    'pocStruct': {
        'value': 0
    },
    'pocStructNull': null,
    'intArray': [
    ],
    'intArrayNull': null,
    'intNullArray': [333, null],
    'jsonValue': null,
    'derivedClass': {
        'derivedVal': 0,
        'article': 'article-1',
        'amount': 0,
        'name': null
    },
    'derivedClassNull': null,
    'testEnum': 'e1',
    'testEnumNull': null
}");
            
internal    readonly string nonClsValid   = 
@"{
    ""id"": ""cls-1"",
    ""int8"": -127,
    ""uint16"": 1,
    ""uint32"": 2,
    ""uint64"": 3
}";
        }

       
        private static string AsJson (string str) {
            return str.Replace('\'', '"');
        }
    }
}
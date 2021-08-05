// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Schema.JSON;
using Friflo.Json.Flow.Schema.Native;
using Friflo.Json.Flow.Schema.Validation;
using Friflo.Json.Tests.Common.UnitTest.Flow.Graph;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Schema.Validation
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ValidatePocStore : LeakTestsFixture
    {
        static readonly         string  JsonSchemaFolder = CommonUtils.GetBasePath() + "assets/Schema/JSON/PocStore";
        private static readonly Type[]  PocStoreTypes    = { typeof(Order), typeof(Customer), typeof(Article), typeof(Producer), typeof(Employee), typeof(TestType) };
        
        [Test]
        public static void ValidateByJsonSchema() {
            var schemas                 = JsonTypeSchema.ReadSchemas(JsonSchemaFolder);
            var jsonSchema              = new JsonTypeSchema(schemas);
            using (var validationSet    = new ValidationSet(jsonSchema))
            using (var validator        = new JsonValidator()) {
                var test = new TestTypes {
                    testType    = jsonSchema.TypeAsValidationType<TestType>(validationSet, "UnitTest.Flow.Graph")
                };
                ValidateSuccess(validator, test);
                ValidateFailure(validator, test);
            }
        }
        
        [Test]
        public static void ValidateByTypes() {
            using (var typeStore        = CreateTypeStore(PocStoreTypes))
            using (var nativeSchema     = new NativeTypeSchema(typeStore))
            using (var validationSet    = new ValidationSet(nativeSchema))
            using (var validator        = new JsonValidator(true)) {
                validator.qualifiedTypeErrors = false; // ensure API available
                var test = new TestTypes {
                    testType    = nativeSchema.TypeAsValidationType<TestType>(validationSet)
                };
                ValidateSuccess(validator, test);
                ValidateFailure(validator, test);
            }
        }
        
        private static void ValidateSuccess(JsonValidator validator, TestTypes test)
        {
        }
        
        private static void ValidateFailure(JsonValidator validator, TestTypes test)
        {
            IsFalse(validator.ValidateObject("{\"bigInt\": null }",         test.testType, out string error));
            AreEqual("Found null for required field. - type: TestType, path: bigInt, pos: 15", error);
            
            IsFalse(validator.ValidateObject("{\"uint8\": true }",          test.testType, out error));
            AreEqual("Found boolean but expect: Uint8 - type: TestType, path: uint8, pos: 14", error);
            
            IsFalse(validator.ValidateObject("{\"uint8\": \"abc\" }",       test.testType, out error));
            AreEqual("Found string but expect: Uint8 - type: TestType, path: uint8, pos: 15", error);

            IsFalse(validator.ValidateObject("{\"uint8\": 1.5 }",           test.testType, out error));
            AreEqual("Found floating point number but expect Uint8 - type: TestType, path: uint8, pos: 13", error);
            
            IsFalse(validator.ValidateObject("{\"uint8\": [] }",            test.testType, out error));
            AreEqual("Found array but expect: Uint8 - type: TestType, path: uint8[], pos: 11", error);
            
            IsFalse(validator.ValidateObject("{\"uint8\": {} }",            test.testType, out error));
            AreEqual("Found object but expect: Uint8 - type: TestType, path: uint8, pos: 11", error);
            
            IsFalse(validator.ValidateObject("{\"xxx\": {} }",            test.testType, out error));
            AreEqual("Field not found. key: 'xxx' - type: TestType, path: xxx, pos: 9", error);

        }
        
        private class TestTypes {
            internal    ValidationType  testType;
            
            // internal    readonly string testInvalid        = AsJson(@"{'id': 'role-deny', 'rights': [  ] }");
        }

        // --- helper
        private static TypeStore CreateTypeStore (ICollection<Type> types) {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            typeStore.AddMappers(types);
            return typeStore;
        } 
        
        /* private static string AsJson (string str) {
            return str.Replace('\'', '"');
        } */
    }
}
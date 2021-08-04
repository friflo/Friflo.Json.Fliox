// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Schema.JSON;
using Friflo.Json.Flow.Schema.Native;
using Friflo.Json.Flow.Schema.Validation;
using Friflo.Json.Flow.UserAuth;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Schema.Validation
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ValidateUserStore : LeakTestsFixture
    {
        static readonly         string  JsonSchemaFolder = CommonUtils.GetBasePath() + "assets/Schema/JSON/UserStore";
        private static readonly Type[]  UserStoreTypes   = { typeof(Role), typeof(UserCredential), typeof(UserPermission) };
        
        [Test]
        public static void ValidateByJsonSchema() {
            var schemas                 = JsonTypeSchema.ReadSchemas(JsonSchemaFolder);
            var jsonSchema              = new JsonTypeSchema(schemas);
            using (var validationSet    = new ValidationSet(jsonSchema))
            using (var validator        = new JsonValidator()) {
                var test = new TestTypes {
                    roleType    = jsonSchema.TypeAsValidationType<Role>(validationSet, "Friflo.Json.Flow.UserAuth")
                };
                ValidateSuccess(validator, test);
                ValidateFailure(validator, test);
            }
        }
        
        [Test]
        public static void ValidateByTypes() {
            using (var typeStore        = CreateTypeStore(UserStoreTypes))
            using (var nativeSchema     = new NativeTypeSchema(typeStore))
            using (var validationSet    = new ValidationSet(nativeSchema))
            using (var validator        = new JsonValidator()) {
                var test = new TestTypes {
                    roleType    = nativeSchema.TypeAsValidationType<Role>(validationSet)
                };
                ValidateSuccess(validator, test);
                ValidateFailure(validator, test);
            }
        }
        
        private static void ValidateSuccess(JsonValidator validator, TestTypes test)
        {
            IsTrue(validator.ValidateObject     (test.roleDeny,     test.roleType, out _));
            IsTrue(validator.ValidateObject     (test.roleDatabase, test.roleType, out _));
            
            IsTrue(validator.ValidateArray      ("[]",              test.roleType, out _));
            IsTrue(validator.ValidateObjectMap  ("{}",              test.roleType, out _));

            var json = "[" + test.roleDeny + "]";
            IsTrue(validator.ValidateArray      (json,              test.roleType, out _));

            json = "{\"key\": " + test.roleDeny + "}";
            IsTrue(validator.ValidateObjectMap  (json,              test.roleType, out _));
        }
        
        private static void ValidateFailure(JsonValidator validator, TestTypes test)
        {
            IsFalse(validator.ValidateObject("123",                 test.roleType, out var error));
            AreEqual("ValidateObject() expect object. was: ValueNumber - type: Friflo.Json.Flow.UserAuth.Role, path: (root), pos: 3", error);
            
            IsFalse(validator.ValidateObject("{}",                  test.roleType, out error));
            AreEqual("Missing required fields: [id, rights] - type: Friflo.Json.Flow.UserAuth.Role, path: (root), pos: 2", error);
            
            IsFalse(validator.ValidateObject("[]",                  test.roleType, out error));
            AreEqual("ValidateObject() expect object. was: ArrayStart - type: Friflo.Json.Flow.UserAuth.Role, path: [], pos: 1", error);
            
            IsFalse(validator.ValidateObject(test.roleUnknownDisc,  test.roleType, out error));
            AreEqual("Unknown discriminant: xxx - type: Friflo.Json.Flow.Auth.Rights.Right, path: rights[0].type, pos: 41", error);
            
            IsFalse(validator.ValidateObject(test.roleMissingDisc,  test.roleType, out error));
            AreEqual("Expect discriminator string as first member. Expect: 'type', was: 'ObjectEnd' - type: Friflo.Json.Flow.Auth.Rights.Right, path: rights[0], pos: 29", error);

        }
        
        private class TestTypes {
            internal    ValidationType  roleType;
            
            internal    readonly string roleDeny        = AsJson(@"{'id': 'role-deny', 'rights': [  ] }");
            internal    readonly string roleDatabase    = AsJson(
                @"{'id': 'role-database','description': 'test',
                    'rights': [ { 'type': 'database', 'containers': {'Article': { 'operations': ['read', 'update'], 'subscribeChanges': ['update'] }}} ]
                }");
            internal    readonly string roleUnknownDisc = AsJson(@"{'id': 'role', 'rights': [{ 'type': 'xxx' }] }");
            internal    readonly string roleMissingDisc = AsJson(@"{'id': 'role', 'rights': [{ }] }");
        }

        // --- helper
        private static TypeStore CreateTypeStore (ICollection<Type> types) {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            typeStore.AddMappers(types);
            return typeStore;
        } 
        
        private static string AsJson (string str) {
            return str.Replace('\'', '"');
        }
    }
}
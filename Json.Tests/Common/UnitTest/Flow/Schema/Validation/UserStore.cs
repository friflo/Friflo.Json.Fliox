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
    public class UserStoreValidation : LeakTestsFixture
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
                ValidateSuccess         (validator, test);
                ValidateFailure         (validator, test);
                ValidateSuccessNoAlloc  (validator, test);
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
                ValidateSuccess         (validator, test);
                ValidateFailure         (validator, test);
                ValidateSuccessNoAlloc  (validator, test);
            }
        }
        
        private static void ValidateSuccess(JsonValidator validator, TestTypes test)
        {
            SimpleAssert.IsTrue(validator.ValidateObject     (test.roleDeny,        test.roleType, out _));
            SimpleAssert.IsTrue(validator.ValidateObject     (test.roleDatabase,    test.roleType, out _));
            
            SimpleAssert.IsTrue(validator.ValidateArray      ("[]",                 test.roleType, out _));
            SimpleAssert.IsTrue(validator.ValidateObjectMap  ("{}",                 test.roleType, out _));

            SimpleAssert.IsTrue(validator.ValidateArray      (test.roleDenyArray,   test.roleType, out _));
            SimpleAssert.IsTrue(validator.ValidateObjectMap  (test.roleDenyMap,     test.roleType, out _));
        }
        
        private static void ValidateSuccessNoAlloc(JsonValidator validator, TestTypes test) {
            var memLog = new MemoryLogger(2, 1, MemoryLog.Enabled);
            memLog.Snapshot();
            ValidateSuccess(validator, test);
            memLog.Snapshot();
            memLog.AssertNoAllocations();
        }

        private static void ValidateFailure(JsonValidator validator, TestTypes test)
        {
            IsFalse(validator.ValidateObject("",                    test.roleType, out var error));
            AreEqual("unexpected EOF on root at Role > (root), pos: 0", error);
            
            IsFalse(validator.ValidateObject("123",                 test.roleType, out error));
            AreEqual("ValidateObject() expect object. was: ValueNumber at Role > (root), pos: 3", error);
            
            IsFalse(validator.ValidateObject("{}",                  test.roleType, out error));
            AreEqual("Missing required fields: [id, rights] at Role > (root), pos: 2", error);
            
            IsFalse(validator.ValidateObject("[]",                  test.roleType, out error));
            AreEqual("ValidateObject() expect object. was: ArrayStart at Role > [], pos: 1", error);
            
            var json = AsJson(@"{'rights': [{ 'type': 'xxx' }] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Invalid discriminant. Was: 'xxx', expect: [allow, task, message, subscribeMessage, database, predicate] at Right > rights[0].type, pos: 27", error);
            
            json = AsJson(@"{'rights': [{ }] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Expect discriminator as first member. Was: ObjectEnd, expect: 'type' at Right > rights[0], pos: 15", error);

            json = AsJson(@"{'rights': [{ 'disc': 'xxx' }] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Invalid discriminator. Was: 'disc', expect: 'type' at Right > rights[0].disc, pos: 27", error);

            IsFalse(validator.ValidateObject("{]",                  test.roleType, out error));
            AreEqual("unexpected character > expect key. Found: ] at Role > (root), pos: 2", error);
            
            IsFalse(validator.ValidateObject("{\"id\": 42 }",       test.roleType, out error));
            AreEqual("Incorrect type. Was: 42, expect: string at Role > id, pos: 9", error);
            
            json = "{\"id\": \"id\", \"rights\": [] } yyy";
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Expected EOF at Role > (root), pos: 29", error);
            
            
            // ------------------------------------ test array elements ------------------------------------
            json = AsJson(@"{'rights': [ X ] }");            
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("unexpected character while reading value. Found: X at Role > rights[0], pos: 14", error);
            
            json = AsJson(@"{'rights': [{ 'type': 'task', 'types': 'zzz' } ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. Was: 'zzz', expect: TaskType[] at RightTask > rights[0].types, pos: 44", error);
            
            json = AsJson(@"{'rights': [{ 'type': 'task', 'types': ['yyy'] } ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Invalid enum value. Was: yyy, expect: TaskType at RightTask > rights[0].types[0], pos: 45", error);
            
            json = AsJson(@"{'rights': [{ 'type': 'task', 'types': {} ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. Was: object, expect: TaskType[] at RightTask > rights[0].types, pos: 40", error);
            
            json = AsJson(@"{'rights': [{ 'type': 'task', 'types': [ {} ] ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. Was: object, expect: TaskType at RightTask > rights[0].types[0], pos: 42", error);
            
            json = AsJson(@"{'rights': [{ 'type': 'task', 'types': true ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. Was: true, expect: TaskType[] at RightTask > rights[0].types, pos: 43", error);
            
            json = AsJson(@"{'rights': [{ 'type': 'task', 'types': [ false ] ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. Was: false, expect: TaskType at RightTask > rights[0].types[0], pos: 46", error);
            
            json = AsJson(@"{'rights': [{ 'type': 'task', 'types': 123 ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. Was: 123, expect: TaskType[] at RightTask > rights[0].types, pos: 42", error);
            
            json = AsJson(@"{'rights': [{ 'type': 'task', 'types': [ 456 ] ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. Was: 456, expect: TaskType at RightTask > rights[0].types[0], pos: 44", error);
            
            json = AsJson(@"{'rights': [{ 'type': 'task', 'types': null ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Required property must not be null. at RightTask > rights[0].types, pos: 43", error);
            
            json = AsJson(@"{'rights': [{ 'type': 'task', 'types': [ null ] ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Element must not be null. at RightTask > rights[0].types[0], pos: 45", error);
            
            
            // ----------------------------- test dictionary elements (values) -----------------------------
            json = AsJson(@"{'rights': [ { 'type': 'database', 'containers': true } ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. Was: true, expect: ContainerAccess at RightDatabase > rights[0].containers, pos: 53", error);
            
            json = AsJson(@"{'rights': [ { 'type': 'database', 'containers': { 'key': true } } ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. Was: true, expect: ContainerAccess at RightDatabase > rights[0].containers.key, pos: 62", error);
            
            json = AsJson(@"{'rights': [ { 'type': 'database', 'containers': 123 } ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. Was: 123, expect: ContainerAccess at RightDatabase > rights[0].containers, pos: 52", error);
            
            json = AsJson(@"{'rights': [ { 'type': 'database', 'containers': { 'key': 456 } } ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. Was: 456, expect: ContainerAccess at RightDatabase > rights[0].containers.key, pos: 61", error);
            
            json = AsJson(@"{'rights': [ { 'type': 'database', 'containers': [] } ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. Was: array, expect: ContainerAccess at RightDatabase > rights[0].containers[], pos: 50", error);
            
            json = AsJson(@"{'rights': [ { 'type': 'database', 'containers': { 'key': [] } } ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Found array as array item. expect: ContainerAccess at RightDatabase > rights[0].containers.key[], pos: 59", error); // todo

        }
        
        private class TestTypes {
            internal    ValidationType  roleType;
            
            internal    readonly string roleDeny        = AsJson(@"{'id': 'role-deny', 'rights': [  ] }");
            internal    readonly string roleDenyArray;
            internal    readonly string roleDenyMap;
            
            internal    readonly string roleDatabase    = AsJson(
@"{'id': 'role-database','description': 'test',
    'rights': [ { 'type': 'database', 'containers': {'Article': { 'operations': ['read', 'update'], 'subscribeChanges': ['update'] }}} ]
}");
            
            internal TestTypes() {
                roleDenyArray   = "[" + roleDeny + "]";
                roleDenyMap     = "{\"key\": " + roleDeny + "}";
            }
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
// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.UserAuth;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.JSON;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Fliox.Schema.Validation;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Schema.Validation
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class UserStoreValidation : LeakTestsFixture
    {
        private static readonly string  JsonSchemaFolder    = CommonUtils.GetBasePath() + "assets~/Schema/JSON/UserStore";
        private static readonly Type[]  UserStoreTypes      = FlioxClient.GetEntityTypes<UserStore>();
        
        [Test]
        public static void ValidateByJsonSchema() {
            var schemas                 = JsonTypeSchema.ReadSchemas(JsonSchemaFolder);
            var jsonSchema              = new JsonTypeSchema(schemas);
            using (var validationSet    = new ValidationSet(jsonSchema))
            using (var validator        = new TypeValidator()) {
                var test = new TestTypes {
                    roleType    = jsonSchema.TypeAsValidationType<Role>(validationSet, "Friflo.Json.Fliox.Hub.UserAuth")
                };
                ValidateSuccess         (validator, test);
                ValidateFailure         (validator, test);
                ValidateSuccessNoAlloc  (validator, test);
            }
        }
        
        [Test]
        public static void ValidateByTypes() {
            using (var nativeSchema     = new NativeTypeSchema(UserStoreTypes))
            using (var validationSet    = new ValidationSet(nativeSchema))
            using (var validator        = new TypeValidator()) {
                var test = new TestTypes {
                    roleType    = nativeSchema.TypeAsValidationType<Role>(validationSet)
                };
                ValidateSuccess         (validator, test);
                ValidateFailure         (validator, test);
                ValidateSuccessNoAlloc  (validator, test);
            }
        }
        
        private static void ValidateSuccess(TypeValidator validator, TestTypes test)
        {
            SimpleAssert.IsTrue(validator.ValidateObject     (test.roleDeny,        test.roleType, out _));
            SimpleAssert.IsTrue(validator.ValidateObject     (test.roleDatabase,    test.roleType, out _));
            
            SimpleAssert.IsTrue(validator.ValidateArray      (test.jsonArray,       test.roleType, out _));
            SimpleAssert.IsTrue(validator.ValidateObjectMap  (test.jsonObject,      test.roleType, out _));

            SimpleAssert.IsTrue(validator.ValidateArray      (test.roleDenyArray,   test.roleType, out _));
            SimpleAssert.IsTrue(validator.ValidateObjectMap  (test.roleDenyMap,     test.roleType, out _));
        }
        
        private static void ValidateSuccessNoAlloc(TypeValidator validator, TestTypes test) {
            var memLog = new MemoryLogger(2, 1, MemoryLog.Enabled);
            memLog.Snapshot();
            ValidateSuccess(validator, test);
            memLog.Snapshot();
            memLog.AssertNoAllocations();
        }

        private static void ValidateFailure(TypeValidator validator, TestTypes test)
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
            AreEqual("Invalid discriminant. was: 'xxx', expect: [allow, task, message, subscribeMessage, access, predicate] at Right > rights[0].type, pos: 27", error);
            
            json = AsJson(@"{'rights': [{ }] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Expect discriminator as first member. was: ObjectEnd, expect: 'type' at Right > rights[0], pos: 15", error);

            json = AsJson(@"{'rights': [{ 'disc': 'xxx' }] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Invalid discriminator. was: 'disc', expect: 'type' at Right > rights[0].disc, pos: 27", error);

            IsFalse(validator.ValidateObject("{]",                  test.roleType, out error));
            AreEqual("unexpected character > expect key. Found: ] at Role > (root), pos: 2", error);
            
            IsFalse(validator.ValidateObject("{\"id\": 42 }",       test.roleType, out error));
            AreEqual("Incorrect type. was: 42, expect: string at Role > id, pos: 9", error);
            
            json = "{\"id\": \"id\", \"rights\": [] } yyy";
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Expected EOF at Role > (root), pos: 29", error);
            
            
            // ------------------------------------ test array elements ------------------------------------
            json = AsJson(@"{'rights': [ X ] }");            
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("unexpected character while reading value. Found: X at Role > rights[0], pos: 14", error);
            
            json = AsJson(@"{'rights': [{ 'type': 'task', 'types': 'zzz' } ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. was: 'zzz', expect: TaskType[] at RightTask > rights[0].types, pos: 44", error);
            
            json = AsJson(@"{'rights': [{ 'type': 'task', 'types': ['yyy'] } ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Invalid enum value. was: 'yyy', expect: TaskType at RightTask > rights[0].types[0], pos: 45", error);
            
            json = AsJson(@"{'rights': [{ 'type': 'task', 'types': {} ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. was: object, expect: TaskType[] at RightTask > rights[0].types, pos: 40", error);
            
            json = AsJson(@"{'rights': [{ 'type': 'task', 'types': [ {} ] ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. was: object, expect: TaskType at RightTask > rights[0].types[0], pos: 42", error);
            
            json = AsJson(@"{'rights': [{ 'type': 'task', 'types': true ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. was: true, expect: TaskType[] at RightTask > rights[0].types, pos: 43", error);
            
            json = AsJson(@"{'rights': [{ 'type': 'task', 'types': [ false ] ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. was: false, expect: TaskType at RightTask > rights[0].types[0], pos: 46", error);
            
            json = AsJson(@"{'rights': [{ 'type': 'task', 'types': 123 ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. was: 123, expect: TaskType[] at RightTask > rights[0].types, pos: 42", error);
            
            json = AsJson(@"{'rights': [{ 'type': 'task', 'types': [ 456 ] ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. was: 456, expect: TaskType at RightTask > rights[0].types[0], pos: 44", error);
            
            json = AsJson(@"{'rights': [{ 'type': 'task', 'types': null ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Required property must not be null. at RightTask > rights[0].types, pos: 43", error);
            
            json = AsJson(@"{'rights': [{ 'type': 'task', 'types': [ null ] ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Element must not be null. at RightTask > rights[0].types[0], pos: 45", error);
            
            
            // ----------------------------- test dictionary elements (values) -----------------------------
            json = AsJson(@"{'rights': [ { 'type': 'access', 'containers': true } ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. was: true, expect: ContainerAccess at RightAccess > rights[0].containers, pos: 51", error);
            
            json = AsJson(@"{'rights': [ { 'type': 'access', 'containers': { 'key': true } } ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. was: true, expect: ContainerAccess at RightAccess > rights[0].containers.key, pos: 60", error);
            
            json = AsJson(@"{'rights': [ { 'type': 'access', 'containers': 123 } ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. was: 123, expect: ContainerAccess at RightAccess > rights[0].containers, pos: 50", error);
            
            json = AsJson(@"{'rights': [ { 'type': 'access', 'containers': { 'key': 456 } } ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. was: 456, expect: ContainerAccess at RightAccess > rights[0].containers.key, pos: 59", error);
            
            json = AsJson(@"{'rights': [ { 'type': 'access', 'containers': [] } ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. was: array, expect: ContainerAccess at RightAccess > rights[0].containers[], pos: 48", error);
            
            json = AsJson(@"{'rights': [ { 'type': 'access', 'containers': { 'key': [] } } ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Found array as array item. expect: ContainerAccess at RightAccess > rights[0].containers.key[], pos: 57", error); // todo

        }
        
        private class TestTypes {
            internal    ValidationType  roleType;
            
            internal    readonly JsonValue   roleDeny;
            internal    readonly JsonValue   roleDenyArray;
            internal    readonly JsonValue   roleDenyMap;
            internal    readonly JsonValue   jsonArray;
            internal    readonly JsonValue   jsonObject;
            internal    readonly JsonValue   roleDatabase    = new JsonValue(AsJson(
@"{'id': 'role-database','description': 'test',
    'rights': [ { 'type': 'access', 'containers': {'Article': { 'operations': ['read', 'upsert'], 'subscribeChanges': ['upsert'] }}} ]
}"));
            
            internal TestTypes() {
                roleDeny        = new JsonValue(AsJson(@"{'id': 'role-deny', 'rights': [  ] }"));
                roleDenyArray   = new JsonValue("[" + roleDeny + "]");
                roleDenyMap     = new JsonValue("{\"key\": " + roleDeny + "}");
                jsonArray       = new JsonValue("[]");
                jsonObject      = new JsonValue("{}");
            }
        }
       
        private static string AsJson (string str) {
            return str.Replace('\'', '"');
        }
    }
}
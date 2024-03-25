// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.DB.UserAuth;
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
        
        [Test]
        public static void ValidateByJsonSchema() {
            var schemas             = JsonTypeSchema.ReadSchemas(JsonSchemaFolder);
            var jsonSchema          = new JsonTypeSchema(schemas);
            using (var validator    = new TypeValidator()) {
                var validationSet   = new ValidationSet(jsonSchema);
                var test = new TestTypes {
                    roleType    = jsonSchema.TypeAsValidationType<Role>(validationSet, "Friflo.Json.Fliox.Hub.DB.UserAuth")
                };
                ValidateSuccess         (validator, test);
                ValidateFailure         (validator, test);
                ValidateSuccessNoAlloc  (validator, test);
            }
        }
        
        [Test]
        public static void ValidateByTypes() {
            var nativeSchema = NativeTypeSchema.Create(typeof(UserStore));
            using (var validator    = new TypeValidator()) {
                var validationSet   = new ValidationSet(nativeSchema);
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
            AreEqual("expect object. was: 123 at Role > (root), pos: 3", error);
            
            IsFalse(validator.ValidateObject("{}",                  test.roleType, out error));
            AreEqual("Missing required fields: [id, taskRights] at Role > (root), pos: 2", error);
            
            IsFalse(validator.ValidateObject("[]",                  test.roleType, out error));
            AreEqual("expect object. was: array at Role > [], pos: 1", error);
            
            var json = AsJson(@"{'taskRights': [{ 'type': 'xxx' }] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Invalid discriminant. was: 'xxx', expect: [dbFull, dbTask, dbContainer, sendMessage, subscribeMessage, predicate] at TaskRight > taskRights[0].type, pos: 31", error);
            
            json = AsJson(@"{'taskRights': [{ }] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Expect discriminator as first member. was: ObjectEnd, expect: 'type' at TaskRight > taskRights[0], pos: 19", error);

            json = AsJson(@"{'taskRights': [{ 'disc': 'xxx' }] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Invalid discriminator. was: 'disc', expect: 'type' at TaskRight > taskRights[0].disc, pos: 31", error);

            IsFalse(validator.ValidateObject("{]",                  test.roleType, out error));
            AreEqual("unexpected character > expect key. Found: ] at Role > (root), pos: 2", error);
            
            IsFalse(validator.ValidateObject("{\"id\": 42 }",       test.roleType, out error));
            AreEqual("Incorrect type. was: 42, expect: string at Role > id, pos: 9", error);
            
            json = "{\"id\": \"id\", \"taskRights\": [] } yyy";
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Expected EOF at Role > (root), pos: 33", error);
            
            
            // ------------------------------------ test array elements ------------------------------------
            json = AsJson(@"{'taskRights': [ X ] }");            
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("unexpected character while reading value. Found: X at Role > taskRights[0], pos: 18", error);
            
            json = AsJson(@"{'taskRights': [{ 'type': 'dbTask', 'types': 'zzz' } ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. was: 'zzz', expect: TaskType[] at DbTaskRight > taskRights[0].types, pos: 50", error);
            
            json = AsJson(@"{'taskRights': [{ 'type': 'dbTask', 'types': ['yyy'] } ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Invalid enum value. was: 'yyy', expect: TaskType at DbTaskRight > taskRights[0].types[0], pos: 51", error);
            
            json = AsJson(@"{'taskRights': [{ 'type': 'dbTask', 'types': {} ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. was: object, expect: TaskType[] at DbTaskRight > taskRights[0].types, pos: 46", error);
            
            json = AsJson(@"{'taskRights': [{ 'type': 'dbTask', 'types': [ {} ] ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. was: object, expect: TaskType at DbTaskRight > taskRights[0].types[0], pos: 48", error);
            
            json = AsJson(@"{'taskRights': [{ 'type': 'dbTask', 'types': true ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. was: true, expect: TaskType[] at DbTaskRight > taskRights[0].types, pos: 49", error);
            
            json = AsJson(@"{'taskRights': [{ 'type': 'dbTask', 'types': [ false ] ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. was: false, expect: TaskType at DbTaskRight > taskRights[0].types[0], pos: 52", error);
            
            json = AsJson(@"{'taskRights': [{ 'type': 'dbTask', 'types': 123 ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. was: 123, expect: TaskType[] at DbTaskRight > taskRights[0].types, pos: 48", error);
            
            json = AsJson(@"{'taskRights': [{ 'type': 'dbTask', 'types': [ 456 ] ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. was: 456, expect: TaskType at DbTaskRight > taskRights[0].types[0], pos: 50", error);
            
            json = AsJson(@"{'taskRights': [{ 'type': 'dbTask', 'types': null ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Required property must not be null. at DbTaskRight > taskRights[0].types, pos: 49", error);
            
            json = AsJson(@"{'taskRights': [{ 'type': 'dbTask', 'types': [ null ] ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Element must not be null. at DbTaskRight > taskRights[0].types[0], pos: 51", error);
            
            
            // ----------------------------- test array elements (values) -----------------------------
            json = AsJson(@"{'taskRights': [ { 'type': 'dbContainer', 'containers': true } ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. was: true, expect: ContainerAccess[] at DbContainerRight > taskRights[0].containers, pos: 60", error);
            
            json = AsJson(@"{'taskRights': [ { 'type': 'dbContainer', 'containers': { 'key': true } } ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. was: object, expect: ContainerAccess[] at DbContainerRight > taskRights[0].containers, pos: 57", error);
            
            json = AsJson(@"{'taskRights': [ { 'type': 'dbContainer', 'containers': 123 } ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. was: 123, expect: ContainerAccess[] at DbContainerRight > taskRights[0].containers, pos: 59", error);
            
            json = AsJson(@"{'taskRights': [ { 'type': 'dbContainer', 'containers': { 'key': 456 } } ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. was: object, expect: ContainerAccess[] at DbContainerRight > taskRights[0].containers, pos: 57", error);
            
            json = AsJson(@"{'taskRights': [ { 'type': 'dbContainer', 'containers': [] } ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Missing required fields: [database] at DbContainerRight > taskRights[0], pos: 60", error);
            
            json = AsJson(@"{'taskRights': [ { 'type': 'dbContainer', 'containers': { 'key': [] } } ] }");
            IsFalse(validator.ValidateObject(json,                  test.roleType, out error));
            AreEqual("Incorrect type. was: object, expect: ContainerAccess[] at DbContainerRight > taskRights[0].containers, pos: 57", error); // todo

        }
        
        private class TestTypes {
            internal    ValidationType       roleType;
            
            internal    readonly JsonValue   roleDeny;
            internal    readonly JsonValue   roleDenyArray;
            internal    readonly JsonValue   roleDenyMap;
            internal    readonly JsonValue   jsonArray;
            internal    readonly JsonValue   jsonObject;
            internal    readonly JsonValue   roleDatabase    = new JsonValue(AsJson(
@"{'id': 'db-operation','description': 'test',
    'taskRights': [ { 'type': 'dbContainer', 'database':'main_db', 'containers': [{'name': 'articles', 'operations': ['read', 'upsert'], 'subscribeChanges': ['upsert']}]} ]
}"));
            
            internal TestTypes() {
                roleDeny        = new JsonValue(AsJson(@"{'id': 'db-deny', 'taskRights': [  ] }"));
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
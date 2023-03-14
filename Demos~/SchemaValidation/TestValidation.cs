using System.Collections.Generic;
using Friflo.Json.Fliox.Schema;
using Friflo.Json.Fliox.Schema.Language;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace SchemaValidation
{
    public static class TestValidation
    {
        // --- primitive types
        [Test]
        public static void TestValidatePrimitives() {
            {
                var success = JsonValidator.Validate("1", typeof(int), out var error);
                IsTrue(success);
            } {
                var success = JsonValidator.Validate("true", typeof(bool), out var error);
                IsTrue(success);
            } {
                var success = JsonValidator.Validate("1.23", typeof(double), out var error);
                IsTrue(success);
            } {
                var success = JsonValidator.Validate("\"abc\"", typeof(string), out var error);
                IsTrue(success);
            } {
                var success = JsonValidator.Validate("\"xyz\"", typeof(int), out var  error);
                IsFalse(success);
                AreEqual("Incorrect type. was: 'xyz', expect: int32 (root), pos: 5", error);
            }
        }
        
        // --- array types
        [Test]
        public static void TestValidateArray() {
            {
                var success = JsonValidator.Validate("[1]", typeof(int[]), out var error);
                IsTrue(success);
            } {
                var success = JsonValidator.Validate("[1,2,3]", typeof(List<int>), out var error);
                IsTrue(success);
            }
        }
        
        // --- optional class fields
        [Test]
        public static void TestValidateOptionalFields() {
            {
                var json = "{}";
                var success = JsonValidator.Validate(json, typeof(OptionalFields), out var error);
                IsTrue(success);
            } {
                var json = "{\"age\":42,\"name\":\"Peter\",\"gender\":\"male\",\"intArray\":[1]}";
                var success = JsonValidator.Validate(json, typeof(OptionalFields), out var error);
                IsTrue(success);
            } {
                var json = "{\"a\":1}";
                var success = JsonValidator.Validate(json, typeof(OptionalFields), out var  error);
                IsFalse(success);
                AreEqual("Unknown property: 'a' at OptionalFields > a, pos: 6", error);
            } {
                var json = "{\"age\":true}";
                var success = JsonValidator.Validate(json, typeof(OptionalFields), out var  error);
                IsFalse(success);
                AreEqual("Incorrect type. was: true, expect: int32 at OptionalFields > age, pos: 11", error);
            }
        }
        
        // --- required class fields
        [Test]
        public static void TestValidateRequiredFields() {
            {
                var json = "{\"age\":42,\"name\":\"Peter\",\"gender\":\"male\",\"intArray\":[1]}";
                var success = JsonValidator.Validate(json, typeof(RequiredFields), out var  error);
                IsTrue(success);
            } {
                var json = "{}";
                var success = JsonValidator.Validate(json, typeof(RequiredFields), out var  error);
                IsFalse(success);
                AreEqual("Missing required fields: [age, name, gender, intArray] at RequiredFields > (root), pos: 2", error);
            }
        }

        // --- polymorph classes
        [Test]
        public static void TestValidatePolymorphType() {
            {
                var json = "{\"vehicleType\":\"car\",\"seatCount\":1}";
                var success = JsonValidator.Validate(json, typeof(Vehicle), out var  error);
                IsTrue(success);
            } {
                var json = "{}";
                var success = JsonValidator.Validate(json, typeof(Vehicle), out var  error);
                IsFalse(success);
                AreEqual("Expect discriminator as first member. was: ObjectEnd, expect: 'vehicleType' at Vehicle > (root), pos: 2", error);
            }
        }
        
        /// Generate types for: C#, GraphQL, HTML, JSON Schema, Kotlin, Markdown and Typescript in folder: ./schema
        [Test]
        public static void GenerateSchemaModels() {
            var schemaModels = SchemaModel.GenerateSchemaModels(typeof(RequiredFields));
            foreach (var schemaModel in schemaModels) {
                var folder = $"./schema/{schemaModel.type}";
                schemaModel.WriteFiles(folder);
            }
        }
    }
}

// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#pragma warning disable CS0649

using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox.Schema;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable UnusedVariable
// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable ConvertToConstant.Local
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Schema.Validation
{
    public static class TestJsonValidator
    {
        class OptionalFields
        {
            public  int?    age;
            public  string  name;
            public  int[]   intArray;
        }
        
        class RequiredFields
        {
                       public   int     age;
            [Required] public   string  name;
            [Required] public   int[]   intArray;
        }
        
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
        
        [Test]
        public static void TestValidateOptionalFields() {
            {
                var json = "{}";
                var success = JsonValidator.Validate(json, typeof(OptionalFields), out var error);
                IsTrue(success);
            } {
                var json = "{\"age\":42,\"name\":\"Peter\",\"intArray\":[1]}";
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
        
        [Test]
        public static void TestValidateRequiredFields() {
            {
                var json = "{\"age\":42,\"name\":\"Peter\",\"intArray\":[1]}";
                var success = JsonValidator.Validate(json, typeof(RequiredFields), out var  error);
                IsTrue(success);
            } {
                var json = "{}";
                var success = JsonValidator.Validate(json, typeof(RequiredFields), out var  error);
                IsFalse(success);
                AreEqual("Missing required fields: [age, name, intArray] at RequiredFields > (root), pos: 2", error);
            }
        }
    }
}
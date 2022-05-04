// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Fliox.Schema.Validation;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable InlineOutVariableDeclaration
// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Schema.Validation
{
    public static class TestValidation
    {
        [Test]
        public static void ValidateArguments() {
            var intArrayArg     = CreateValidationType(typeof(int[]));
            var intArg          = CreateValidationType(typeof(int));
            var intNullArg      = CreateValidationType(typeof(int?));
            var stringArg       = CreateValidationType(typeof(string));

            using (var validator = new TypeValidator()) {
                bool success;
                string error;
                // --- int
                success = validator.ValidateField(new JsonValue("123"), intArg, out error);
                IsTrue(success);
                
                success = validator.ValidateField(new JsonValue("null"), intArg, out error);
                IsFalse(success);
                AreEqual("expect non null value. was null", error);
                
                success = validator.ValidateField(new JsonValue("{}"), intArg, out error);
                IsFalse(success);
                AreEqual("Incorrect type. was: object, expect: int32 at int32 > (root), pos: 1", error);
                
                success = validator.ValidateField(new JsonValue("xxx"), intArg, out error);
                IsFalse(success);
                AreEqual("unexpected character while reading value. Found: x", error);
                
                // --- int?
                success = validator.ValidateField(new JsonValue("456"), intNullArg, out error);
                IsTrue(success);
                
                success = validator.ValidateField(new JsonValue("null"), intNullArg, out error);
                IsTrue(success);

                success = validator.ValidateField(new JsonValue("true"), intNullArg, out error);
                IsFalse(success);
                AreEqual("Incorrect type. was: true, expect: int32 (root), pos: 4", error);
                
                // --- int[]
                success = validator.ValidateField(new JsonValue("[1,2,3]"), intArrayArg, out error);
                IsTrue(success);
                
                success = validator.ValidateField(new JsonValue("null"), intArrayArg, out error);
                IsTrue(success);
                
                success = validator.ValidateField(new JsonValue("[\"abc\"]"), intArrayArg, out error);
                IsFalse(success);
                AreEqual("Incorrect type. was: 'abc', expect: int32 [0], pos: 6", error);
                
                // --- string
                success = validator.ValidateField(new JsonValue("\"xyz\""), stringArg, out error);
                IsTrue(success);
                
                success = validator.ValidateField(new JsonValue("null"), stringArg, out error);
                IsTrue(success);
                
                success = validator.ValidateField(new JsonValue("42"), stringArg, out error);
                IsFalse(success);
                AreEqual("Incorrect type. was: 42, expect: string (root), pos: 2", error);
            }
        }
        
        private static ValidationType CreateValidationType(Type type) {
            var schema          = NativeTypeSchema.Create(type);
            var validationSet   = new ValidationSet(schema);
            var validationType  = validationSet.GetValidationType(schema, type);
            return validationType;
        }
    }
}
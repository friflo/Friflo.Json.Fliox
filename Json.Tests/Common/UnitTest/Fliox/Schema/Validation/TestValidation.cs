// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Validation;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable InlineOutVariableDeclaration
// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Schema.Validation
{
    public class TestValidation
    {
        private             TypeValidator       validator;
        private   readonly  NativeValidationSet validationSet   = new NativeValidationSet();
        private             bool                success;
        private             string              error;
        
        [OneTimeSetUp]
        public void  Init() {
            validator = new TypeValidator();
        }
        [OneTimeTearDown]
        public void  Dispose() {
            validator.Dispose();
        }
        
        [Test]
        public void ValidateInt() {
            var validation = validationSet.GetValidationType(typeof(int));
            
            success = validator.ValidateField(new JsonValue("123"),         validation, out error);
            IsTrue(success);
                
            success = validator.ValidateField(new JsonValue("null"),        validation, out error);
            IsFalse(success);
            AreEqual("expect non null value. was null", error);
                
            success = validator.ValidateField(new JsonValue("{}"),          validation, out error);
            IsFalse(success);
            AreEqual("Incorrect type. was: object, expect: int32 at int32 > (root), pos: 1", error);
                
            success = validator.ValidateField(new JsonValue("xxx"),         validation, out error);
            IsFalse(success);
            AreEqual("unexpected character while reading value. Found: x", error);
        }
        
        [Test]
        public void ValidateIntArray() {
            var validation = validationSet.GetValidationType(typeof(int[]));
            
            success = validator.ValidateField(new JsonValue("[1,2,3]"),     validation, out error);
            IsTrue(success);
                
            success = validator.ValidateField(new JsonValue("null"),        validation, out error);
            IsTrue(success);
                
            success = validator.ValidateField(new JsonValue("[\"abc\"]"),   validation, out error);
            IsFalse(success);
            AreEqual("Incorrect type. was: 'abc', expect: int32 [0], pos: 6", error);
        }

        [Test]
        public void ValidateIntNull() {
            var validation = validationSet.GetValidationType(typeof(int?));
            
            success = validator.ValidateField(new JsonValue("456"),         validation, out error);
            IsTrue(success);
                
            success = validator.ValidateField(new JsonValue("null"),        validation, out error);
            IsTrue(success);

            success = validator.ValidateField(new JsonValue("true"),        validation, out error);
            IsFalse(success);
            AreEqual("Incorrect type. was: true, expect: int32 (root), pos: 4", error);
        }
        
        [Test]
        public void ValidateString() {
            var validation       = validationSet.GetValidationType(typeof(string));
            success = validator.ValidateField(new JsonValue("\"xyz\""),     validation, out error);
            IsTrue(success);
                
            success = validator.ValidateField(new JsonValue("null"),        validation, out error);
            IsTrue(success);
                
            success = validator.ValidateField(new JsonValue("42"),          validation, out error);
            IsFalse(success);
            AreEqual("Incorrect type. was: 42, expect: string (root), pos: 2", error);
        }
    }
}
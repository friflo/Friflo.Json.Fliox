// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Schema.Validation;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable InlineOutVariableDeclaration
// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Schema.Validation
{
    public class TestValidation
    {
        private     TypeValidator       validator;
        private     NativeValidationSet validationSet;
        private     bool                success;
        private     string              error;
        
        [OneTimeSetUp]
        public void  Init() {
            validator       = new TypeValidator();
            validationSet   = new NativeValidationSet();
        }
        [OneTimeTearDown]
        public void  Dispose() {
            validator.Dispose();
        }
        
        [Test]
        public void ValidateInt() {
            var validation  = validationSet.GetValidationType(typeof(int));
            var cached      = validationSet.GetValidationType(typeof(int)); // test coverage: return cached ValidationType
            IsTrue(validation == cached);
            
            success = validator.Validate(new JsonValue("123"),          validation, out error);
            IsTrue(success);
            
            success = validator.Validate(new JsonValue("null"),         validation, out error);
            IsFalse(success);
            AreEqual("Incorrect type. was: null, expect: int32 (root), pos: 4", error);
            
            success = validator.Validate(new JsonValue("\"noInt\""),    validation, out error);
            IsFalse(success);
            AreEqual("Incorrect type. was: 'noInt', expect: int32 (root), pos: 7", error);
                
            success = validator.Validate(new JsonValue("{}"),           validation, out error);
            IsFalse(success);
            AreEqual("Incorrect type. was: object, expect: int32 at int32 > (root), pos: 1", error);
            
            success = validator.Validate(new JsonValue("[]"),           validation, out error);
            IsFalse(success);
            AreEqual("Incorrect type. was: array, expect: int32 [], pos: 1", error);
                
            success = validator.Validate(new JsonValue("xxx"),          validation, out error);
            IsFalse(success);
            AreEqual("unexpected character while reading value. Found: x", error);
        }
        
        [Test]
        public void ValidateBool() {
            var validation = validationSet.GetValidationType(typeof(bool));
            
            success = validator.Validate(new JsonValue("true"),         validation, out error);
            IsTrue(success);
                
            success = validator.Validate(new JsonValue("null"),         validation, out error);
            IsFalse(success);
            AreEqual("Incorrect type. was: null, expect: boolean (root), pos: 4", error);
            
            success = validator.Validate(new JsonValue("555"),          validation, out error);
            IsFalse(success);
            AreEqual("Incorrect type. was: 555, expect: boolean (root), pos: 3", error);

            success = validator.Validate(new JsonValue("\"noBool\""),   validation, out error);
            IsFalse(success);
            AreEqual("Incorrect type. was: 'noBool', expect: boolean (root), pos: 8", error);
                
            success = validator.Validate(new JsonValue("{}"),           validation, out error);
            IsFalse(success);
            AreEqual("Incorrect type. was: object, expect: boolean at boolean > (root), pos: 1", error);
            
            success = validator.Validate(new JsonValue("[]"),           validation, out error);
            IsFalse(success);
            AreEqual("Incorrect type. was: array, expect: boolean [], pos: 1", error);
                
            success = validator.Validate(new JsonValue("yyy"),          validation, out error);
            IsFalse(success);
            AreEqual("unexpected character while reading value. Found: y", error);
        }
        
        [Test]
        public void ValidateIntNull() {
            var validation = validationSet.GetValidationType(typeof(int?));
            
            success = validator.Validate(new JsonValue("456"),          validation, out error);
            IsTrue(success);
                
            success = validator.Validate(new JsonValue("null"),         validation, out error);
            IsTrue(success);

            success = validator.Validate(new JsonValue("true"),         validation, out error);
            IsFalse(success);
            AreEqual("Incorrect type. was: true, expect: int32 (root), pos: 4", error);
        }
        
        [Test]
        public void ValidateString() {
            var validation       = validationSet.GetValidationType(typeof(string));
            success = validator.Validate(new JsonValue("\"xyz\""),      validation, out error);
            IsTrue(success);
                
            success = validator.Validate(new JsonValue("null"),         validation, out error);
            IsTrue(success);
                
            success = validator.Validate(new JsonValue("42"),           validation, out error);
            IsFalse(success);
            AreEqual("Incorrect type. was: 42, expect: string (root), pos: 2", error);

            success = validator.Validate(new JsonValue("[]"),           validation, out error);
            IsFalse(success);
            AreEqual("Incorrect type. was: array, expect: string [], pos: 1", error);
            
            success = validator.Validate(new JsonValue("{}"),           validation, out error);
            IsFalse(success);
            AreEqual("Incorrect type. was: object, expect: string at string > (root), pos: 1", error);
            
            success = validator.Validate(new JsonValue("null {"),       validation, out error);
            IsFalse(success);
            AreEqual("Expected EOF at string > (root), pos: 6", error);
        }
        
        [Test]
        public void ValidateDateTime() {
            var validation       = validationSet.GetValidationType(typeof(DateTime));
            success = validator.Validate(new JsonValue("\"2022-05-09T10:12:15.786Z\""),     validation, out error);
            IsTrue(success);
            
            success = validator.Validate(new JsonValue("\"error-05-09T10:12:15.786Z\""),    validation, out error);
            IsFalse(success);
            AreEqual("Invalid DateTime: 'error-05-09T10:12:15.786Z' (root), pos: 27", error);
        }
        
        [Test]
        public void ValidateBigInteger() {
            var validation       = validationSet.GetValidationType(typeof(BigInteger));
            success = validator.Validate(new JsonValue("\"123456789\""),            validation, out error);
            IsTrue(success);
            
            success = validator.Validate(new JsonValue("\"invalid big int\""),      validation, out error);
            IsFalse(success);
            AreEqual("Invalid BigInteger: 'invalid big int' (root), pos: 17", error);
        }
        
        [Test]
        public void ValidateGuid() {
            var validation       = validationSet.GetValidationType(typeof(Guid));
            success = validator.Validate(new JsonValue("\"11111111-2222-3333-4444-555555555555\""),      validation, out error);
            IsTrue(success);
            
            success = validator.Validate(new JsonValue("\"invalid Guid\""),         validation, out error);
            IsFalse(success);
            AreEqual("Invalid Guid: 'invalid Guid' (root), pos: 14", error);
        }
        
        [Test]
        public void ValidateEnum() {
            var validation       = validationSet.GetValidationType(typeof(ValidationEnum));
            success = validator.Validate(new JsonValue("\"One\""),                  validation, out error);
            IsTrue(success);
            
            success = validator.Validate(new JsonValue("\"invalid_enum_value\""),   validation, out error);
            IsFalse(success);
            AreEqual("Invalid enum value. was: 'invalid_enum_value', expect: ValidationEnum (root), pos: 20", error);
        }
        
        [Test]
        public void ValidateJsonValue() {
            var validation       = validationSet.GetValidationType(typeof(JsonValue));
            success = validator.Validate(new JsonValue("\"xyz\""),      validation, out error);
            IsTrue(success);
            
            success = validator.Validate(new JsonValue("222"),          validation, out error);
            IsTrue(success);
            
            success = validator.Validate(new JsonValue("true"),         validation, out error);
            IsTrue(success);

            success = validator.Validate(new JsonValue("null"),         validation, out error);
            IsTrue(success);
                
            // --- object
            success = validator.Validate(new JsonValue("{}"),           validation, out error);
            IsTrue(success);
            
            success = validator.Validate(new JsonValue("{\"foo\": 88}"),validation, out error);
            IsTrue(success);
            
            // --- array
            success = validator.Validate(new JsonValue("[]"),           validation, out error);
            IsTrue(success);
            
            success = validator.Validate(new JsonValue("[66,\"str\"]"), validation, out error);
            IsTrue(success);
            
            success = validator.Validate(new JsonValue("[null]"),       validation, out error);
            IsTrue(success);
            
            success = validator.Validate(new JsonValue("[[]]"),         validation, out error);
            IsTrue(success);

            success = validator.Validate(new JsonValue("[{}]"),         validation, out error);
            IsTrue(success);
            
            success = validator.Validate(new JsonValue("123 {"),         validation, out error);
            IsFalse(success);
            AreEqual("Expected EOF at JSON > (root), pos: 5", error);
        }
        
        // --- container types: array, List<>, ...
        [Test]
        public void ValidateIntArray() {
            var validation = validationSet.GetValidationType(typeof(int[]));
            
            success = validator.Validate(new JsonValue("[1,2,3]"),      validation, out error);
            IsTrue(success);
                
            success = validator.Validate(new JsonValue("null"),         validation, out error);
            IsTrue(success);
            
            success = validator.Validate(new JsonValue("[null]"),       validation, out error);
            IsFalse(success);
            AreEqual("Element must not be null. [0], pos: 5", error);
                
            success = validator.Validate(new JsonValue("[\"abc\"]"),    validation, out error);
            IsFalse(success);
            AreEqual("Incorrect type. was: 'abc', expect: int32 [0], pos: 6", error);
            
            success = validator.Validate(new JsonValue("\"no array\""), validation, out error);
            IsFalse(success);
            AreEqual("Incorrect type. was: 'no array', expect: int32 (root), pos: 10", error);
            
            success = validator.Validate(new JsonValue("{}"),           validation, out error);
            IsFalse(success);
            AreEqual("Incorrect type. was: object, expect: int32 at int32 > (root), pos: 1", error);
        }
        
        [Test]
        public void ValidateIntNullArray() {
            var validation = validationSet.GetValidationType(typeof(int?[]));
            
            success = validator.Validate(new JsonValue("[4,null]"),     validation, out error);
            IsTrue(success);
        }
        
        [Test]
        public void ValidateIntList() {
            var validation = validationSet.GetValidationType(typeof(List<int>));
            
            success = validator.Validate(new JsonValue("[5]"),          validation, out error);
            IsTrue(success);
            
            success = validator.Validate(new JsonValue("[null]"),       validation, out error);
            IsFalse(success);
            AreEqual("Element must not be null. [0], pos: 5", error);
        }
        
        [Test]
        public void ValidateIntNullList() {
            var validation = validationSet.GetValidationType(typeof(List<int?>));
            
            success = validator.Validate(new JsonValue("[6,null]"),     validation, out error);
            IsTrue(success);
        }
    }
    
    enum ValidationEnum { None, One, Two }
}
// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#pragma warning disable CS0649 // classes at not instantiated in tests

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Schema;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable all
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
    
    // --- class type
    class OptionalFields
    {
        public  int?    age;
        public  string  name;
        public  Gender? gender;
        public  int[]   intArray;
    }
    
    class RequiredFields
    {
                    public  int     age;
        [Required]  public  string  name;
                    public  Gender gender;
        [Required]  public  int[]   intArray;
    }
    
    enum Gender {
        male,
        female
    }
    
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
    
    // --- polymorph class type
    [Discriminator("vehicleType")]
    [PolymorphType(typeof(Car),     "car")]
    [PolymorphType(typeof(Bike),    "bike")]
    class Vehicle { }
    
    class Car : Vehicle {
        public int  seatCount;
    }
    
    class Bike : Vehicle {
        public bool hasLuggageRack;
    }
    
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
}

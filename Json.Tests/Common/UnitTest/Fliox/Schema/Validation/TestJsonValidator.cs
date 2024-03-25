// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#pragma warning disable CS0649 // classes at not instantiated in tests

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox;
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
        
        [Test]
        public static void TestValidateInteger_Happy() {
            string error;
            IsTrue(JsonValidator.Validate("0",                      typeof(byte),   out error));
            IsTrue(JsonValidator.Validate("255",                    typeof(byte),   out error));
            
            IsTrue(JsonValidator.Validate("-32768",                 typeof(short),  out error));
            IsTrue(JsonValidator.Validate("32767",                  typeof(short),  out error));
            
            IsTrue(JsonValidator.Validate("-2147483648",            typeof(int),    out error));
            IsTrue(JsonValidator.Validate("2147483647",             typeof(int),    out error));
            
            IsTrue(JsonValidator.Validate("-9223372036854775808",   typeof(long),   out error));
            IsTrue(JsonValidator.Validate("9223372036854775807",    typeof(long),   out error));
            
            // Non_CLS
            IsTrue(JsonValidator.Validate("-128",                   typeof(sbyte),  out error));
            IsTrue(JsonValidator.Validate("127",                    typeof(sbyte),  out error));
            
            IsTrue(JsonValidator.Validate("0",                      typeof(ushort), out error));
            IsTrue(JsonValidator.Validate("65535",                  typeof(ushort), out error));
            
            IsTrue(JsonValidator.Validate("0",                      typeof(uint),   out error));
            IsTrue(JsonValidator.Validate("4294967295",             typeof(uint),   out error));
            
            IsTrue(JsonValidator.Validate("0",                      typeof(ulong),  out error));
            IsTrue(JsonValidator.Validate("18446744073709551615",   typeof(ulong),  out error));
        }
        
        [Test]
        public static void TestValidateInteger_Error() {
            string error;
            IsFalse(JsonValidator.Validate("-1",                    typeof(byte),   out error));
            IsFalse(JsonValidator.Validate("256",                   typeof(byte),   out error));
            IsFalse(JsonValidator.Validate("1.2",                   typeof(byte),   out error));
            
            IsFalse(JsonValidator.Validate("-32769",                typeof(short),  out error));
            IsFalse(JsonValidator.Validate("32768",                 typeof(short),  out error));
            
            IsFalse(JsonValidator.Validate("-2147483649",           typeof(int),    out error));
            IsFalse(JsonValidator.Validate("2147483648",            typeof(int),    out error));
            
            IsFalse(JsonValidator.Validate("-9223372036854775809",  typeof(long),   out error));
            IsFalse(JsonValidator.Validate("9223372036854775808",   typeof(long),   out error));
            
            // Non_CLS
            IsFalse(JsonValidator.Validate("-129",                  typeof(sbyte),  out error));
            IsFalse(JsonValidator.Validate("128",                   typeof(sbyte),  out error));
            
            IsFalse(JsonValidator.Validate("-1",                    typeof(ushort), out error));
            IsFalse(JsonValidator.Validate("65536",                 typeof(ushort), out error));
            
            IsFalse(JsonValidator.Validate("-1",                    typeof(uint),   out error));
            IsFalse(JsonValidator.Validate("4294967296",            typeof(uint),   out error));
            
            IsFalse(JsonValidator.Validate("-1",                    typeof(ulong),  out error));
            IsFalse(JsonValidator.Validate("18446744073709551616",  typeof(ulong),  out error));
            IsFalse(JsonValidator.Validate("1.2",                   typeof(ulong),  out error));
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
}
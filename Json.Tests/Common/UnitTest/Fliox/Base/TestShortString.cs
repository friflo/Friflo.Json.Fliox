// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable InlineOutVariableDeclaration
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Base
{
    public static class TestShortString
    {
        [Test]
        public static void TestShortString_Null() {
            {
                var result = new ShortString();
                IsTrue(result.IsNull());
            } {
                var result = new ShortString(null);
                IsTrue(result.IsNull());
            } {
                var result = new ShortString("a");
                IsFalse(result.IsNull());
            }  {
                var result = new ShortString("a string length > 15");
                IsFalse(result.IsNull());
            }
        }
        
        [Test]
        public static void TestShortString_String() {
            {
                ShortStringUtils.StringToLongLong("", out string str, out long lng, out long lng2);
                IsNull(str);
                AreEqual(0x_00_00_00_00_00_00_00_00, lng);
                AreEqual(0x_01_00_00_00_00_00_00_00, lng2);
                //           ^-- length byte
                
                ShortStringUtils.LongLongToString(lng, lng2, out string result);
                AreEqual("", result);
            } {
                ShortStringUtils.StringToLongLong("a", out string str, out long lng, out long lng2);
                IsNull(str);
                AreEqual(0x_00_00_00_00_00_00_00_61, lng);
                AreEqual(0x_02_00_00_00_00_00_00_00, lng2);
                
                ShortStringUtils.LongLongToString(lng, lng2, out string result);
                AreEqual("a", result);
            } {
                ShortStringUtils.StringToLongLong("012345678901234", out string str, out long lng, out long lng2);
                IsNull(str);
                AreEqual(0x_37_36_35_34_33_32_31_30, lng);
                AreEqual(0x_10_34_33_32_31_30_39_38, lng2);
                
                ShortStringUtils.LongLongToString(lng, lng2, out string result);
                AreEqual("012345678901234", result);
            } {
                ShortStringUtils.StringToLongLong("☀🌎♥👋", out string str, out long lng, out long lng2);
                IsNull(str);
                AreEqual(0x_E2_8E_8C_9F_F0_80_98_E2, (ulong)lng);
                AreEqual(0x_0F_00_8B_91_9F_F0_A5_99, lng2);
                
                ShortStringUtils.LongLongToString(lng, lng2, out string result);
                AreEqual("☀🌎♥👋", result);
            } {
                ShortStringUtils.StringToLongLong("0123456789012345", out string str, out long lng, out long lng2);
                AreEqual("0123456789012345", str);
                AreEqual(0x_00_00_00_00_00_00_00_00, lng);
                AreEqual(0x_7f_00_00_00_00_00_00_00, lng2);
            }
        }
        
        [Test]
        public static void TestShortString_Bytes() {
            {
                var input = new Bytes("").AsSpan();
                ShortStringUtils.BytesToLongLong(input, out long lng, out long lng2);
                AreEqual(0x_00_00_00_00_00_00_00_00, lng);
                AreEqual(0x_01_00_00_00_00_00_00_00, lng2);
                
                ShortStringUtils.LongLongToString(lng, lng2, out string result);
                AreEqual("", result);
            } {
                var input = new Bytes("a").AsSpan();
                ShortStringUtils.BytesToLongLong(input, out long lng, out long lng2);
                AreEqual(0x_00_00_00_00_00_00_00_61, lng);
                AreEqual(0x_02_00_00_00_00_00_00_00, lng2);
                
                ShortStringUtils.LongLongToString(lng, lng2, out string result);
                AreEqual("a", result);
            } {
                var input = new Bytes("012345678901234").AsSpan();
                ShortStringUtils.BytesToLongLong(input, out long lng, out long lng2);
                AreEqual(0x_37_36_35_34_33_32_31_30, lng);
                AreEqual(0x_10_34_33_32_31_30_39_38, lng2);
                
                ShortStringUtils.LongLongToString(lng, lng2, out string result);
                AreEqual("012345678901234", result);
            }
        }
        
        [Test]
        public static void TestShortString_IsEqual() {
            // --- equal
            AssertIsEqual(null,                     null,                   true);
            AssertIsEqual("a",                      "a",                    true);
            AssertIsEqual("a string length > 15",   "a string length > 15", true);
            
            // --- not equal
            AssertIsEqual("a",                      "b",                    false);
            AssertIsEqual("a",                      null,                   false);
            AssertIsEqual("a string length > 15",   null,                   false);
            AssertIsEqual(null,                     "b",                    false);
            AssertIsEqual(null,                     "b string length > 15", false);
        }
        
        private static void AssertIsEqual(string left, string right, bool expected) {
            var leftShort   = new ShortString(left);
            var rightShort  = new ShortString(right);
            var result      = leftShort.IsEqual(rightShort);
            AreEqual(expected, result);
        }
        
        [Test]
        public static void TestShortString_CompareNull () {
            // --- string compare reference
            {
                var result = string.CompareOrdinal(null, null);
                AreEqual( 0, result);
            } {
                var result = string.CompareOrdinal(null, "a");
                AreEqual(-1, result);
            } {
                var result = string.CompareOrdinal("a", null);
                AreEqual(+1, result);
            }
            // --- ShortString null
            {
                var result = new ShortString().Compare(default);
                AreEqual( 0, result);
            }
            // --- ShortString (long) compare
            {
                var result = new ShortString().Compare(new ShortString("a string length > 15"));
                AreEqual(-1, result);
            } {
                var result = new ShortString("a string length > 15").Compare(default);
                AreEqual(+1, result);
            }
            // --- ShortString (short) compare
            {
                var result = new ShortString().Compare(new ShortString("a"));
                AreEqual(-1, result);
            } {
                var result = new ShortString("a").Compare(default);
                AreEqual(+1, result);
            }
        }
        
        [Test]
        public static void TestShortString_Compare() {
            AssertStartsCompare("a",                    "b",                    -1);
            AssertStartsCompare("a string length > 15", "b",                    -1);
            AssertStartsCompare("a",                    "b string length > 15", -1);
            AssertStartsCompare("a string length > 15", "b string length > 15", -1);
            //
            AssertStartsCompare("a",                    "a",                     0);
            AssertStartsCompare("",                     "",                      0);
            AssertStartsCompare("a string length > 15", "a string length > 15",  0);
            //
            AssertStartsCompare("b",                    "a",                     1);
            AssertStartsCompare("b string length > 15", "a",                     1);
            AssertStartsCompare("b",                    "a string length > 15",  1);
            AssertStartsCompare("b string length > 15", "a string length > 15",  1);
        }
        
        private static void AssertStartsCompare(string left, string right, int expected) {
            var leftShort   = new ShortString(left);
            var rightShort  = new ShortString(right);
            var result      = leftShort.Compare(rightShort);
            AreEqual(expected, result);
        }
        
        [Test]
        public static void TestShortString_StartsWith() {
            AssertStartsWith("",                        "",  true);
            AssertStartsWith("a",                       "",  true);
            AssertStartsWith("a",                       "a", true);
            AssertStartsWith("ab",                      "a", true);
            AssertStartsWith("a string length > 15",    "a", true);
            AssertStartsWith("a", "a string length > 15",    false);
            //
            AssertStartsWith("abcde0123456789",         "abcde0123456789",  true);
            AssertStartsWith("abcde0123456789",         "abcde012345678",  true);
            AssertStartsWith("abcde0123456789",         "abcde01234567",  true);
            AssertStartsWith("abcde0123456789",         "abcde0123456",  true);
            AssertStartsWith("abcde0123456789",         "abcde012345",  true);
            AssertStartsWith("abcde0123456789",         "abcde01234",  true);
            AssertStartsWith("abcde0123456789",         "abcde0123",  true);
            AssertStartsWith("abcde0123456789",         "abcde012",  true);
            AssertStartsWith("abcde0123456789",         "abcde01",  true);
            AssertStartsWith("abcde0123456789",         "abcde0",  true);
            AssertStartsWith("abcde0123456789",         "abcde",  true);
            AssertStartsWith("abcde0123456789",         "abcd",  true);
            AssertStartsWith("abcde0123456789",         "abc",  true);
            AssertStartsWith("abcde0123456789",         "ab",  true);
            AssertStartsWith("abcde0123456789",         "a",  true);
            AssertStartsWith("abcde0123456789",         "",  true);
        }
        
        private static void AssertStartsWith(string left, string right, bool expected) {
            var leftShort   = new ShortString(left);
            var rightShort  = new ShortString(right);
            var result      = leftShort.StartsWith(rightShort);
            AreEqual(expected, result);
        }
        
        [Test]
        public static void TestShortString_ToJsonKey() {
            {
                var str = new ShortString ();
                var key = new JsonKey(str);
                IsNull(key.AsString());
            } {
                var str = new ShortString ("abc");
                var key = new JsonKey(str);
                AreEqual("abc", key.AsString());
            } {
                var str = new ShortString ("1");
                var key = new JsonKey(str);
                AreEqual(1, key.AsLong());
                AreEqual("1", key.AsString());
            } {
                var str = new ShortString ("550e8400-e29b-11d4-a716-446655440000");
                var key = new JsonKey(str);
                AreEqual(new Guid("550e8400-e29b-11d4-a716-446655440000"), key.AsGuid());
                AreEqual("550e8400-e29b-11d4-a716-446655440000", key.AsString());
            } {
                var str = new ShortString ("a string length > 15");
                var key = new JsonKey(str);
                AreEqual("a string length > 15", key.AsString());
            }
        }
        
        [Test]
        public static void TestShortString_FromJsonKey() {
            {
                var key = new JsonKey();
                var str = new ShortString(key);
                IsNull(str.AsString());
            } {
                var key = new JsonKey("abc");
                var str = new ShortString(key);
                AreEqual("abc", str.AsString());
            } {
                var key = new JsonKey("1");
                var str = new ShortString(key);
                AreEqual("1", str.AsString());
            } {
                var key = new JsonKey("550e8400-e29b-11d4-a716-446655440000");
                var str = new ShortString(key);
                AreEqual("550e8400-e29b-11d4-a716-446655440000", str.AsString());
            } {
                var key = new JsonKey("a string length > 15");
                var str = new ShortString(key);
                AreEqual("a string length > 15", str.AsString());
            }
        }
        
        [Test]
        public static void TestShortString_Append() {
            var target = new Bytes(10);
            ShortStringUtils.StringToLongLong("abc", out _, out long lng, out long lng2);
            target.AppendShortString(lng, lng2);
            AreEqual("abc", target.AsString());
        }
        
        
        private static List<string> CreateStrings(int count) {
            var list = new List<string>(count);
            for (int n = 0; n < count; n++) {
                list.Add(n.ToString());
            }
            return list;
        }
        
        private static ShortString[] CreateShortStrings(int count) {
            var list = new ShortString[count];
            for (int n = 0; n < count; n++) {
                list[n] = new ShortString(n.ToString());
            }
            return list;
        }
        
        // ----------------------------------------- pref tests -----------------------------------------
        private const int ValueCount        = 10_000;
        private const int EqualsIterations  = 1; // 100_000;
        
        [Test]
        public static void TestShortString_StringEquals () {
            var values   = CreateShortStrings(ValueCount);
            var foo2     = new ShortString("12");
            for (int n = 0; n < EqualsIterations; n++) {
                for (int i = 0; i < ValueCount; i++) {
                    var _ = values[i].IsEqual(foo2);
                }
            }
        }
        
        [Test]
        public static void TestShortString_StringEqualsReference () {
            var values   = CreateStrings(ValueCount);
            var foo2     = "12";
            for (int n = 0; n < EqualsIterations; n++) {
                for (int i = 0; i < ValueCount; i++) {
                    var _ = values[i] == foo2;
                }
            }
        }
        
        private const int StartsWithIterations  = 1; // 1000;
        
        [Test]
        public static void TestShortString_StringStartsWith () {
            var values   = CreateShortStrings(ValueCount);
            var foo2     = new ShortString("12");
            for (int n = 0; n < StartsWithIterations; n++) {
                for (int i = 0; i < ValueCount; i++) {
                    var _ = values[i].StartsWith(foo2);
                }
            }
        }
        
        [Test]
        public static void TestShortString_StringStartsWithReference () {
            var values   = CreateStrings(ValueCount);
            var foo2     = "12";
            for (int n = 0; n < StartsWithIterations; n++) {
                for (int i = 0; i < ValueCount; i++) {
                    var _ = values[i].StartsWith(foo2);
                }
            }
        }
        
        private const int LookupIterations  = 1; // 10_000;
        
        [Test]
        public static void TestShortString_Lookup () {
            var values   = CreateStrings(ValueCount);
            var map     = values.ToDictionary(k => new ShortString(k), v => 0, ShortString.Equality);
            var key     = new ShortString("12");
            for (int n = 0; n < LookupIterations; n++) {
                for (int i = 0; i < ValueCount; i++) {
                    var _ = map[key];
                }
            }
        }
        
        [Test]
        public static void TestShortString_LookupReference () {
            var values  = CreateStrings(ValueCount);
            var map     = values.ToDictionary(k => k, v => 0);
            var key     = "12";
            for (int n = 0; n < LookupIterations; n++) {
                for (int i = 0; i < ValueCount; i++) {
                    var _ = map[key];
                }
            }
        }
    }
}
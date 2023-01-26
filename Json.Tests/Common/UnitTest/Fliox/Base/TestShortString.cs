// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
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
        public static void TestShortString_String() {
            {
                ShortStringUtils.StringToLongLong("", out string str, out long lng, out long lng2);
                IsNull(str);
                AreEqual(0x_00_00_00_00_00_00_00_00, lng);
                AreEqual(0x_00_00_00_00_00_00_00_00, lng2);
                
                ShortStringUtils.LongLongToString(lng, lng2, out string result);
                AreEqual("", result);
            } {
                ShortStringUtils.StringToLongLong("a", out string str, out long lng, out long lng2);
                IsNull(str);
                AreEqual(0x_00_00_00_00_00_00_00_61, lng);
                AreEqual(0x_01_00_00_00_00_00_00_00, lng2);
                //           ^-- length byte
                
                ShortStringUtils.LongLongToString(lng, lng2, out string result);
                AreEqual("a", result);
            } {
                ShortStringUtils.StringToLongLong("012345678901234", out string str, out long lng, out long lng2);
                IsNull(str);
                AreEqual(0x_37_36_35_34_33_32_31_30, lng);
                AreEqual(0x_0F_34_33_32_31_30_39_38, lng2);
                
                ShortStringUtils.LongLongToString(lng, lng2, out string result);
                AreEqual("012345678901234", result);
            } {
                ShortStringUtils.StringToLongLong("☀🌎♥👋", out string str, out long lng, out long lng2);
                IsNull(str);
                AreEqual(0x_E2_8E_8C_9F_F0_80_98_E2, (ulong)lng);
                AreEqual(0x_0E_00_8B_91_9F_F0_A5_99, lng2);
                
                ShortStringUtils.LongLongToString(lng, lng2, out string result);
                AreEqual("☀🌎♥👋", result);
            } {
                ShortStringUtils.StringToLongLong("0123456789012345", out string str, out long lng, out long lng2);
                AreEqual("0123456789012345", str);
                AreEqual(0, lng);
                AreEqual(0, lng2);
            }
        }
        
        [Test]
        public static void TestShortString_Bytes() {
            {
                var input = new Bytes("");
                ShortStringUtils.BytesToLongLong(input, out long lng, out long lng2);
                AreEqual(0x_00_00_00_00_00_00_00_00, lng);
                AreEqual(0x_00_00_00_00_00_00_00_00, lng2);
                
                ShortStringUtils.LongLongToString(lng, lng2, out string result);
                AreEqual("", result);
            } {
                var input = new Bytes("a");
                ShortStringUtils.BytesToLongLong(input, out long lng, out long lng2);
                AreEqual(0x_00_00_00_00_00_00_00_61, lng);
                AreEqual(0x_01_00_00_00_00_00_00_00, lng2);
                
                ShortStringUtils.LongLongToString(lng, lng2, out string result);
                AreEqual("a", result);
            } {
                var input = new Bytes("012345678901234");
                ShortStringUtils.BytesToLongLong(input, out long lng, out long lng2);
                AreEqual(0x_37_36_35_34_33_32_31_30, lng);
                AreEqual(0x_0F_34_33_32_31_30_39_38, lng2);
                
                ShortStringUtils.LongLongToString(lng, lng2, out string result);
                AreEqual("012345678901234", result);
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
            var leftKey  = new ShortString(left);
            var rightKey = new ShortString(right);
            var result   = ShortString.StringCompare(leftKey, rightKey);
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
            var leftKey  = new ShortString(left);
            var rightKey = new ShortString(right);
            var result   = ShortString.StringStartsWith(leftKey, rightKey);
            AreEqual(expected, result);
        }
        
        [Test]
        public static void TestShortString_Append() {
            var target = new Bytes(10);
            ShortStringUtils.StringToLongLong("abc", out _, out long lng, out long lng2);
            target.AppendShortString(lng, lng2);
            AreEqual("abc", target.AsString());
        }
        
        private const int Count = 10; // 50_000_000;
        
        [Test]
        public static void TestShortString_StringStartsWith () {

            var foo1     = new ShortString("foobar");
            var foo2     = new ShortString("foo");
            for (int n = 0; n < Count; n++) {
                var _ = ShortString.StringStartsWith(foo1, foo2);
            }
        }
        
        [Test]
        public static void TestShortString_StringStartsWithReference () {

            var foo1     = "foobar";
            var foo2     = "foo";
            for (int n = 0; n < Count; n++) {
                var _ = foo1.StartsWith(foo2, StringComparison.Ordinal);
            }
        }
        
        /// <summary>
        /// Performance optimization significant for Unity:     50_000_000 => 0.6 sec
        /// </summary>
        [Test]
        public static void TestShortString_StringEquals () {

            var foo1     = new ShortString("foo");
            var foo2     = new ShortString("foo");
            for (int n = 0; n < Count; n++) {
                var _ = foo1.IsEqual(foo2);
            }
        }
        
        /// <summary>
        /// Reference string comparison:                        50_000_000 => 13.0 sec
        /// </summary>
        [Test]
        public static void TestShortString_StringEqualsReference () {
            var foo1     = new string("foo");
            var foo2     = new string("foo");
            for (int n = 0; n < Count; n++) {
                var _ = foo1 == foo2;
            }
        }
    }
}
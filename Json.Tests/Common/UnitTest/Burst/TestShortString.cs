// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable InlineOutVariableDeclaration
namespace Friflo.Json.Tests.Common.UnitTest.Burst
{
    public static class TestShortString
    {
        [Test]
        public static void TestShortString_String() {
            {
                ShortString.StringToLongLong("", out string str, out long lng, out long lng2);
                IsNull(str);
                AreEqual(0x_00_00_00_00_00_00_00_00, lng);
                AreEqual(0x_00_00_00_00_00_00_00_00, lng2);
                
                ShortString.LongLongToString(lng, lng2, out string result);
                AreEqual("", result);
            } {
                ShortString.StringToLongLong("a", out string str, out long lng, out long lng2);
                IsNull(str);
                AreEqual(0x_00_00_00_00_00_00_00_61, lng);
                AreEqual(0x_01_00_00_00_00_00_00_00, lng2);
                //           ^-- length byte
                
                ShortString.LongLongToString(lng, lng2, out string result);
                AreEqual("a", result);
            } {
                ShortString.StringToLongLong("012345678901234", out string str, out long lng, out long lng2);
                IsNull(str);
                AreEqual(0x_37_36_35_34_33_32_31_30, lng);
                AreEqual(0x_0F_34_33_32_31_30_39_38, lng2);
                
                ShortString.LongLongToString(lng, lng2, out string result);
                AreEqual("012345678901234", result);
            } {
                ShortString.StringToLongLong("☀🌎♥👋", out string str, out long lng, out long lng2);
                IsNull(str);
                AreEqual(0x_E2_8E_8C_9F_F0_80_98_E2, (ulong)lng);
                AreEqual(0x_0E_00_8B_91_9F_F0_A5_99, lng2);
                
                ShortString.LongLongToString(lng, lng2, out string result);
                AreEqual("☀🌎♥👋", result);
            } {
                ShortString.StringToLongLong("0123456789012345", out string str, out long lng, out long lng2);
                AreEqual("0123456789012345", str);
                AreEqual(0, lng);
                AreEqual(0, lng2);
            }
        }
        
        [Test]
        public static void TestShortString_Bytes() {
            {
                var input = new Bytes("");
                ShortString.BytesToLongLong(input, out long lng, out long lng2);
                AreEqual(0, lng);
                AreEqual(0, lng2);
                
                ShortString.LongLongToString(lng, lng2, out string result);
                AreEqual("", result);
            } {
                var input = new Bytes("a");
                ShortString.BytesToLongLong(input, out long lng, out long lng2);
                AreEqual(0x_00_00_00_00_00_00_00_61, lng);
                AreEqual(0x_01_00_00_00_00_00_00_00, lng2);
                
                ShortString.LongLongToString(lng, lng2, out string result);
                AreEqual("a", result);
            } {
                var input = new Bytes("012345678901234");
                ShortString.BytesToLongLong(input, out long lng, out long lng2);
                AreEqual(0x_37_36_35_34_33_32_31_30, lng);
                AreEqual(0x_0F_34_33_32_31_30_39_38, lng2);
                
                ShortString.LongLongToString(lng, lng2, out string result);
                AreEqual("012345678901234", result);
            }
        }
        
        [Test]
        public static void TestShortString_Compare() {
            AssertStartsCompare("a",                    "b",                    -1);
            AssertStartsCompare("a string length > 15", "b",                    -1);
            AssertStartsCompare("a",                    "b string length > 15", -1);
            AssertStartsCompare("a string length > 15", "b string length > 15", -1);
        }
        
        private static void AssertStartsCompare(string left, string right, int expected) {
            var leftKey  = new JsonKey(left);
            var rightKey = new JsonKey(right);
            var result   = JsonKey.StringCompare(leftKey, rightKey, StringComparison.InvariantCulture);
            AreEqual(expected, result);
        }
        
        [Test]
        public static void TestShortString_StartsWith() {
            AssertStartsWith("",                        "",  true);
            AssertStartsWith("a",                       "a", true);
            AssertStartsWith("ab",                      "a", true);
            AssertStartsWith("a string length > 15",    "a", true);
            AssertStartsWith("a", "a string length > 15",    false);
        }
        
        private static void AssertStartsWith(string left, string right, bool expected) {
            var leftKey  = new JsonKey(left);
            var rightKey = new JsonKey(right);
            var result   = JsonKey.StringStartsWith(leftKey, rightKey, StringComparison.InvariantCulture);
            AreEqual(expected, result);
        }
        
        [Test]
        public static void TestShortString_Append() {
            var target = new Bytes(10);
            ShortString.StringToLongLong("abc", out _, out long lng, out long lng2);
            target.AppendShortString(lng, lng2);
            AreEqual("abc", target.AsString());
        }
    }
}
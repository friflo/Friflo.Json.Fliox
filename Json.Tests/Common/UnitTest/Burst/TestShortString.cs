// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

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
        public static void TestShortStringUtils_String() {
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
        public static void TestShortStringUtils_Bytes() {
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
        public static void TestShortStringUtils_Compare() {
            {
                var left  = new JsonKey("a");
                var right = new JsonKey("b");
                var result = JsonKey.StringCompare(left, right);
                AreEqual(-1, result); 
            } {
                var left  = new JsonKey("a");
                var right = new JsonKey("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
                var result = JsonKey.StringCompare(left, right);
                AreEqual(-1, result); 
            } {
                var left  = new JsonKey("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
                var right = new JsonKey("b");
                var result = JsonKey.StringCompare(left, right);
                AreEqual(-1, result); 
            } {
                var left  = new JsonKey("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
                var right = new JsonKey("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
                var result = JsonKey.StringCompare(left, right);
                AreEqual(-1, result); 
            }
        }
        
        [Test]
        public static void TestShortStringUtils_Append() {
            var target = new Bytes(10);
            ShortString.StringToLongLong("abc", out _, out long lng, out long lng2);
            target.AppendShortString(lng, lng2);
            AreEqual("abc", target.AsString());
        }
    }
}
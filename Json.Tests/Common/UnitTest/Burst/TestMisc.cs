// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// JSON_BURST_TAG
using Str32 = System.String;

namespace Friflo.Json.Tests.Common.UnitTest.Burst
{
    public class TestMisc : LeakTestsFixture
    {
        [Test]
        public void TestUtf8() {
            Bytes src = CommonUtils.FromFile ("assets~/Burst/EuroSign.txt");
            String str = src.AsString();
            AreEqual("€", str);

            Bytes dst = new Bytes(0);
            dst.AppendStringUtf8("€");
            IsTrue(src.IsEqual(dst));
            dst.Dispose();
            src.Dispose();
        }
        
        [Test]
        public void TestStringIsEqual() {
            Bytes bytes = new Bytes("€");
            AreEqual(3, bytes.Len); // UTF-8 length of € is 3
            String eur = "€";
            AreEqual(eur, bytes.AsString());
            IsTrue(bytes.IsEqualString(eur));
            bytes.Dispose();
            //
            Bytes xyz = new Bytes("xyz");
            String abc = "abc";
            AreEqual(abc.Length, xyz.Len); // Ensure both have same UTF-8 length (both ASCII)
            AreNotEqual(abc, xyz.AsString());
            IsFalse(xyz.IsEqualString(abc));
            xyz.Dispose();          
        }
        
        [Test]
        public void TestUtf8Compare() {
            using (var empty        = new Bytes(""))    //              (0 byte)
            using (var a            = new Bytes("a"))   //  a U+0061    (1 byte)
            using (var ab           = new Bytes("ab"))  //              (2 bytes)
            using (var copyright    = new Bytes("©"))   //  © U+00A9    (2 bytes)  
            using (var euro         = new Bytes("€"))   //  € U+20AC    (3 bytes)
            using (var smiley       = new Bytes("😎"))   //  😎 U+1F60E   (4 bytes)
            {
                IsTrue (Utf8Utils.IsStringEqualUtf8 ("", empty));
                IsTrue (Utf8Utils.IsStringEqualUtf8 ("a", a));

                IsFalse(Utf8Utils.IsStringEqualUtf8 ("a",  ab));
                IsFalse(Utf8Utils.IsStringEqualUtf8 ("ab", a));

                IsTrue (Utf8Utils.IsStringEqualUtf8 ("©", copyright));
                IsTrue (Utf8Utils.IsStringEqualUtf8 ("€", euro));
                IsTrue (Utf8Utils.IsStringEqualUtf8 ("😎", smiley));
            }
        }

        private void AssertUnicodeToByte (ref Bytes dst, string src) {
            dst.Clear();
            dst.EnsureCapacityAbs(dst.end + 4);
            int c = char.ConvertToUtf32 (src, 0);
            Utf8Utils.AppendUnicodeToBytes(ref dst, c);
            AreEqual(src, dst.AsString());
        }

        [Test]
        public void TestUnicodeToBytes() {
            var dst = new Bytes("");
            try {
                AssertUnicodeToByte(ref dst, "a");
                AssertUnicodeToByte(ref dst, "©");
                AssertUnicodeToByte(ref dst, "€");
                AssertUnicodeToByte(ref dst, "😎");

                Str32 src = "a©€😎🌍";
                dst.Clear();
                Utf8JsonWriter.AppendEscString(ref dst, src.AsSpan());
                AreEqual("\"" + src + "\"", dst.AsString());
            } finally {
                dst.Dispose();
            }
        }
        
        private void AssertUnicodeRange(ref Bytes dst, int from, int to, int count) {
            AreEqual(to - from, count);
            for (int codePoint = from; codePoint < to; codePoint++) {
                dst.Clear();
                string str = char.ConvertFromUtf32 (codePoint);
                Utf8Utils.AppendUnicodeToBytes(ref dst, codePoint);
                AreEqual(str, dst.AsString());
            }
        }
        
        [Test]
        public void TestUnicodeToBytesFull() {
            var dst = new Bytes("");
            try {
                dst.EnsureCapacityAbs(dst.end + 4);
                // Test only near borders to speedup test
                AssertUnicodeRange(ref dst,        0,  0x000100, 256); // test border:    0x80
                AssertUnicodeRange(ref dst, 0x0007f0,  0x000810,  32); // test border:   0x800
                AssertUnicodeRange(ref dst, 0x00d7f0,  0x00d800,  16);
                // exclude surrogates:      0x00d800 - 0x00dfff
                AssertUnicodeRange(ref dst, 0x00e000,  0x00e010,  16);
                AssertUnicodeRange(ref dst, 0x00fff0,  0x010010,  32); // test border: 0x10000
                AssertUnicodeRange(ref dst, 0x10fff0,  0x110000,  16);
            } finally{
                dst.Dispose();
            }
        }
        
        [Test]
        public void TestAppendEscString() {
            var dst = new Bytes("");
            try {
                // Test only near borders to speedup test
                AssertAppendEscStringRange(ref dst,       35,        92,  57); // exclude escape characters  
                AssertAppendEscStringRange(ref dst, 0x0007f0,  0x000810,  32); // test border:   0x800
                AssertAppendEscStringRange(ref dst, 0x00d7f0,  0x00d800,  16);
                // exclude surrogates:      0x00d800 - 0x00dfff
                AssertAppendEscStringRange(ref dst, 0x00e000,  0x00e010,  16);
                AssertAppendEscStringRange(ref dst, 0x00fff0,  0x010010,  32); // test border: 0x10000
                AssertAppendEscStringRange(ref dst, 0x10fff0,  0x110000,  16);

                AssertAppendEscStringChar(ref dst, '\b', "b");
                AssertAppendEscStringChar(ref dst, '\f', "f");
                AssertAppendEscStringChar(ref dst, '\n', "n");
                AssertAppendEscStringChar(ref dst, '\r', "r");
                AssertAppendEscStringChar(ref dst, '\t', "t");
                AssertAppendEscStringChar(ref dst, '"',  "\"");
                AssertAppendEscStringChar(ref dst, '\\', "\\");
            } finally {
                dst.Dispose();
            }
        }
        
        private void AssertAppendEscStringRange(ref Bytes dst, int from, int to, int count) {
            AreEqual(to - from, count);
            for (int codePoint = from; codePoint < to; codePoint++) {
                dst.Clear();
                string str = char.ConvertFromUtf32 (codePoint);
                Utf8JsonWriter.AppendEscString(ref dst, str.AsSpan());
                AreEqual($"\"{str}\"", dst.AsString());
            }
        }
        
        private void AssertAppendEscStringChar(ref Bytes dst, char escChar, string expect) {
            dst.Clear();
            string str = char.ConvertFromUtf32 (escChar);
            Utf8JsonWriter.AppendEscString(ref dst, str.AsSpan());
            AreEqual($"\"\\{expect}\"", dst.AsString());
        }


        [Test]
        public void TestBurstStringInterpolation() {
            using (Bytes bytes = new Bytes(128)) {
                int val = 42;
                int val2 = 43;
                char a = 'a';
                bytes.AppendStr32 ($"With Bytes {val} {val2} {a}");
                AreEqual("With Bytes 42 43 a", $"{bytes.ToStr32()}");

                var withValues = bytes.ToStr32();
                String32 str32 = new String32("World");
                String128 err = new String128($"Hello {str32.value} {withValues}");
                AreEqual("Hello World With Bytes 42 43 a", err.value);
            }
        }

        [Test]
        public void TestSerializeUnicode() {
            using (var s = new Utf8JsonWriter()) {
                s.InitSerializer();
                s.ObjectStart();
                s.MemberStr ("test", "a©€😎🌍");
                s.ObjectEnd();
                Console.WriteLine(s.json.AsString());
                string dst = s.json.AsString();
                AreEqual(@"{""test"":""a©€😎🌍""}", dst);
            }
        }
        
        [Test]
        public static void TestIsIntegral() {
            IsFalse (IsIntegral(""));
            IsFalse (IsIntegral("-"));
            
            IsFalse (IsIntegral("/"));
            IsTrue  (IsIntegral("0"));
            IsTrue  (IsIntegral("9"));
            IsFalse (IsIntegral(":"));
            
            IsFalse (IsIntegral("-0"));
            IsTrue  (IsIntegral("-1"));
            
            IsFalse (IsIntegral("01"));
            IsTrue  (IsIntegral("10"));
            IsTrue  (IsIntegral("99"));
        }
        
        private static bool IsIntegral(string str) {
            using (var bytes = new Bytes(str)) {
                return Bytes.IsIntegral(bytes.AsSpan());
            }
        }
        
        [Test]
        public void TestGuid() {
            var guid    = new Guid("11111111-0000-1111-0000-111111111111");
            var str     = guid.ToString();
            using (var bytes = new Bytes(str))
            using (var dest  = new Bytes(0))
            {
                IsTrue(Bytes.TryParseGuid(bytes.AsSpan(), out Guid result));
                AreEqual(guid, result);
                
                dest.AppendGuid(guid);
                AreEqual(str, bytes.AsString());
            }
        }
        
        [Test]
        public void TestBytesIsEqualPerf() {
            int count = 5; // 500_000_000;
            var left    = new Bytes ("abc");
            var right   = new Bytes ("123");
            for (int n = 0; n < count; n++) {
                left.IsEqual(right);
            }
        }
        
        private const string TestStr        = "0123";
        private const int    AppendCount    = 10; // 100_000_000;
        
        [Test]
        public void TestBytesAppendBytesOldPerf() {

            var bytes   = new Bytes (128);
            bytes.Set(TestStr);
            var target  = new Bytes (128);
            for (int n = 0; n < AppendCount; n++) {
                target.end = 0;
                target.AppendBytesOld(ref bytes);
            }
            AreEqual(TestStr, target.AsString());
        }
        
        [Test]
        public void TestBytesAppendBytesPerf() {
            var bytes   = new Bytes (TestStr);
            var target  = new Bytes (128);
            for (int n = 0; n < AppendCount; n++) {
                target.end = 0;
                target.AppendBytes(bytes);
            }
            
            AreEqual(TestStr, target.AsString());
        }
        
        [Test]
        public void TestBytesAppendBytes() {
            const int max = 64 + 8;
            var target  = new Bytes (max);
            var sb      = new StringBuilder();
            
            for (int n = 0; n < max; n++) {
                var str     = sb.ToString();
                var bytes   = new Bytes (str);
                target.end = 0;
                for (int o = 0; o < max; o++) { target.buffer[o] = 0; }
                target.AppendBytes(bytes);
                
                AreEqual(str, target.AsString(), $"n: {n}");
                int i = 0;
                for (; i < n; i ++) {
                    IsTrue(target.buffer[i] > 0);
                }
                for (; i < max; i ++) {
                    IsTrue(target.buffer[i] == 0);
                }
                sb.Append((char) (n + 48));
            }
        }
    }
    
    struct StructAssign
    {
        public int val;
    }
    
    public class TestStructBehavior
    {
        [Test]
        public void TestStructAssignment() {
            StructAssign var1 = new StructAssign();
            StructAssign var2 = var1; // copy as value;
            ref StructAssign ref1 = ref var1; 
            var1.val = 11;
            AreEqual(0, var2.val); // copy still unchanged
            AreEqual(11, ref1.val); // reference reflect changes
            
            modifyParam(var1);  // method parameter is copied as value, original value stay unchanged
            AreEqual(11, ref1.val);
            
            modifyRefParam(ref var1);
            AreEqual(12, ref1.val); // method parameter is given as reference value, original value is changed
        }

        // ReSharper disable once UnusedParameter.Local
        private void modifyParam(StructAssign param) {
            param.val = 12;
        }
        
        private void modifyRefParam(ref StructAssign param) {
            param.val = 12;
        }
        
        // in parameter is passed as reference (ref readonly) - it must not be changed
        // using in parameter degrade performance:
        // [c# 7.2 - Why would one ever use the "in" parameter modifier in C#? - Stack Overflow] https://stackoverflow.com/questions/52820372/why-would-one-ever-use-the-in-parameter-modifier-in-c
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void passByReadOnlyRef(in StructAssign param) {
            // param.val = 12;  // error CS8332: Cannot assign to a member of variable 'in Struct' because it is a readonly variable
        }
    }
}
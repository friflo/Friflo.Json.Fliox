// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox;
using NUnit.Framework;
using static NUnit.Framework.Assert;


namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Transform
{
    public static class TestJsonKey
    {
        [Test]
        public static void JsonKeyTests () {
            {
                var sb = new StringBuilder();
                var key = new JsonKey (123);
                key.AppendTo(sb);
                AreEqual ("123", sb.ToString());
            } {
                var sb = new StringBuilder();
                var key = new JsonKey (new Guid("11111111-2222-3333-4444-555555555555"));
                key.AppendTo(sb);
                AreEqual ("11111111-2222-3333-4444-555555555555", sb.ToString());
            } {
                var sb = new StringBuilder();
                var key = new JsonKey ("abc");
                key.AppendTo(sb);
                AreEqual ("abc", sb.ToString());
            } {
                var sb = new StringBuilder();
                var key = new JsonKey ("append UTF-8 string with more than 15 characters");
                key.AppendTo(sb);
                AreEqual ("append UTF-8 string with more than 15 characters", sb.ToString());
            }
        }
        
        [Test]
        public static void JsonKeyTests_Guid () {
            var guidStr = "11111111-2222-3333-4444-555555555555";
            var guidSrc = new Guid(guidStr);
            var guidKey = new JsonKey (guidSrc);
            var guidDst = guidKey.AsGuid();
            AreEqual(guidSrc, guidDst);
        }
        
        [Test]
        public static void JsonKeyTests_String () {
            {
                var str = new JsonKey ("short string");
                AreEqual("short string", str.AsString());
            }
            {
                var str = new JsonKey ("UTF-8 string with more than 15 characters");
                AreEqual("UTF-8 string with more than 15 characters", str.AsString());
            }
        }
        
        /// <summary>
        /// Compare performance of <see cref="ShortString"/> optimization using 15 / 16 characters 
        /// </summary>
        [Test]
        public static void JsonKeyTests_StringPerf () {
            int count       = 10; // 20_000_000;
            var value       = new Bytes("0---------1----"); // 15 characters. 
            var list        = new List<JsonKey>(count);
            var valueParser = new ValueParser();
            var _           = new JsonKey (ref value, ref valueParser, default); // force one time allocations
            var start       = GC.GetAllocatedBytesForCurrentThread();
            for (int n = 0; n < count; n++) {
                var key = new JsonKey (ref value, ref valueParser, default);
                list.Add(key);
            }
            var dif = GC.GetAllocatedBytesForCurrentThread() - start;
            AreEqual(count, list.Count);
            AreEqual(0, dif, "allocated bytes");
        }

        private const int Count = 50_000_000;
        
        /// <summary>
        /// Performance optimization significant for Unity:     50_000_000 => 0.6 sec
        /// </summary>
        [Test]
        public static void JsonKeyTests_StringEquals () {

            var foo1     = new JsonKey("foo");
            var foo2     = new JsonKey("foo");
            for (int n = 0; n < Count; n++) {
                var _ = foo1.IsEqual(foo2);
            }
        }
        
        /// <summary>
        /// Reference string comparison:                        50_000_000 => 13.0 sec
        /// </summary>
        [Test]
        public static void JsonKeyTests_StringEqualsReference () {
            var foo1     = new string("foo");
            var foo2     = new string("foo");
            for (int n = 0; n < Count; n++) {
                var _ = foo1 == foo2;
            }
        }
    }
}
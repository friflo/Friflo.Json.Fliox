// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;
using NUnit.Framework;
using static NUnit.Framework.Assert;


namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Base
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
                var key = new JsonKey (new Guid("f0f1f2f3-f4f5-f6f7-f8f9-fafbfcfdfeff"));
                key.AppendTo(sb);
                AreEqual ("f0f1f2f3-f4f5-f6f7-f8f9-fafbfcfdfeff", sb.ToString());
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
            var writer = new ObjectWriter(new TypeStore());
            {
                var str     = new JsonKey ("short string");
                AreEqual("short string", str.AsString());
            } {
                var str     = new JsonKey ("UTF-8 string with more than 15 characters");
                AreEqual("UTF-8 string with more than 15 characters", str.AsString());
            } {
                var str     = new JsonKey ("--\"--");   // support: "
                var result  = writer.Write(str);
                AreEqual("\"--\\\"--\"", result);
            } {
                var str     = new JsonKey ("--\\--");   // support: \
                var result  = writer.Write(str);
                AreEqual("\"--\\\\--\"", result);
            } {
                var str     = new JsonKey ("☀🌎♥👋");   // support unicode
                var result  = writer.Write(str);
                AreEqual("\"☀🌎♥👋\"", result);
            }
        }
        
        [Test]
        public static void JsonKeyTests_Serialization () {
            var mapper = new ObjectMapper(new TypeStore());
            {
                var key     = new JsonKey("123");
                var json    = mapper.writer.Write(key);
                AreEqual("123", json);
                var result  = mapper.Read<JsonKey>(json);
                IsTrue(result.IsLong());
                IsTrue(key.IsEqual(result));
            } {
                // keys are normalized to either: LONG, string or GUID
                var key     = new JsonKey("456");
                var json    = "\"456\""; // normalized to LONG
                var result  = mapper.Read<JsonKey>(json);
                IsTrue(result.IsLong());
                IsTrue(key.IsEqual(result));
            } {
                var key     = new JsonKey("55554444-3333-2222-1111-666677778888");
                var json    = mapper.writer.Write(key);
                AreEqual("\"55554444-3333-2222-1111-666677778888\"", json);
                var result  = mapper.Read<JsonKey>(json);
                IsTrue(result.IsGuid());
                IsTrue(key.IsEqual(result));
            } {
                var key     = new JsonKey("short string");
                var json    = mapper.writer.Write(key);
                AreEqual("\"short string\"", json);
                var result  = mapper.Read<JsonKey>(json);
                IsTrue(result.IsString());
                IsTrue(key.IsEqual(result));
            } {
                var key     = new JsonKey("string longer than 15 characters");
                var json    = mapper.writer.Write(key);
                AreEqual("\"string longer than 15 characters\"", json);
                var result  = mapper.Read<JsonKey>(json);
                IsTrue(result.IsString());
                IsTrue(key.IsEqual(result));
            }
        }
        
        /// <summary>
        /// Compare performance of <see cref="ShortStringUtils"/> optimization using 15 / 16 characters 
        /// </summary>
        [Test]
        public static void JsonKeyTests_StringPerf () {
            int count       = 10; // 20_000_000;
            var value       = new Bytes("0---------1----"); // 15 characters. 
            var list        = new List<JsonKey>(count);
            var _           = new JsonKey (value, default); // force one time allocations
            var start       = Mem.GetAllocatedBytes();
            for (int n = 0; n < count; n++) {
                var key = new JsonKey (value, default);
                list.Add(key);
            }
            var dif = Mem.GetAllocationDiff(start);
            AreEqual(count, list.Count);
            Mem.NoAlloc(dif);
        }
    }
}
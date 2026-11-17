// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;
using NUnit.Framework;
using static NUnit.Framework.Assert;
#pragma warning disable 649 // Field 'field' is never assigned

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
{
    // A dictionary value equal to default(TValue) was taken for null: the value 0, false, the first enum member or a
    // default struct was written as null, and reading the document failed ("Cannot assign null to float").
    public static class TestDictionaryZeroValue
    {
        private enum Kind { Zero, One }
        private struct Pair { public int A; public float B; }
        
        private static Dictionary<string, T> RoundTrip<T>(Dictionary<string, T> value, out string json) {
            using (var typeStore = new TypeStore(new StoreConfig()))
            using (var reader = new ObjectReader(typeStore))
            using (var writer = new ObjectWriter(typeStore))
            {
                json        = writer.Write(value);
                var result  = reader.Read<Dictionary<string, T>>(json);
                IsFalse(reader.Error.ErrSet, json + " " + reader.Error.msg);
                return result;
            }
        }
        
        private static void AssertRoundTrip<T>(Dictionary<string, T> source) {
            var result = RoundTrip(source, out var json);
            AreEqual(source.Count, result.Count, json);
            foreach (var pair in source)
                AreEqual(pair.Value, result[pair.Key], json);
        }
        
        [Test]
        public static void TestDictionaryZeroValues() {
            AssertRoundTrip(new Dictionary<string, float>   { ["zero"] = 0f,  ["one"] = 1.5f });
            AssertRoundTrip(new Dictionary<string, double>  { ["zero"] = 0.0 });
            AssertRoundTrip(new Dictionary<string, int>     { ["zero"] = 0,   ["three"] = 3 });
            AssertRoundTrip(new Dictionary<string, long>    { ["zero"] = 0L });
            AssertRoundTrip(new Dictionary<string, short>   { ["zero"] = 0 });
            AssertRoundTrip(new Dictionary<string, byte>    { ["zero"] = 0 });
            AssertRoundTrip(new Dictionary<string, uint>    { ["zero"] = 0 });
            AssertRoundTrip(new Dictionary<string, bool>    { ["false"] = false, ["true"] = true });
            AssertRoundTrip(new Dictionary<string, Kind>    { ["first"] = Kind.Zero, ["second"] = Kind.One });
            AssertRoundTrip(new Dictionary<string, Pair>    { ["default"] = default, ["set"] = new Pair { A = 1, B = 2 } });
            AssertRoundTrip(new Dictionary<string, string>  { ["empty"] = "" });
        }
        
        [Test]
        public static void TestDictionaryZeroValueWithSpecialKeys() {
            AssertRoundTrip(new Dictionary<string, float> { ["a\"b"] = 0f, ["c\\d"] = 0f, [""] = 0f, ["\n"] = 0f });
        }
        
        [Test]
        public static void TestDictionaryNullStillNull() {
            var source = new Dictionary<string, string> { ["null"] = null, ["text"] = "x" };
            var result = RoundTrip(source, out var json);
            IsTrue(json.Contains("\"null\":null"), json);
            IsNull(result["null"], json);
            var nullable = new Dictionary<string, int?> { ["null"] = null, ["zero"] = 0 };
            var nullableResult = RoundTrip(nullable, out json);
            IsNull(nullableResult["null"],  json);
            AreEqual(0, nullableResult["zero"], json);
        }
    }
}

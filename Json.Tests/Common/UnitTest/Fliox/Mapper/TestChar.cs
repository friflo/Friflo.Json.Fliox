// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Fliox.Mapper;
using NUnit.Framework;
using static NUnit.Framework.Assert;
#pragma warning disable 649 // Field 'field' is never assigned

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
{
    // A char used to have no mapper: a char member was not written at all ("{}") and read back as '\0'.
    // It is written as a JSON string of exactly one UTF-16 code unit.
    public static class TestChar
    {
        private class CharClass {
            public char     Value;
            public char?    NullableValue;
            public char[]   Array;
            public char     Property { get; set; }
        }
        private struct CharStruct {
            public char     Field;
            public char?    NullableField;
        }
        
        private static readonly char[] Chars = {
            'a', 'Z', ' ', '"', '\\', '/', '\n', '\t', '\0', '\u001f', '\u007f', 'é', '€', '中', '\ud800', '\udfff'
        };
        
        private static T RoundTrip<T>(T value, out string json) {
            using (var typeStore = new TypeStore(new StoreConfig()))
            using (var reader = new ObjectReader(typeStore))
            using (var writer = new ObjectWriter(typeStore))
            {
                json        = writer.Write(value);
                var result  = reader.Read<T>(json);
                IsFalse(reader.Error.ErrSet, json + " " + reader.Error.msg);
                return result;
            }
        }
        
        [Test]
        public static void TestCharClassRoundTrip() {
            foreach (var c in Chars) {
                var source = new CharClass { Value = c, NullableValue = c, Array = new [] { c, 'x', c }, Property = c };
                var result = RoundTrip(source, out var json);
                AreEqual(c,                     result.Value,           json);
                AreEqual((char?)c,              result.NullableValue,   json);
                AreEqual(source.Array,          result.Array,           json);
                AreEqual(c,                     result.Property,        json);
            }
        }
        
        [Test]
        public static void TestCharNullable() {
            var result = RoundTrip(new CharClass { NullableValue = null }, out var json);
            IsNull(result.NullableValue, json);
        }
        
        [Test]
        public static void TestCharStructRoundTrip() {
            foreach (var c in Chars) {
                var result = RoundTrip(new CharStruct { Field = c, NullableField = c }, out var json);
                AreEqual(c,         result.Field,           json);
                AreEqual((char?)c,  result.NullableField,   json);
            }
        }
        
        [Test]
        public static void TestCharWrittenAsOneCharacterString() {
            RoundTrip(new CharStruct { Field = 'a' }, out var json);
            IsTrue(json.Contains("\"Field\":\"a\""), json);
            RoundTrip(new CharStruct { Field = '"' }, out json);
            IsTrue(json.Contains("\"Field\":\"\\\"\""), json);
        }
        
        [Test]
        public static void TestCharReadErrors() {
            using (var typeStore = new TypeStore(new StoreConfig()))
            using (var reader = new ObjectReader(typeStore) { ErrorHandler = ObjectReader.NoThrow })
            {
                reader.Read<CharStruct>("{\"Field\":\"ab\"}");
                IsTrue(reader.Error.ErrSet);
                reader.Read<CharStruct>("{\"Field\":\"\"}");
                IsTrue(reader.Error.ErrSet);
                reader.Read<CharStruct>("{\"Field\":\"a\"}");
                IsFalse(reader.Error.ErrSet, reader.Error.msg.ToString());
            }
        }
    }
}

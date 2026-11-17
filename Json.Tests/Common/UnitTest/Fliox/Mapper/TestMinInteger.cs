// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Mapper;
using NUnit.Framework;
using static NUnit.Framework.Assert;
#pragma warning disable 649 // Field 'field' is never assigned

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
{
    // The smallest int and long used to be written as a bare "-": the digits were taken from the negated value, and
    // -int.MinValue (-long.MinValue) is not a positive number. The document was valid for nobody and read back as 0.
    public static class TestMinInteger
    {
        private class IntegerClass {
            public int      Int32;
            public long     Int64;
            public short    Int16;
            public sbyte    Int8;
            public int?     NullableInt32;
            public long?    NullableInt64;
            public int[]    Int32Array;
            public long[]   Int64Array;
        }
        private struct IntegerStruct {
            public int      Int32;
            public long     Int64;
        }
        
        private static readonly int[]  Ints  = { int.MinValue,  int.MinValue + 1,  int.MaxValue,  0,  -1 };
        private static readonly long[] Longs = { long.MinValue, long.MinValue + 1, long.MaxValue, 0L, -1L };
        
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
        public static void TestMinIntegerClassRoundTrip() {
            var source = new IntegerClass {
                Int32 = int.MinValue, Int64 = long.MinValue, Int16 = short.MinValue, Int8 = sbyte.MinValue,
                NullableInt32 = int.MinValue, NullableInt64 = long.MinValue, Int32Array = Ints, Int64Array = Longs
            };
            var result = RoundTrip(source, out var json);
            AreEqual(int.MinValue,      result.Int32);
            AreEqual(long.MinValue,     result.Int64);
            AreEqual(short.MinValue,    result.Int16);
            AreEqual(sbyte.MinValue,    result.Int8);
            AreEqual(int.MinValue,      result.NullableInt32);
            AreEqual(long.MinValue,     result.NullableInt64);
            AreEqual(Ints,              result.Int32Array);
            AreEqual(Longs,             result.Int64Array);
            IsTrue(json.Contains("-2147483648"));
            IsTrue(json.Contains("-9223372036854775808"));
        }
        
        [Test]
        public static void TestMinIntegerStructRoundTrip() {
            for (int n = 0; n < Ints.Length; n++) {
                var result = RoundTrip(new IntegerStruct { Int32 = Ints[n], Int64 = Longs[n] }, out _);
                AreEqual(Ints[n],  result.Int32);
                AreEqual(Longs[n], result.Int64);
            }
        }
        
        [Test]
        public static void TestMinIntegerAppend() {
            using (var format = new ValueFormat()) {
                format.InitTokenFormat();
                var bytes = new Bytes(0);
                foreach (var value in Ints) {
                    bytes.Clear();
                    format.AppendInt(ref bytes, value);
                    AreEqual(value.ToString(), bytes.AsString());
                }
                foreach (var value in Longs) {
                    bytes.Clear();
                    format.AppendLong(ref bytes, value);
                    AreEqual(value.ToString(), bytes.AsString());
                }
                bytes.Dispose();
            }
        }
    }
}

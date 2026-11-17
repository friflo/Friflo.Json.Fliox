// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Fliox.Mapper;
using NUnit.Framework;
using static NUnit.Framework.Assert;
#pragma warning disable 649 // Field 'field' is never assigned

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
{
    // NaN and the infinities are not JSON numbers. They are written as the strings "NaN", "Infinity" and "-Infinity"
    // and a float or double member reads them back.
    public static class TestNonFiniteFloat
    {
        private class FloatClass {
            public float    Float;
            public double   Double;
            public float[]  Array;
            public double?  Nullable;
        }
        private struct FloatStruct {
            public float    Float;
            public double   Double;
        }
        
        private static readonly float[] Values = { float.NaN, float.PositiveInfinity, float.NegativeInfinity, 1.5f };
        
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
        public static void TestNonFiniteClassRoundTrip() {
            foreach (var value in Values) {
                var source = new FloatClass { Float = value, Double = value, Array = new [] { value, 2f }, Nullable = value };
                var result = RoundTrip(source, out _);
                AreEqual(value,         result.Float);
                AreEqual((double)value, result.Double);
                AreEqual(value,         result.Array[0]);
                AreEqual(2f,            result.Array[1]);
                AreEqual((double)value, result.Nullable);
            }
        }
        
        [Test]
        public static void TestNonFiniteStructRoundTrip() {
            foreach (var value in Values) {
                var result = RoundTrip(new FloatStruct { Float = value, Double = value }, out _);
                AreEqual(value,         result.Float);
                AreEqual((double)value, result.Double);
            }
        }
        
        [Test]
        public static void TestNaNIsWrittenAsString() {
            var result = RoundTrip(new FloatClass { Float = float.NaN, Double = double.NaN, Array = new [] { float.NaN } }, out var json);
            IsTrue(float.IsNaN(result.Float));
            IsTrue(double.IsNaN(result.Double));
            IsTrue(float.IsNaN(result.Array[0]));
            IsTrue(json.Contains("\"NaN\""), json);
        }
        
        [Test]
        public static void TestOrdinaryNumberIsStillANumber() {
            RoundTrip(new FloatStruct { Float = 1.5f, Double = 2.5 }, out var json);
            AreEqual("{\"Float\":1.5,\"Double\":2.5}", json);
        }
        
        [Test]
        public static void TestOtherStringForFloatIsStillAnError() {
            using (var typeStore = new TypeStore(new StoreConfig()))
            using (var reader = new ObjectReader(typeStore) { ErrorHandler = ObjectReader.NoThrow })
            {
                reader.Read<FloatStruct>("{\"Float\":\"abc\"}");
                IsTrue(reader.Error.ErrSet);
            }
        }
    }
}

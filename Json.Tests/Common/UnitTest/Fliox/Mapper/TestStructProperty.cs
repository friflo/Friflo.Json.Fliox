// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using Friflo.Json.Fliox.Mapper;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
{
    // Properties of a struct are set on the boxed struct while it is read. They used to be set on a copy and were lost.
    public static class TestStructProperty
    {
        private struct FieldStruct      { public float Value; }
        private struct AutoPropStruct   { public float Value { get; set; } }
        private struct BackedPropStruct { private float _value; public float Value { get => _value; set => _value = value; } }
        private struct InitPropStruct   { public float Value { get; init; } }
        private struct MixedStruct {
            public int      Int32       { get; set; }
            public long     Int64       { get; set; }
            public byte     Byte        { get; set; }
            public ulong    UInt64      { get; set; }
            public double   Double      { get; set; }
            public bool     Bool        { get; set; }
            public string   Text        { get; set; }
            public int?     NullableInt { get; set; }
            public int      Field;
        }
        private class AutoPropClass { public float Value { get; set; } }
        
        private static T RoundTrip<T>(T value) {
            using (var typeStore = new TypeStore(new StoreConfig()))
            using (var reader = new ObjectReader(typeStore))
            using (var writer = new ObjectWriter(typeStore))
            {
                var json    = writer.Write(value);
                var result  = reader.Read<T>(json);
                IsFalse(reader.Error.ErrSet, reader.Error.msg.ToString());
                return result;
            }
        }
        
        [Test] public static void TestStructFieldRoundTrip()        => AreEqual(1.5f, RoundTrip(new FieldStruct      { Value = 1.5f }).Value);
        [Test] public static void TestStructAutoPropertyRoundTrip() => AreEqual(1.5f, RoundTrip(new AutoPropStruct   { Value = 1.5f }).Value);
        [Test] public static void TestStructBackedPropertyRoundTrip()=> AreEqual(1.5f, RoundTrip(new BackedPropStruct { Value = 1.5f }).Value);
        [Test] public static void TestStructInitPropertyRoundTrip() => AreEqual(1.5f, RoundTrip(new InitPropStruct   { Value = 1.5f }).Value);
        [Test] public static void TestClassPropertyRoundTrip()      => AreEqual(1.5f, RoundTrip(new AutoPropClass    { Value = 1.5f }).Value);
        
        [Test]
        public static void TestStructPropertiesOfAllKindsRoundTrip() {
            var value = new MixedStruct {
                Int32 = 7, Int64 = 1L << 40, Byte = 9, UInt64 = 1UL << 63, Double = 2.5, Bool = true, Text = "text", NullableInt = 3, Field = 4
            };
            var result = RoundTrip(value);
            AreEqual(7,         result.Int32);
            AreEqual(1L << 40,  result.Int64);
            AreEqual((byte)9,   result.Byte);
            AreEqual(1UL << 63, result.UInt64);
            AreEqual(2.5,       result.Double);
            IsTrue(             result.Bool);
            AreEqual("text",    result.Text);
            AreEqual(3,         result.NullableInt);
            AreEqual(4,         result.Field);
        }
    }
}

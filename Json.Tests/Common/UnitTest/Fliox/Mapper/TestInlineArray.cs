// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;
using Friflo.Json.Fliox.Mapper;
using NUnit.Framework;
using static NUnit.Framework.Assert;
#pragma warning disable 649 // Field 'field' is never assigned

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
{
    // A struct annotated with [InlineArray(N)] used to be mapped as an object without members: it was written as "{}"
    // and its elements were lost. It is written as a JSON array of N elements.
    public static class TestInlineArray
    {
        [InlineArray(4)] private struct FloatBlock  { private float  element; }
        [InlineArray(3)] private struct IntBlock    { private int    element; }
        
        private enum Kind { None, Speed, Armor }
        private struct Entry { public Kind Key; public float Value; }
        [InlineArray(2)] private struct EntryBlock  { private Entry  element; }
        
        private class BlockClass {
            public FloatBlock   Floats;
            public IntBlock     Ints { get; set; }
            public EntryBlock   Entries;
        }
        private struct BlockStruct {
            public FloatBlock   Floats;
            public EntryBlock   Entries;
        }
        private class ArrayClass {
            public BlockStruct[] Items;
        }
        
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
        
        private static FloatBlock Floats(float a, float b, float c, float d) {
            var block = new FloatBlock();
            block[0] = a; block[1] = b; block[2] = c; block[3] = d;
            return block;
        }
        private static EntryBlock Entries(Entry first, Entry second) {
            var block = new EntryBlock();
            block[0] = first; block[1] = second;
            return block;
        }
        
        [Test]
        public static void TestInlineArrayClassRoundTrip() {
            var source = new BlockClass {
                Floats  = Floats(1.5f, 2, 3, 4),
                Ints    = default,
                Entries = Entries(new Entry { Key = Kind.Speed, Value = 5 }, new Entry { Key = Kind.Armor, Value = 0.25f })
            };
            var ints = new IntBlock();
            ints[0] = int.MinValue + 1; ints[1] = 0; ints[2] = int.MaxValue;
            source.Ints = ints;
            var result = RoundTrip(source, out var json);
            AreEqual(1.5f,  result.Floats[0], json);
            AreEqual(4f,    result.Floats[3], json);
            AreEqual(int.MinValue + 1, result.Ints[0],  json);
            AreEqual(int.MaxValue,     result.Ints[2],  json);
            AreEqual(Kind.Speed,    result.Entries[0].Key,   json);
            AreEqual(5f,            result.Entries[0].Value, json);
            AreEqual(Kind.Armor,    result.Entries[1].Key,   json);
            AreEqual(0.25f,         result.Entries[1].Value, json);
            IsTrue(json.Contains("\"Floats\":[1.5,2,3,4]"), json);
        }
        
        [Test]
        public static void TestInlineArrayStructRoundTrip() {
            var source = new BlockStruct { Floats = Floats(1, 2, 3, 4), Entries = Entries(new Entry { Key = Kind.Armor, Value = 7 }, default) };
            var result = RoundTrip(source, out var json);
            AreEqual(3f,            result.Floats[2], json);
            AreEqual(Kind.Armor,    result.Entries[0].Key,   json);
            AreEqual(7f,            result.Entries[0].Value, json);
            AreEqual(Kind.None,     result.Entries[1].Key,   json);
        }
        
        [Test]
        public static void TestInlineArrayEmptyBlock() {
            var result = RoundTrip(new BlockStruct(), out var json);
            for (int n = 0; n < 4; n++)
                AreEqual(0f, result.Floats[n], json);
            IsTrue(json.Contains("\"Floats\":[0,0,0,0]"), json);
        }
        
        [Test]
        public static void TestInlineArrayInArrayOfStructs() {
            var source = new ArrayClass { Items = new [] {
                new BlockStruct { Floats = Floats(1, 2, 3, 4) },
                new BlockStruct { Floats = Floats(5, 6, 7, 8), Entries = Entries(default, new Entry { Key = Kind.Speed, Value = 9 }) }
            }};
            var result = RoundTrip(source, out var json);
            AreEqual(2,     result.Items.Length,            json);
            AreEqual(4f,    result.Items[0].Floats[3],      json);
            AreEqual(5f,    result.Items[1].Floats[0],      json);
            AreEqual(9f,    result.Items[1].Entries[1].Value, json);
        }
        
        [Test]
        public static void TestInlineArrayShorterLongerAndNull() {
            using (var typeStore = new TypeStore(new StoreConfig()))
            using (var reader = new ObjectReader(typeStore) { ErrorHandler = ObjectReader.NoThrow })
            {
                var shorter = reader.Read<BlockStruct>("{\"Floats\":[1,2]}");
                IsFalse(reader.Error.ErrSet, reader.Error.msg.ToString());
                AreEqual(2f, shorter.Floats[1]);
                AreEqual(0f, shorter.Floats[2]);
                AreEqual(0f, shorter.Floats[3]);
                
                var empty = reader.Read<BlockStruct>("{\"Floats\":[]}");
                IsFalse(reader.Error.ErrSet, reader.Error.msg.ToString());
                AreEqual(0f, empty.Floats[0]);
                
                var nullBlock = reader.Read<BlockStruct>("{\"Floats\":null}");
                IsFalse(reader.Error.ErrSet, reader.Error.msg.ToString());
                AreEqual(0f, nullBlock.Floats[0]);
                
                var missing = reader.Read<BlockStruct>("{}");
                IsFalse(reader.Error.ErrSet, reader.Error.msg.ToString());
                AreEqual(0f, missing.Floats[3]);
                
                reader.Read<BlockStruct>("{\"Floats\":[1,2,3,4,5]}");
                IsTrue(reader.Error.ErrSet);
                IsTrue(reader.Error.msg.ToString().Contains("(4)"), reader.Error.msg.ToString());
            }
        }
    }
}
#endif

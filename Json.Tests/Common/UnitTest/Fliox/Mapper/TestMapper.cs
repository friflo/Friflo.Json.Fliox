// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Numerics;
using Friflo.Json.Burst;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;

using static NUnit.Framework.Assert;

// using static Friflo.Json.Tests.Common.UnitTest.NoCheck;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
{
    public enum EnumClass {
        Value1 = 11,
        Value2 = 22,
        Value3 = 11, // duplicate constant value - C#/.NET maps these enum values to the first value using same constant   
    }

    public class TestMapper : LeakTestsFixture
    { 
        [Test]
        public void TestEnumMapper() {
            // C#/.NET behavior in case of duplicate enum v
            AreEqual(EnumClass.Value1, EnumClass.Value3);
            var value1 = "\"Value1\"";
            var value2 = "\"Value2\"";
            var value3 = "\"Value3\"";
            var hello =  "\"hello\"";
            var num11 =  "11";
            var num999 = "999";

            using (var typeStore = new TypeStore(new StoreConfig()))
            using (var enc   = new ObjectReader(typeStore) { ErrorHandler =  ObjectReader.NoThrow} )
            using (var write = new ObjectWriter(typeStore))
            {
                EnumClass   enumValue1  = enc.Read<EnumClass> (value1);
                EnumClass?  enumValue2  = enc.Read<EnumClass?>(value2);
                write.Write(enumValue1);
                write.Write(enumValue2);
                
                // --- check allocation Read()
                var start = Mem.GetAllocatedBytes();
                enumValue1  = enc.Read<EnumClass>(value1);
                enumValue2  = enc.Read<EnumClass?>(value2);
                var diff    = Mem.GetAllocationDiff(start);
                Mem.NoAlloc(diff);
                AreEqual(EnumClass.Value1, enumValue1);
                AreEqual(EnumClass.Value2, enumValue2);
                
                // --- check allocation Write()

                start = Mem.GetAllocatedBytes();
                write.WriteAsBytes(enumValue1);
                write.WriteAsBytes(enumValue2);
                diff = Mem.GetAllocatedBytes() - start;
                Mem.NoAlloc(diff);


                AreEqual(EnumClass.Value1, enc.Read<EnumClass>(value1));
                AreEqual(EnumClass.Value2, enc.Read<EnumClass>(value2));
                AreEqual(EnumClass.Value3, enc.Read<EnumClass>(value3));
                AreEqual(EnumClass.Value1, enc.Read<EnumClass>(value3));
                
                enc.Read<EnumClass>(hello);
                StringAssert.Contains(" Cannot assign string to enum EnumClass. got: 'hello'", enc.Error.msg.AsString());

                AreEqual(EnumClass.Value1, enc.Read<EnumClass>(num11));
                
                enc.Read<EnumClass>(num999);
                StringAssert.Contains("Cannot assign number to enum EnumClass. got: 999", enc.Error.msg.AsString());

                var result = write.Write(EnumClass.Value1);
                AreEqual("\"Value1\"", result);
                
                result = write.Write(EnumClass.Value2);
                AreEqual("\"Value2\"", result);
                
                result = write.Write(EnumClass.Value3);
                AreEqual("\"Value1\"", result);
                
                // --- Nullable
                AreEqual(EnumClass.Value1, enc.Read<EnumClass?>(value1));
                AreEqual(EnumClass.Value2, enc.Read<EnumClass?>(value2));
                AreEqual(EnumClass.Value3, enc.Read<EnumClass?>(value3));
                AreEqual(EnumClass.Value1, enc.Read<EnumClass?>(value3));
                
                result = write.Write<EnumClass?>(null);
                AreEqual("null", result);
                
                result = write.Write<EnumClass?>(EnumClass.Value1);
                AreEqual("\"Value1\"", result);
                
            }
        }
        
        
        [Test]
        public void TestBigInteger() {
            const string bigIntStr = "1234567890123456789012345678901234567890";
            var bigIntNum = BigInteger.Parse(bigIntStr);
            using (var typeStore    = new TypeStore(new StoreConfig()))
            using (var enc          = new ObjectReader(typeStore) { ErrorHandler =  ObjectReader.NoThrow} )
            using (var bigInt       = new Bytes($"\"{bigIntStr}\"")) {
                AreEqual(bigIntNum, enc.Read<BigInteger>(bigInt));
            }
        }

        class RecursiveClass {
            public RecursiveClass recField;
        }
        
        [Test]
        public void TestMaxDepth() {
            using (var typeStore    = new TypeStore(new StoreConfig()))
            using (var enc          = new ObjectReader(typeStore) { ErrorHandler =  ObjectReader.NoThrow} )
            using (var writer       = new ObjectWriter(typeStore))
            using (var recDepth1    = new Bytes("{\"recField\":null}"))
            using (var recDepth2    = new Bytes("{\"recField\":{\"recField\":null}}"))
            {
                // --- JsonReader
                enc.MaxDepth = 1;
                var result = enc.Read<RecursiveClass>(recDepth1);
                AreEqual(JsonEvent.EOF, enc.JsonEvent);
                IsNull(result.recField);

                enc.Read<RecursiveClass>(recDepth2);
                AreEqual("JsonParser/JSON error: nesting in JSON document exceed maxDepth: 1 path: 'recField' at position: 13", enc.Error.msg.AsString());
                
                // --- JsonWriter
                // maxDepth: 1
                writer.MaxDepth = 1;
                writer.Write(new RecursiveClass());
                AreEqual(0, writer.Level);
                // no exception

                var rec = new RecursiveClass { recField = new RecursiveClass() };
                var e = Throws<InvalidOperationException>(() => writer.Write(rec));
                AreEqual("JsonParser: maxDepth exceeded. maxDepth: 1", e.Message);
                AreEqual(2, writer.Level);
                
                // maxDepth: 0
                writer.MaxDepth = 0;
                writer.Write(1);
                AreEqual(0, writer.Level);
                // no exception
                
                var e2 = Throws<InvalidOperationException>(() => writer.Write(new RecursiveClass()));
                AreEqual("JsonParser: maxDepth exceeded. maxDepth: 0", e2.Message);
                AreEqual(1, writer.Level);
            }
        }

        class Base {
            public int baseField = 0;
        }

        [NamingPolicy(NamingPolicyType.Default)]
        class Derived : Base {
            [Serialize] private     int derivedField = 0;
            [Serialize] private     int Int32 { get; set; }  // compiler auto generate backing field

            public void AssertFields() {
                AreEqual(10, baseField);
                AreEqual(20, Int32);
                AreEqual(21, derivedField);
            }
        }

        [Test]
        public void TestPropertyWithIndexOperator()
        {
            using (var typeStore    = new TypeStore(new StoreConfig()))
            using (var json         = new Bytes("{\"X\":1,\"Y\":2,\"Z\":3}"))
            using (var reader       = new ObjectReader(typeStore) { ErrorHandler =  ObjectReader.NoThrow} )
            using (var writer       = new ObjectWriter(typeStore))
            {
                var result = reader.Read<Vec3>(json);
                AreEqual(new Vec3{ X= 1, Y = 2, Z = 3 }, result);
                var jsonResult = writer.Write(result);
                AreEqual(json.AsString(), jsonResult);
            }
        }
    }
}

/// <summary>
/// Similar to <see cref="Vector3"/>.
/// .NET 7 introduced indexer (compiler adds a property named Item) 
/// </summary>
struct Vec3
{
    public int X;
    public int Y;
    public int Z;
    
    public int this[int index] { get => 0; set => _ = value; }
}
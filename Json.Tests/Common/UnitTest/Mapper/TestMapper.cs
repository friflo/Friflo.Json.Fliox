using System;
using System.Numerics;
using Friflo.Json.Burst;
using Friflo.Json.Mapper;
using NUnit.Framework;

using static NUnit.Framework.Assert;
// using static Friflo.Json.Tests.Common.UnitTest.NoCheck;


namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    public enum EnumClass {
        Value1 = 11,
        Value2 = 22,
        Value3 = 11, // duplicate constant value - C#/.NET maps these enum values to the first value using same constant   
    }

    public class TestMapper
    { 
        [Test]
        public void TestEnumMapper() {
            // C#/.NET behavior in case of duplicate enum v
            AreEqual(EnumClass.Value1, EnumClass.Value3);

            using (TypeStore typeStore = new TypeStore())
            using (JsonReader enc = new JsonReader(typeStore, JsonReader.NoThrow))
            using (JsonWriter write = new JsonWriter(typeStore))
            using (var value1 = new Bytes("\"Value1\""))
            using (var value2 = new Bytes("\"Value2\""))
            using (var value3 = new Bytes("\"Value3\""))
            using (var hello =  new Bytes("\"hello\""))
            using (var num11 =  new Bytes("11"))
            using (var num999 = new Bytes("999"))
            {
                AreEqual(EnumClass.Value1, enc.Read<EnumClass>(value1));
                AreEqual(EnumClass.Value2, enc.Read<EnumClass>(value2));
                AreEqual(EnumClass.Value3, enc.Read<EnumClass>(value3));
                AreEqual(EnumClass.Value1, enc.Read<EnumClass>(value3));
                
                enc.Read<EnumClass>(hello);
                StringAssert.Contains(" Cannot assign string to enum value. Value unknown. Expect: Friflo.Json.Tests.Common.UnitTest.Mapper.EnumClass, got: 'hello'", enc.Error.msg.ToString());

                AreEqual(EnumClass.Value1, enc.Read<EnumClass>(num11));
                
                enc.Read<EnumClass>(num999);
                StringAssert.Contains("Cannot assign number to enum value. Value unknown. Expect: Friflo.Json.Tests.Common.UnitTest.Mapper.EnumClass, got: 999", enc.Error.msg.ToString());

                write.Write(EnumClass.Value1);
                AreEqual("\"Value1\"", write.bytes.ToString());
                
                write.Write(EnumClass.Value2);
                AreEqual("\"Value2\"", write.bytes.ToString());
                
                write.Write(EnumClass.Value3);
                AreEqual("\"Value1\"", write.bytes.ToString());
                
                // --- Nullable
                AreEqual(EnumClass.Value1, enc.Read<EnumClass?>(value1));
                AreEqual(EnumClass.Value2, enc.Read<EnumClass?>(value2));
                AreEqual(EnumClass.Value3, enc.Read<EnumClass?>(value3));
                AreEqual(EnumClass.Value1, enc.Read<EnumClass?>(value3));
                
                write.Write<EnumClass?>(null);
                AreEqual("null", write.bytes.ToString());
                
                write.Write<EnumClass?>(EnumClass.Value1);
                AreEqual("\"Value1\"", write.bytes.ToString());
                
            }
        }
        
        [Test]
        public void TestBigInteger() {
            const string bigIntStr = "1234567890123456789012345678901234567890";
            var bigIntNum = BigInteger.Parse(bigIntStr);
            using (TypeStore typeStore = new TypeStore())
            using (JsonReader enc = new JsonReader(typeStore, JsonReader.NoThrow))
            using (var bigInt = new Bytes($"\"{bigIntStr}\"")) {
                AreEqual(bigIntNum, enc.Read<BigInteger>(bigInt));
            }
        }

        class RecursiveClass {
            public RecursiveClass recField;
        }
        
        [Test]
        public void TestMaxDepth() {
            using (TypeStore typeStore =    new TypeStore())
            using (JsonReader enc =         new JsonReader(typeStore, JsonReader.NoThrow))
            using (JsonWriter writer =      new JsonWriter(typeStore))
            using (var recDepth1 = new Bytes("{\"recField\":null}"))
            using (var recDepth2 = new Bytes("{\"recField\":{\"recField\":null}}"))
            {
                // --- JsonReader
                enc.maxDepth = 1;
                var result = enc.Read<RecursiveClass>(recDepth1);
                AreEqual(JsonEvent.EOF, enc.JsonEvent);
                IsNull(result.recField);

                enc.Read<RecursiveClass>(recDepth2);
                AreEqual("JsonParser/JSON error: nesting in JSON document exceed maxDepth: 1 path: 'recField' at position: 13", enc.Error.msg.ToString());
                
                // --- JsonWriter
                // maxDepth: 1
                writer.maxDepth = 1;
                writer.Write(new RecursiveClass());
                AreEqual(0, writer.Level);
                // no exception

                var rec = new RecursiveClass { recField = new RecursiveClass() };
                var e = Throws<InvalidOperationException>(() => writer.Write(rec));
                AreEqual("JsonParser: maxDepth exceeded. maxDepth: 1", e.Message);
                AreEqual(2, writer.Level);
                
                // maxDepth: 0
                writer.maxDepth = 0;
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

        class Derived : Base {
            public int derivedField = 0;
        }
        

        [Test]
        public void TestDerivedClass() {
            using (var typeStore = new TypeStore())
            using (var derivedJson = new Bytes("{\"derivedField\":22,\"baseField\":11}"))
            using (var reader = new JsonReader(typeStore, JsonReader.NoThrow))
            using (var writer = new JsonWriter(typeStore))
            {
                var result = reader.Read<Derived>(derivedJson);
                AreEqual(11, result.baseField);
                AreEqual(22, result.derivedField);
                
                writer.Write(result);
                AreEqual(derivedJson.ToString(), writer.Output.ToString());
            }
        }

    }
}
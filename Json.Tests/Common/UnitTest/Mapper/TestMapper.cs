using System;
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
            using (JsonReader enc = new JsonReader(typeStore))
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
            }
        }

        class RecursiveClass {
            public RecursiveClass recField;
        }
        
        [Test]
        public void TestMaxDepth() {
            using (TypeStore typeStore =    new TypeStore())
            using (JsonReader enc =         new JsonReader(typeStore))
            using (JsonWriter writer =      new JsonWriter(typeStore))
            using (var recDepth1 = new Bytes("{\"recField\":null}"))
            using (var recDepth2 = new Bytes("{\"recField\":{\"recField\":null}}"))
            {
                enc.maxDepth = 1;
                var result = enc.Read<RecursiveClass>(recDepth1);
                AreEqual(JsonEvent.EOF, enc.parser.Event);
                IsNull(result.recField);

                enc.Read<RecursiveClass>(recDepth2);
                AreEqual("JsonParser/JSON error: nesting in JSON document exceed maxDepth: 1 path: 'recField' at position: 13", enc.Error.msg.ToString());
                //
                writer.maxDepth = 1;
                writer.Write(new RecursiveClass());
                // no exception

                var rec = new RecursiveClass { recField = new RecursiveClass() };
                var e = Throws<InvalidOperationException>(() => writer.Write(rec));
                AreEqual("JsonParser: maxDepth exceeded. maxDepth: 1", e.Message);
            }
        }
    }
}
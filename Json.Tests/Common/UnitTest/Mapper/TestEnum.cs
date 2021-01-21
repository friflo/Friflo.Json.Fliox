using Friflo.Json.Burst;
using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Map;
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

    public class TestEnum
    { 
        [Test]
        public void TestEnumMapper() {
            // C#/.NET behavior in case of duplicate enum v
            AreEqual(EnumClass.Value1, EnumClass.Value3);

            using (TypeStore typeStore = new TypeStore(new DefaultTypeResolver(ResolverMode.Debug)))
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
    }
}
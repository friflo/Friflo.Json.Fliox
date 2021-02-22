using System;
using System.Numerics;
using Friflo.Json.Burst;
using Friflo.Json.Mapper;
using Friflo.Json.Tests.Unity.Utils;
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

    public class TestMapper : LeakTestsFixture
    { 
        [Test] public void  TestEnumMapperReflect()   { TestEnumMapper(TypeAccess.Reflection); }
        [Test] public void  TestEnumMapperIL()        { TestEnumMapper(TypeAccess.IL); }
        
        private void TestEnumMapper(TypeAccess typeAccess) {
            // C#/.NET behavior in case of duplicate enum v
            AreEqual(EnumClass.Value1, EnumClass.Value3);
            var value1 = "\"Value1\"";
            var value2 = "\"Value2\"";
            var value3 = "\"Value3\"";
            var hello =  "\"hello\"";
            var num11 =  "11";
            var num999 = "999";

            using (var typeStore = new TypeStore(null, new StoreConfig(typeAccess)))
            using (JsonReader enc = new JsonReader(typeStore, JsonReader.NoThrow))
            using (JsonWriter write = new JsonWriter(typeStore))
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
        
        [Test] public void  TestBigIntegerReflect()   { TestBigInteger(TypeAccess.Reflection); }
        [Test] public void  TestBigIntegerIL()        { TestBigInteger(TypeAccess.IL); }
        
        private void TestBigInteger(TypeAccess typeAccess) {
            const string bigIntStr = "1234567890123456789012345678901234567890";
            var bigIntNum = BigInteger.Parse(bigIntStr);
            using (var typeStore = new TypeStore(null, new StoreConfig(typeAccess)))
            using (JsonReader enc = new JsonReader(typeStore, JsonReader.NoThrow))
            using (var bigInt = new Bytes($"\"{bigIntStr}\"")) {
                AreEqual(bigIntNum, enc.Read<BigInteger>(bigInt));
            }
        }

        class RecursiveClass {
            public RecursiveClass recField;
        }
        
        [Test] public void  TestMaxDepthReflect()   { TestMaxDepth(TypeAccess.Reflection); }
        [Test] public void  TestMaxDepthIL()        { TestMaxDepth(TypeAccess.IL); }
        
        private void TestMaxDepth(TypeAccess typeAccess) {
            using (var typeStore = new TypeStore(null, new StoreConfig(typeAccess)))
            using (JsonReader enc =         new JsonReader(typeStore, JsonReader.NoThrow))
            using (JsonWriter writer =      new JsonWriter(typeStore))
            using (var recDepth1 = new Bytes("{\"recField\":null}"))
            using (var recDepth2 = new Bytes("{\"recField\":{\"recField\":null}}"))
            {
                // --- JsonReader
                enc.MaxDepth = 1;
                var result = enc.Read<RecursiveClass>(recDepth1);
                AreEqual(JsonEvent.EOF, enc.JsonEvent);
                IsNull(result.recField);

                enc.Read<RecursiveClass>(recDepth2);
                AreEqual("JsonParser/JSON error: nesting in JSON document exceed maxDepth: 1 path: 'recField' at position: 13", enc.Error.msg.ToString());
                
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

        class Derived : Base {
            private     int derivedField = 0;
            private     int Int32 { get; set; }  // compiler auto generate backing field

            public void AssertFields() {
                AreEqual(10, baseField);
                AreEqual(20, Int32);
                AreEqual(21, derivedField);
            }
        }

        [Test] public void  TestDerivedClassReflect()   { TestDerivedClass(TypeAccess.Reflection); }
        [Test] public void  TestDerivedClassIL()        { TestDerivedClass(TypeAccess.IL); }
        
        private void TestDerivedClass(TypeAccess typeAccess) {
            using (var typeStore = new TypeStore(null, new StoreConfig(typeAccess)))
            using (var derivedJson = new Bytes("{\"Int32\":20,\"derivedField\":21,\"baseField\":10}"))
            using (var reader = new JsonReader(typeStore, JsonReader.NoThrow))
            using (var writer = new JsonWriter(typeStore))
            {
                var result = reader.Read<Derived>(derivedJson);
                result.AssertFields();
                var jsonResult = writer.Write(result);
                AreEqual(derivedJson.ToString(), jsonResult);
            }
        }

        
        interface ITest {
            
        }

        class TestImpl : ITest {
            
        }

        public interface IInstanceFactory<T> {
            T CreateInstance(string name);
        }

        class TestFactory : IInstanceFactory<ITest>
        {
            public ITest CreateInstance(string name) {
                return new TestImpl();
            }
        }
        
        [Test] [Ignore("")] public void  TestInterfaceReflect()   { TestInterface(TypeAccess.Reflection); }
        [Test] [Ignore("")] public void  TestInterfaceIL()        { TestInterface(TypeAccess.IL); }
        
        private void TestInterface(TypeAccess typeAccess) {
            var json = "{}";
            using (var typeStore = new TypeStore(null, new StoreConfig(typeAccess)))
            using (var reader = new JsonReader(typeStore, JsonReader.NoThrow))
            using (var writer = new JsonWriter(typeStore))
            {
                // typeStore.AddInstanceFactory(new TestFactory());
                var result = reader.Read<ITest>(json);
                var jsonResult = writer.Write(result);
                AreEqual(json, jsonResult);
            }
        }
        
    }
}
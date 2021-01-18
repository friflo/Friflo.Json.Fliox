// using static NUnit.Framework.Assert;
using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Utils;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;
using static Friflo.Json.Tests.Common.Utils.NoCheck;

namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    public struct TestStruct {
        public int              key;
    }
    
    public class TestClass {
        public TestClass    selfReference; // test cyclic references
        public TestClass    testChild;
        // public int              key;
        public int[]            intArray;

    }

    public class TestNoAllocation
    {
        string testClassJson = $@"
{{
    ""intArray"":null,
    ""testChild"":null,
    ""key"":42,
    ""unknownObject"": {{
        ""anotherUnknown"": 42
    }},
    ""unknownArray"": [42],
    ""unknownStr"": ""str"", 
    ""unknownNumber"":999,
    ""unknownBool"":true,
    ""unknownNull"":null
}}";
        
        [Test]
        public void TestNoAlloc() {
            var memLog = new MemoryLogger(100, 100, MemoryLog.Enabled);
            
            var reusedClass = new TestClass();

            var hashMap = new HashMapOpen<Bytes, string>(11, new Bytes("__REMOVED"));

            var reusedArrDbl =      new double[3];
            var reusedArrFlt =      new float[3];
            var reusedArrLng =      new long[3];
            var reusedArrInt =      new int[3];
            var reusedArrShort =    new short[3];
            var reusedArrByte =     new byte[3];
            var reusedArrBool =     new bool[2];
            
            var reusedListDbl =     new List<double>();
            var reusedListFlt =     new List<float>();
            var reusedListLng =     new List<long>();
            var reusedListInt =     new List<int>();
            var reusedListShort =   new List<short>();
            var reusedListByte =    new List<byte>();
            var reusedListBool =    new List<bool>();
            
            using (TypeStore typeStore = new TypeStore(new DebugTypeResolver()))
            using (JsonReader enc = new JsonReader(typeStore))
            using (JsonWriter write = new JsonWriter(typeStore))
            using (var hello =      new Bytes ("\"hello\""))
            using (var @double =    new Bytes ("12.5"))
            using (var @long =      new Bytes ("42"))
            using (var @true =      new Bytes ("true"))
            using (var @null =      new Bytes ("null"))
            using (var dblOverflow= new Bytes ("1.7976931348623157E+999"))
                // --- arrays
            using (var arrNum =     new Bytes ("[1,2,3]"))
            using (var arrStr =     new Bytes ("[\"hello\"]"))
            using (var arrBln =     new Bytes ("[true, false]"))
            using (var arrObj =     new Bytes ("[{\"key\":42}]"))
            using (var arrNull =    new Bytes ("[null]"))
            using (var arrArrNum =  new Bytes ("[[1,2,3]]"))
            using (var arrArrObj =  new Bytes ("[[{\"key\":42}]]"))
                // --- class/map
            using (var testClass =  new Bytes (testClassJson)) 
            using (var mapNull =    new Bytes ("{\"key\":null}"))
            using (var mapNum =     new Bytes ("{\"key\":42}"))
            using (var mapBool =    new Bytes ("{\"key\":true}"))
            using (var mapStr =     new Bytes ("{\"key\":\"value\"}"))
            using (var mapMapNum =  new Bytes ("{\"key\":{\"key\":42}}"))
            using (var mapNum2 =    new Bytes ("{\"str\":44}"))
            using (var invalid =    new Bytes ("invalid")) {
                int iterations = 1000;
                for (int n = 0; n < iterations; n++) {
                    memLog.Snapshot();
                    Var result = new Var();
                    
                    //IsTrue(enc.Read<double> (@double, ref result));     AreEqual(12.5d, result.Dbl);
                    //IsTrue(enc.Read<float>  (@double, ref result));     AreEqual(12.5,  result.Flt);
                    //
                    IsTrue(enc.Read<long>       (@long, ref result));     AreEqual(42, result.Lng);
                    IsTrue(enc.Read<int>        (@long, ref result));     AreEqual(42, result.Int);
                    IsTrue(enc.Read<short>      (@long, ref result));     AreEqual(42, result.Short);
                    IsTrue(enc.Read<byte>       (@long, ref result));     AreEqual(42, result.Byte);
                    
                    IsTrue(enc.Read<bool>       (@true, ref result));     AreEqual(true, result.Bool);
                    
                    IsTrue(enc.Read<object>     (@null, ref result));     AreEqual(null, result.Obj);
                    
                    IsTrue(enc.Read<string[]>   (@null, ref result));     AreEqual(null, result.Obj); // no alloc only, if not containing string
                    
                    IsTrue(enc.Read<double[]>   (@null, ref result));     AreEqual(null, result.Obj);
                    IsTrue(enc.Read<float[]>    (@null, ref result));     AreEqual(null, result.Obj);
                    IsTrue(enc.Read<long[]>     (@null, ref result));     AreEqual(null, result.Obj);
                    IsTrue(enc.Read<int[]>      (@null, ref result));     AreEqual(null, result.Obj);
                    IsTrue(enc.Read<short[]>    (@null, ref result));     AreEqual(null, result.Obj);
                    IsTrue(enc.Read<byte[]>     (@null, ref result));     AreEqual(null, result.Obj);
                    IsTrue(enc.Read<bool[]>     (@null, ref result));     AreEqual(null, result.Obj);
                    
                    IsTrue(enc.ReadTo(arrNum, reusedArrDbl));
                    IsTrue(enc.ReadTo(arrNum, reusedArrFlt));
                    IsTrue(enc.ReadTo(arrNum, reusedArrLng));
                    IsTrue(enc.ReadTo(arrNum, reusedArrInt));
                    IsTrue(enc.ReadTo(arrNum, reusedArrShort));
                    IsTrue(enc.ReadTo(arrNum, reusedArrByte));
                    IsTrue(enc.ReadTo(arrBln, reusedArrBool));
                    
                    IsTrue(enc.ReadTo(arrNum, reusedListDbl));


                    IsTrue(enc.ReadTo(mapNum, reusedClass));
                    
                    
                    
                    
                    // AreEqual(42, reusedClass.key);


                    // Ensure minimum required type lookups
                    if (n > 1) {
                        // AreEqual(22, enc.typeCache.LookupCount);
                        AreEqual( 0, enc.typeCache.StoreLookupCount);
                        AreEqual( 0, enc.typeCache.TypeCreationCount);
                    }
                    enc.typeCache.ClearCounts();
                }
            }
            memLog.AssertNoAllocations();
        }

        [Test]
        public void TestHashMapOpen() {
            var memLog = new MemoryLogger(100, 100, MemoryLog.Enabled);
            var hashMap = new HashMapOpen<Bytes, string>(7, new Bytes("__REMOVED"));

            var key1 = new BytesStr("key1");
            var key2 = new BytesStr("key2");
            var key3 = new BytesStr("key3");
            var key4 = new BytesStr("key4");
            var key5 = new BytesStr("key5");
            int iterations = 1000;
            var dict = new Dictionary<BytesStr, String>();
            
            for (int n = 0; n < iterations; n++) {
                memLog.Snapshot();
                if (n == 0) {
                    hashMap.Put(ref key1.value, "key 1");
                    hashMap.Put(ref key2.value, "key 2");
                    hashMap.Put(ref key3.value, "key 3");
                    hashMap.Put(ref key4.value, "key 4");
                    hashMap.Put(ref key5.value, "key 5");
                    dict.TryAdd(key1, "key 1");
                    dict.TryAdd(key2, "key 2");
                    dict.TryAdd(key3, "key 3");
                    dict.TryAdd(key4, "key 4");
                    dict.TryAdd(key5, "key 5");
                }

                bool useHashMap = true;
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (useHashMap) {
                    hashMap.Get(ref key1.value);
                    hashMap.Get(ref key2.value);
                    hashMap.Get(ref key3.value);
                    hashMap.Get(ref key4.value);
                    hashMap.Get(ref key5.value);
                } else {
                    dict.TryGetValue(key1, out string val1);
                    dict.TryGetValue(key2, out string val2);
                    dict.TryGetValue(key3, out string val3);
                    dict.TryGetValue(key4, out string val4);
                    dict.TryGetValue(key5, out string val5);
                }
            }
            memLog.AssertNoAllocations();
        }
    }

    /// <summary>
    /// Using Bytes directly leads to boxing/unboxing. See comment in <see cref="Bytes.Equals(object)"/>
    /// </summary>
    public class BytesStr
    {
        public Bytes value = new Bytes("");

        public BytesStr(string str) {
            value.Set(str);
        }

        public override bool Equals(object obj) {
            if (obj == null)
                return false;
            if (obj is BytesStr other)
                return value.IsEqualBytes(ref other.value);
            return false;
        }

        public override int GetHashCode() {
            return value.GetHashCode();
        }
    }
    
}
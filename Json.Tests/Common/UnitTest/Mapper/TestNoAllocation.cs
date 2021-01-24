using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Utils;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

// using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Common.Utils.SimpleAssert;

// ReSharper disable UnassignedField.Global
// ReSharper disable UnusedVariable

namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    public struct NoAllocStruct {
        public int              key;
    }

    public enum SomeEnum {
        Value1 = 11,
        Value2 = 12,
    }
    
    public class TestClass {
        public TestClass    selfReference; // test cyclic references
        public TestClass    testChild;
        // public int              key;
        public int[]        intArray;
        public SomeEnum     someEnum;

    }

    public class TestNoAllocation
    {
        string testClassJson = $@"
{{
    ""testChild"": {{
        ""someEnum"":""Value2""
    }},
    ""intArray"":[1,2,3],
    ""key"":42,
    ""someEnum"":""Value1"",
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
            
            var reusedListNulDbl =     new List<double?>();
            var reusedListNulFlt =     new List<float?>();
            var reusedListNulLng =     new List<long?>();
            var reusedListNulInt =     new List<int?>();
            var reusedListNulShort =   new List<short?>();
            var reusedListNulByte =    new List<byte?>();
            var reusedListNulBool =    new List<bool?>();
            
            using (TypeStore typeStore = new TypeStore())
            using (JsonReader enc = new JsonReader(typeStore))
            using (JsonWriter write = new JsonWriter(typeStore))
                
            using (var hello =      new Bytes ("\"hello\""))
            using (var @double =    new Bytes ("12.5"))
            using (var @long =      new Bytes ("42"))
            using (var @true =      new Bytes ("true"))
            using (var @null =      new Bytes ("null"))
            using (var value1 =     new Bytes ("\"Value1\""))
            using (var dblOverflow= new Bytes ("1.7976931348623157E+999"))
                // --- arrays
            using (var arrFlt =     new Bytes ("[11.5,12.5,13.5]"))
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

                    
                    // --------------------------------- primitives -------------------------------
                    { IsTrue(enc.Read (@double, out double  result));   AreEqual(12.5d, result); }
                    { IsTrue(enc.Read (@double, out float   result));   AreEqual(12.5,  result); }
                    //
                    { IsTrue(enc.Read (@long,   out long    result));   AreEqual(42,    result); }
                    { IsTrue(enc.Read (@long,   out int     result));   AreEqual(42,    result); }
                    { IsTrue(enc.Read (@long,   out short   result));   AreEqual(42,    result); }
                    { IsTrue(enc.Read (@long,   out byte    result));   AreEqual(42,    result); }
                     
                    { IsTrue(enc.Read (@true,   out bool    result));   AreEqual(true,  result); }

                    { IsTrue(enc.Read (@null,   out object  result));   AreEqual(null,  result); }

                    // --------------------------------- array -----------------------------------
                    { IsTrue(enc.Read (@null, out string[]  result));   AreEqual(null, result); } // no alloc only, if not containing string
 
                    { IsTrue(enc.Read (@null, out double[]  result));   AreEqual(null, result);  }
                    { IsTrue(enc.Read (@null, out float[]   result));   AreEqual(null, result); }
                    { IsTrue(enc.Read (@null, out long[]    result));   AreEqual(null, result); }
                    { IsTrue(enc.Read (@null, out int[]     result));   AreEqual(null, result); }
                    { IsTrue(enc.Read (@null, out short[]   result));   AreEqual(null, result); }
                    { IsTrue(enc.Read (@null, out byte[]    result));   AreEqual(null, result); }
                    { IsTrue(enc.Read (@null, out bool[]    result));   AreEqual(null, result); }
                    
                    NotNull(enc.ReadTo(arrFlt, reusedArrDbl,   out bool _));             AreEqual(11.5d, reusedArrDbl[0]);
                    NotNull(enc.ReadTo(arrNum, reusedArrFlt,   out bool _));
                    NotNull(enc.ReadTo(arrNum, reusedArrLng,   out bool _));
                    NotNull(enc.ReadTo(arrNum, reusedArrInt,   out bool _));
                    NotNull(enc.ReadTo(arrNum, reusedArrShort, out bool _));
                    NotNull(enc.ReadTo(arrNum, reusedArrByte,  out bool _));
                    NotNull(enc.ReadTo(arrBln, reusedArrBool,  out bool _));

                    // --------------------------------- enum -----------------------------------
                    {
                        SomeEnum res = enc.Read<SomeEnum>(value1);          IsTrue(SomeEnum.Value1 == res);  // avoid boxing. AreEqual() boxes
                    } {
                        SomeEnum? res = enc.Read<SomeEnum?>(@null);         AreEqual(null, res);
                    } {
                        enc.Read(hello, out SomeEnum?  result);             IsTrue(enc.Error.ErrSet);
                    }

                    // --------------------------------- List<> ---------------------------------
                    // non nullable elements
                    NotNull(enc.ReadTo(arrFlt, reusedListDbl,   out bool _));              AreEqual(11.5d, reusedListDbl[0]);
                    NotNull(enc.ReadTo(arrNum, reusedListFlt,   out bool _));
                    NotNull(enc.ReadTo(arrNum, reusedListLng,   out bool _));
                    NotNull(enc.ReadTo(arrNum, reusedListInt,   out bool _));
                    NotNull(enc.ReadTo(arrNum, reusedListShort, out bool _));
                    NotNull(enc.ReadTo(arrNum, reusedListByte,  out bool _));
                    NotNull(enc.ReadTo(arrBln, reusedListBool,  out bool _));
                    
                    // nullable elements
                    NotNull(enc.ReadTo(arrFlt, reusedListNulDbl,    out bool _));           AreEqual(11.5d, reusedListDbl[0]);
                    NotNull(enc.ReadTo(arrNum, reusedListNulFlt,    out bool _));
                    NotNull(enc.ReadTo(arrNum, reusedListNulLng,    out bool _));
                    NotNull(enc.ReadTo(arrNum, reusedListNulInt,    out bool _));
                    NotNull(enc.ReadTo(arrNum, reusedListNulShort, out bool _));
                    NotNull(enc.ReadTo(arrNum, reusedListNulByte,  out bool _));
                    NotNull(enc.ReadTo(arrBln, reusedListNulBool,  out bool _));

                    // --------------------------------- class ---------------------------------
                    NotNull(enc.ReadTo(testClass, reusedClass,  out bool _));
                    AreEqual(3,               reusedClass.intArray.Length);
                    IsTrue(SomeEnum.Value1 == reusedClass.someEnum); // avoid boxing. AreEqual() boxes
                    IsTrue(SomeEnum.Value2 == reusedClass.testChild.someEnum); // avoid boxing. AreEqual() boxes
                    
                    // AreEqual(42, reusedClass.key);


                    // Ensure minimum required type lookups
                    if (n > 1) {
                        AreEqual( 41, enc.typeCache.LookupCount);
                        AreEqual(  0, enc.typeCache.StoreLookupCount);
                        AreEqual(  0, enc.typeCache.TypeCreationCount);
                    }
                    enc.typeCache.ClearCounts();
                }
                AreEqual(587000,   enc.parser.ProcessedBytes);
            }
            memLog.AssertNoAllocations();
        }

        [Test]
        public void TestHashMapOpen() {
            var memLog = new MemoryLogger(100, 100, MemoryLog.Enabled);
            using (var removed =    new Bytes("__REMOVED"))
                
            using (var key1 = new BytesStr("key1"))
            using (var key2 = new BytesStr("key2"))
            using (var key3 = new BytesStr("key3"))
            using (var key4 = new BytesStr("key4"))
            using (var key5 = new BytesStr("key5")) 
            {
                var hashMap = new HashMapOpen<Bytes, string>(7, removed);
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
            }
            memLog.AssertNoAllocations();
        }
    }

    /// <summary>
    /// Using Bytes directly leads to boxing/unboxing. See comment in <see cref="Bytes.Equals(object)"/>
    /// </summary>
    public class BytesStr : IDisposable
    {
        public Bytes value;

        public BytesStr(string str) {
            value = new Bytes(str);
        }
        
        public void Dispose() {
            value.Dispose();
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
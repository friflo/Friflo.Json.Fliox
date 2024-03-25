// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;

// using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Common.Utils.SimpleAssert;

// ReSharper disable UnassignedField.Global
// ReSharper disable UnusedVariable

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
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
        public int[]        intArray;

    }

    public class TestNoAllocation : LeakTestsFixture
    {
        string testClassJson = $@"
{{
    ""testChild"": {{
    }},
    ""intArray"":[1,2,3],
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

            var reusedArrDbl =          new double[3];
            var reusedArrFlt =          new float[3];
            var reusedArrLng =          new long[3];
            var reusedArrInt =          new int[3];
            var reusedArrShort =        new short[3];
            var reusedArrByte =         new byte[3];
            var reusedArrBool =         new bool[2];
            
            var reusedListDbl =         new List<double>();
            var reusedListFlt =         new List<float>();
            var reusedListLng =         new List<long>();
            var reusedListInt =         new List<int>();
            var reusedListShort =       new List<short>();
            var reusedListByte =        new List<byte>();
            var reusedListBool =        new List<bool>();

            var reusedListNulDbl =      new List<double?>();
            var reusedListNulFlt =      new List<float?>();
            var reusedListNulLng =      new List<long?>();
            var reusedListNulInt =      new List<int?>();
            var reusedListNulShort =    new List<short?>();
            var reusedListNulByte =     new List<byte?>();
            var reusedListNulBool =     new List<bool?>();
            
            var reusedDictionaryInt =   new Dictionary<int,    int>();
            var reusedDictionaryGuid =  new Dictionary<Guid,   int>();
            // var reusedDictionaryStr = new Dictionary<string, int>(); // allocate memory
            
            var reusedHashSet =         new HashSet<int>();
 
            var hello =         "\"hello\"";
            var @double =       "12.5";
            var @long =         "42";
            var @true =         "true";
            var @null =         "null";
            var value1 =        "\"Value1\"";

             // --- arrays
            var arrFlt =        "[11.5,12.5,13.5]";
            var arrNum =        "[1,2,3]";
            var arrBln =        "[true, false]";
            
            var mapInt =        "{\"20\": 123 }";
            var mapGuid =       "{\"12341234-1111-2222-3333-444455556666\": 124 }";
            
            var guid = new Guid("12341234-1111-2222-3333-444455556666");

             // --- class/map
            var testClass =     testClassJson; 
            
            using (var typeStore    = new TypeStore(new StoreConfig()))
            using (var enc          = new ObjectReader(typeStore) { ErrorHandler =  ObjectReader.NoThrow} )
            using (var write        = new ObjectWriter(typeStore))
            {
                int iterations = 1000;
                for (int n = 0; n < iterations; n++) {
                    memLog.Snapshot();

                    
                    // --------------------------------- primitives -------------------------------
                    AreEqual(12.5d,   enc.Read<double>  (@double)); 
                    AreEqual(12.5,    enc.Read<float>   (@double)); 
                    
                    AreEqual(42,      enc.Read<long>    (@long  )); 
                    AreEqual(42,      enc.Read<int>     (@long  )); 
                    AreEqual(42,      enc.Read<short>   (@long  )); 
                    AreEqual(42,      enc.Read<byte>    (@long  )); 
                    
                    AreEqual(true,    enc.Read<bool>    (@true  )); 

                    AreEqual(null,    enc.Read<object>  (@null  )); 

                    // --------------------------------- array -----------------------------------
                    AreEqual(null,    enc.Read<string[]>(@null));  // no alloc only, if not containing string
   
                    AreEqual(null,    enc.Read<double[]>(@null));
                    AreEqual(null,    enc.Read<float[]> (@null));
                    AreEqual(null,    enc.Read<long[]>  (@null));
                    AreEqual(null,    enc.Read<int[]>   (@null));
                    AreEqual(null,    enc.Read<short[]> (@null));
                    AreEqual(null,    enc.Read<byte[]>  (@null));
                    AreEqual(null,    enc.Read<bool[]>  (@null));
                    
                    NotNull(enc.ReadTo(arrFlt, reusedArrDbl,   false));             AreEqual(11.5d, reusedArrDbl[0]);
                    NotNull(enc.ReadTo(arrNum, reusedArrFlt,   false));
                    NotNull(enc.ReadTo(arrNum, reusedArrLng,   false));
                    NotNull(enc.ReadTo(arrNum, reusedArrInt,   false));
                    NotNull(enc.ReadTo(arrNum, reusedArrShort, false));
                    NotNull(enc.ReadTo(arrNum, reusedArrByte,  false));
                    NotNull(enc.ReadTo(arrBln, reusedArrBool,  false));

                    // --------------------------------- enum -----------------------------------
                    {
                        SomeEnum res = enc.Read<SomeEnum>(value1);          IsTrue(SomeEnum.Value1 == res);  // avoid boxing. AreEqual() boxes
                    } {
                        SomeEnum? res = enc.Read<SomeEnum?>(@null);         AreEqual(null, res);
                    } {
                        enc.Read<SomeEnum>(hello);        IsTrue(enc.Error.ErrSet);
                    }

                    // --------------------------------- List<> ---------------------------------
                    // non nullable elements
                    NotNull(enc.ReadTo(arrFlt, reusedListDbl,   false));              AreEqual(11.5d, reusedListDbl[0]);
                    NotNull(enc.ReadTo(arrNum, reusedListFlt,   false));
                    NotNull(enc.ReadTo(arrNum, reusedListLng,   false));
                    NotNull(enc.ReadTo(arrNum, reusedListInt,   false));
                    NotNull(enc.ReadTo(arrNum, reusedListShort, false));
                    NotNull(enc.ReadTo(arrNum, reusedListByte,  false));
                    NotNull(enc.ReadTo(arrBln, reusedListBool,  false));
                    
                    // nullable elements
                    NotNull(enc.ReadTo(arrFlt, reusedListNulDbl,   false));           AreEqual(11.5d, reusedListDbl[0]);
                    NotNull(enc.ReadTo(arrNum, reusedListNulFlt,   false));
                    NotNull(enc.ReadTo(arrNum, reusedListNulLng,   false));
                    NotNull(enc.ReadTo(arrNum, reusedListNulInt,   false));
                    NotNull(enc.ReadTo(arrNum, reusedListNulShort, false));
                    NotNull(enc.ReadTo(arrNum, reusedListNulByte,  false));
                    NotNull(enc.ReadTo(arrBln, reusedListNulBool,  false));

                    // --------------------------------- class ---------------------------------
                    enc.ReadTo(testClass, reusedClass, false);
                    AreEqual(3,  reusedClass.intArray.Length);
                    
                    // ------------------------------ Dictionary<,> ----------------------------
                    NotNull(enc.ReadTo(mapInt, reusedDictionaryInt, false));
                    AreEqual(123, reusedDictionaryInt[20]);
                    
                    NotNull(enc.ReadTo(mapGuid, reusedDictionaryGuid, false));
                    AreEqual(124, reusedDictionaryGuid[guid]);
                    
                    // NotNull(enc.ReadTo(mapInt, reusedDictionaryStr));
                    // AreEqual(123, reusedDictionaryStr["20"]);
                    
                    // -------------------------------- HashSet<> ------------------------------
                    NotNull(enc.ReadTo(arrNum, reusedHashSet, false));
                    IsTrue(reusedHashSet.Contains(1));


                    // Ensure minimum required type lookups
                    if (n > 1) {
                        AreEqual( 44, enc.TypeCache.LookupCount);
                        AreEqual(  0, enc.TypeCache.StoreLookupCount);
                        AreEqual(  0, enc.TypeCache.TypeCreationCount);
                    }
                    enc.TypeCache.ClearCounts();
                }
                AreEqual(569000,   enc.ProcessedBytes);
            }
            memLog.AssertNoAllocations();
        }

        /* Note: Did not use Bytes as key type T in HashMapOpen<,> anymore
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
        } */
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
                return value.IsEqual(other.value);
            return false;
        }

        public override int GetHashCode() {
            return value.GetHashCode();
        }
    }
    
}
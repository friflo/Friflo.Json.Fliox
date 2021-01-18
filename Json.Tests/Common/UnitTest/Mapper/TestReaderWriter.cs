using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using Friflo.Json.Burst;
using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Utils;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;
// using static Friflo.Json.Tests.Common.UnitTest.NoCheck;

namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{

    
    public class TestReaderWriter : LeakTestsFixture
    {
        private TypeStore createStore() {
            TypeStore      typeStore = new TypeStore(new DebugTypeResolver());
            typeStore.RegisterType("Sub", typeof( Sub ));
            return typeStore;
        }

        String JsonSimpleObj = "{'val':5}";
        
        [Test]
        public void EncodeJsonSimple()  {
            using (TypeStore typeStore = createStore())
            using (Bytes bytes = CommonUtils.FromString(JsonSimpleObj))
            {
                JsonSimple obj = (JsonSimple) EncodeJson(bytes, typeof(JsonSimple), typeStore);
                AreEqual(5L, obj.val);
            }
        }
        
        int                 num2 =              2;
        
        private Object EncodeJson(Bytes json, Type type, TypeStore typeStore) {
            Object ret = null;
            using (var enc = new JsonReader(typeStore)) {
                // StopWatch stopwatch = new StopWatch();
                for (int n = 0; n < num2; n++) {
                    ret = enc.Read(json, type);
                    if (ret == null)
                        throw new FrifloException(enc.Error.msg.ToString());
                }
                AreEqual(0, enc.SkipInfo.Sum);
                // FFLog.log("EncodeJson: " + json + " : " + stopwatch.Time());
            }
            return ret;
        }
        
        private bool EncodeJsonTo(Bytes json, Object obj, TypeStore typeStore) {
            bool ret = false;
            using (JsonReader enc = new JsonReader(typeStore)) {
                // StopWatch stopwatch = new StopWatch();
                for (int n = 0; n < num2; n++) {
                    ret = enc.ReadTo(json, obj);
                    if (!ret)
                        throw new FrifloException(enc.Error.msg.ToString());
                }
                AreEqual(2, enc.SkipInfo.Sum); // 2 => discriminator: "$type" is skipped, there is simply no field for a discriminator
                // FFLog.log("EncodeJsonTo: " + json + " : " + stopwatch.Time());
                return ret;
            }
        }
        
        private static void CheckMap (IDictionary <String, JsonSimple> map) {
            AreEqual (2, map. Count);
            JsonSimple key1 = map [ "key1" ];
            AreEqual (         1L, key1.val);
            JsonSimple key2 = map [ "key2" ];
            AreEqual (       null, key2);
        }
        
        private static void CheckList (IList <Sub> list) {
            AreEqual (           4, list. Count);
            AreEqual (         11L, list [0] .i64);
            AreEqual (        null, list [1] );
            AreEqual (         13L, list [2] .i64);
            AreEqual (         14L, list [3] .i64);
        }

        private static void CheckJsonComplex (JsonComplex obj) {
            AreEqual (6400000000000000000, obj.i64);
            AreEqual (          32, obj.i32);
            AreEqual ((short)   16, obj.i16);
            AreEqual ((byte)     8, obj.i8);
            AreEqual (        22.5, obj.dbl);
            AreEqual (       11.5f, obj.flt);
            AreEqual (  "string-ý", obj.str);
            AreEqual (        null, obj.strNull);
            AreEqual ("_\"_\\_\b_\f_\r_\n_\t_", obj.escChars);
            AreEqual (        null, obj.n);
            AreEqual (         99L, (( Sub)obj.subType).i64);
            AreEqual (        true, obj.t);
            AreEqual (       false, obj.f);
            AreEqual (          1L, obj.sub.i64);
            AreEqual (          43, obj.structValue.val);
            AreEqual (         21L, obj.arr[0].i64);
            AreEqual (        null, obj.arr[1]    );
            AreEqual (         23L, obj.arr[2].i64);
            AreEqual (         24L, obj.arr[3].i64);
            AreEqual (           4, obj.arr. Length);
            CheckList (obj.list);
            CheckList (obj.list2);
            CheckList (obj.list3);
            CheckList (obj.list4);
            CheckList (obj.listDerived);
            CheckList (obj.listDerivedNull);
            AreEqual (      "str0", obj.listStr [0] );
            AreEqual (        101L, ((Sub)obj.listObj [0]) .i64 );
            AreEqual (        42, obj.listStruct[0].val );
            CheckMap (obj.map);
            CheckMap (obj.map2);
            CheckMap (obj.map3);
            CheckMap (obj.map4);
            AreEqual (         1, obj.mapStruct["key1"].val); 
            CheckMap (obj.mapDerived);
            CheckMap (obj.mapDerivedNull);
            AreEqual (      "str1", obj.map5 [ "key1" ]);
            AreEqual (        null, obj.map5 [ "key2" ]);
        }
        
        private static void SetMap (IDictionary <String, JsonSimple> map) {
            // order not defined for HashMap
            map [ "key1" ]= new  JsonSimple(1L) ;
            map [ "key2" ]= null ;
        }
        
        private static void SetList (IList <Sub> list) {
            list. Add (new Sub(11L));
            list. Add (null);
            list. Add (new Sub(13L));
            list. Add (new Sub(14L));
        }
        
        private static void SetComplex (JsonComplex obj) {
            obj.i64 = 6400000000000000000;
            obj.i32 = 32;
            obj.i16 = 16;
            obj.i8  = 8;
            obj.dbl = 22.5;
            obj.flt = 11.5f;
            obj.str = "string-ý";
            obj.strNull = null;
            obj.escChars =  "_\"_\\_\b_\f_\r_\n_\t_";
            obj.n = null; 
            obj.subType = new Sub(99);
            obj.t = true;
            obj.f = false;
            obj.sub = new Sub();
            obj.sub.i64 = 1L;
            obj.structValue = new JsonStruct(43);
            obj.arr = new Sub[4];
            obj.arr[0] = new Sub(21L);
            obj.arr[1]    = null;
            obj.arr[2] = new Sub(23L);
            obj.arr[3] = new Sub(24L);
            obj.list =  new List <Sub>();
            SetList (obj.list);
            SetList (obj.list2);
            obj.list3 =  new List <Sub>();
            SetList (obj.list3);
            SetList (obj.list4);
            SetList (obj.listDerived);
            obj.listDerivedNull = new DerivedList();
            SetList (obj.listDerivedNull);
            obj.listStr. Add ("str0");
            obj.listObj. Add (new Sub(101));
            obj.listStruct.Add(new JsonStruct(42));
            obj.map = new Dictionary <String, JsonSimple>();
            SetMap (obj.map);
            SetMap (obj.map2);
            obj.map3 = new Dictionary <String, JsonSimple>();
            SetMap (obj.map3);
            SetMap (obj.map4);
            obj.mapStruct["key1"] = new JsonStruct(1);
            SetMap (obj.mapDerived);
            obj.mapDerivedNull = new DerivedMap();
            SetMap (obj.mapDerivedNull);
            obj.map5 = new Dictionary <String, String>();
            obj.map5 [ "key1" ] = "str1" ;
            obj.map5 [ "key2" ] = null ;
            obj.i64Arr = new [] {1, 2, 3};
        }

        [Test]
        public void EncodeJsonComplex() {
            using (TypeStore typeStore = createStore())
            using (Bytes bytes = CommonUtils.FromFile("assets/codec/complex.json")) {
                JsonComplex obj = (JsonComplex) EncodeJson(bytes, typeof(JsonComplex), typeStore);
                CheckJsonComplex(obj);
            }
        }
        
        [Test]
        public void EncodeJsonToComplex()   {
            using (TypeStore typeStore = createStore())
            using (Bytes bytes = CommonUtils.FromFile("assets/codec/complex.json")) {
                JsonComplex obj = new JsonComplex();
                IsTrue(EncodeJsonTo(bytes, obj, typeStore));
                CheckJsonComplex(obj);
            }
        }
        
        [Test]
        public void WriteJsonComplex()
        {
            using (TypeStore typeStore = createStore()) {
                JsonComplex obj = new JsonComplex();
                SetComplex(obj);
                using (JsonWriter writer = new JsonWriter(typeStore)) {
                    writer.Write(obj);

                    using (JsonReader enc = new JsonReader(typeStore)) {
                        JsonComplex res = enc.Read<JsonComplex>(writer.Output);
                        if (res == null)
                            Fail(enc.Error.msg.ToString());
                        CheckJsonComplex(res);
                    }
                }
            }
        }

        [Test]
        public void ReadPrimitive() {
#if !UNITY_EDITOR
            GC.Collect();
            long startBytes = GC.GetAllocatedBytesForCurrentThread();
            
            TestPrimitiveInternal();
            
            if (NoCheck.checkStaticMemoryUsage) {
                GC.Collect();
                long endBytes = GC.GetAllocatedBytesForCurrentThread();
                Console.WriteLine($"startBytes: {startBytes} endBytes: {endBytes}");
                Assert.AreEqual(startBytes, endBytes);
            }
#else
            TestPrimitiveInternal();
#endif
        }

        public struct TestStruct {
            public int key;
        }

        public class TestChild {
        }

        public class TestClass {
            public TestClass    selfReference; // test cyclic references
            public TestChild    testChild;
            public int          key;
            public string       str;
            public BigInteger   bigInt;
            public int[]        intArray;

            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((TestClass) obj);
            }

            private bool Equals(TestClass other) {
                return key == other.key && bigInt.Equals(other.bigInt);
            }

            // ReSharper disable NonReadonlyMemberInGetHashCode
            public override int GetHashCode() {
                return key + bigInt.GetHashCode();
            }
        }

        const string BigInt = "1234567890123456789012345678901234567890";

        private void TestPrimitiveInternal() {
            string testClassJson = $@"
{{
    ""intArray"":null,
    ""testChild"":null,
    ""key"":42,
    ""bigInt"":""{BigInt}"",
    ""unknownObject"": {{
        ""anotherUnknown"": 42
    }},
    ""unknownArray"": [42],
    ""unknownStr"": ""str"", 
    ""unknownNumber"":999,
    ""unknownBool"":true,
    ""unknownNull"":null
}}";
            using (TypeStore typeStore = createStore())
            using (JsonReader enc = new JsonReader(typeStore))
            using (JsonWriter write = new JsonWriter(typeStore))
            using (var hello =      new Bytes ("\"hello\""))
            using (var @double =    new Bytes ("12.5"))
            using (var @long =      new Bytes ("42"))
            using (var @true =      new Bytes ("true"))
            using (var @null =      new Bytes ("null"))
            using (var bigInt =     new Bytes (BigInt))
            using (var dblOverflow= new Bytes ("1.7976931348623157E+999"))
            using (var bigIntStr =  new Bytes ($"\"{BigInt}\""))
            using (var bigIntStrN = new Bytes ($"\"{BigInt}n\""))
            using (var dateTime =   new Bytes ("2021-01-14T09:59:40.101Z"))
            using (var dateTimeStr= new Bytes ("\"2021-01-14T09:59:40.101Z\""))
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
                int iterations = 2; // dont use < 2
                for (int n = 0; n < iterations; n++) {
                    AreEqual("hello",   enc.Read<string>(hello));
                    AreEqual(JsonEvent.EOF, enc.parser.Event);
                    
                    AreEqual(12.5,                      enc.ReadValue<double>   (@double));
                    AreEqual(12.5,                      enc.Read<double?>       (@double));
                    AreEqual(null,                      enc.Read<double?>       (@null));

                    enc.ReadValue<double>(@null);
                    StringAssert.Contains("Cannot assign null to primitive. Expect:", enc.Error.msg.ToString());
#if !UNITY_EDITOR
                    {
                        enc.ThrowException = true;
                        var e = Throws<FrifloException>(() => enc.ReadValue<double>(@null));
                        StringAssert.Contains("Cannot assign null to primitive. Expect:", e.Message);
                        enc.ThrowException = false;
                    }
#endif
                    // error cases
                    enc.ReadValue<double>(@true);
                    StringAssert.Contains("Cannot assign bool to primitive. Expect:", enc.Error.msg.ToString());
                    enc.ReadValue<double>(hello);
                    StringAssert.Contains("Cannot assign string to primitive. Expect:", enc.Error.msg.ToString());
                    enc.ReadValue<double>(arrNum);
                    StringAssert.Contains("Cannot assign array to primitive. Expect:", enc.Error.msg.ToString());
                    enc.ReadValue<double>(mapNum);
                    StringAssert.Contains("Cannot assign object to primitive. Expect:", enc.Error.msg.ToString());
                    enc.ReadValue<long>(bigInt);
                    StringAssert.Contains("Value out of range when parsing long:", enc.Error.msg.ToString());
                    enc.ReadValue<float>(bigInt);
                    AreEqual(true, enc.Error.ErrSet);
                    // StringAssert.Contains("float value out of range. val:", enc.Error.msg.ToString()); // Unity has different error message
                    enc.ReadValue<double>   (dblOverflow);
                    AreEqual(true, enc.Error.ErrSet);
                    // StringAssert.Contains("double value out of range. val:", enc.Error.msg.ToString());// Unity has different error message


                    AreEqual(12.5,                      enc.ReadValue<float>    (@double));
                    AreEqual(12.5,                      enc.Read<float?>        (@double));
                    AreEqual(null,                      enc.Read<float?>        (@null));
                    enc.ReadValue<float>(@null);
                    StringAssert.Contains("Cannot assign null to primitive. Expect:", enc.Error.msg.ToString());

                    AreEqual(42,        enc.ReadValue<long>     (@long));
                    AreEqual(42,        enc.Read<long>          (@long));
                    AreEqual(42,        enc.Read<long?>         (@long));
                    AreEqual(null,      enc.Read<long?>         (@null));
                    enc.ReadValue<long>(@null);
                    StringAssert.Contains("Cannot assign null to primitive. Expect:", enc.Error.msg.ToString());

                    AreEqual(42,        enc.ReadValue<int>      (@long));
                    AreEqual(42,        enc.Read<int>           (@long));
                    AreEqual(42,        enc.Read<int?>          (@long));
                    AreEqual(null,      enc.Read<int?>          (@null));
                    enc.ReadValue<int>(@null);
                    StringAssert.Contains("Cannot assign null to primitive. Expect:", enc.Error.msg.ToString());

                    AreEqual(42,        enc.ReadValue<short>    (@long));
                    AreEqual(42,        enc.Read<short>         (@long));
                    AreEqual(42,        enc.Read<short?>        (@long));
                    AreEqual(null,      enc.Read<short?>        (@null));
                    enc.ReadValue<short>(@null);
                    StringAssert.Contains("Cannot assign null to primitive. Expect:", enc.Error.msg.ToString());

                    AreEqual(42,        enc.ReadValue<byte>     (@long));
                    AreEqual(42,        enc.Read<byte>          (@long));
                    AreEqual(42,        enc.Read<byte?>         (@long));
                    AreEqual(null,      enc.Read<byte?>         (@null));
                    enc.ReadValue<byte>(@null);
                    StringAssert.Contains("Cannot assign null to primitive. Expect:", enc.Error.msg.ToString());

                    AreEqual(true,      enc.ReadValue<bool>     (@true));
                    AreEqual(true,      enc.Read<bool>          (@true));
                    AreEqual(true,      enc.Read<bool?>         (@true));
                    AreEqual(null,      enc.Read<bool?>         (@null));
                    enc.ReadValue<bool>(@null);
                    StringAssert.Contains("Cannot assign null to primitive. Expect:", enc.Error.msg.ToString());

                    
                    AreEqual(null,      enc.Read<object>(@null));
                    AreEqual(JsonEvent.EOF, enc.parser.Event);
                    
                    AreEqual(null,      enc.Read<object>(invalid));
                    AreEqual("JsonParser/JSON error: unexpected character while reading value. Found: i path: '(root)' at position: 1",     enc.Error.msg.ToString());
                    
                    // ------------------------------------- class -------------------------------------
                    {
                        BigInteger bigIntVal = BigInteger.Parse(bigInt.ToString());
                        var expect = new TestClass { key = 42, bigInt = bigIntVal };
                        var value = enc.Read<TestClass>(testClass);
                        if (JsonEvent.EOF != enc.parser.Event)
                            Fail(enc.Error.msg.ToString());
                        AreEqual(new SkipInfo { arrays=1, booleans= 1, floats= 0, integers= 3, nulls= 1, objects= 1, strings= 1 }, enc.parser.skipInfo);
                        AreEqual(expect, value);
                    }

                    TestClass root = new TestClass();
                    TestClass curTestClass = root;
                    for (int i = 0; i < 30; i++) {
                        curTestClass.selfReference = new TestClass();
                        curTestClass = curTestClass.selfReference;
                    }
                    write.Write(root);
                    enc.Read<TestClass>(write.Output);
                    AreEqual(JsonEvent.EOF, enc.parser.Event);
                    
                    enc.Read<TestClass>(mapNull);
                    StringAssert.Contains("Cannot assign null to class field: key. Expect:", enc.Error.msg.ToString());
                    
                    enc.Read<TestClass>(mapStr);
                    StringAssert.Contains("Cannot assign string to class field: key. Expect:", enc.Error.msg.ToString());
                    
                     enc.Read<TestClass>(mapNum2);
                    StringAssert.Contains("Cannot assign number to class field: str. Expect:", enc.Error.msg.ToString());

                    enc.Read<TestClass>(mapBool);
                    StringAssert.Contains("Cannot assign bool to class field: key. Expect:", enc.Error.msg.ToString());
                    

                    // ------------------------------------- Array -------------------------------------
                    AreEqual(null,                enc.Read<string[]>    (@null));           AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(null,                enc.Read<double[]>    (@null));           AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(null,                enc.Read<float[]>     (@null));           AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(null,                enc.Read<long[]>      (@null));           AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(null,                enc.Read<int[]>       (@null));           AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(null,                enc.Read<short[]>     (@null));           AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(null,                enc.Read<byte[]>      (@null));           AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(null,                enc.Read<bool[]>      (@null));           AreEqual(JsonEvent.EOF, enc.parser.Event);
                    
                    enc.Read<TestStruct[]>(arrNull);
                    StringAssert.Contains("Cannot assign null to array element. Expect:", enc.Error.msg.ToString());
                    enc.Read<int[]>(arrNull);
                    StringAssert.Contains("Cannot assign null to array element. Expect:", enc.Error.msg.ToString());
                    enc.Read<int[]>(arrStr);
                    StringAssert.Contains("Cannot assign string to array element. Expect:", enc.Error.msg.ToString());

                    AreEqual(new [] {"hello"},    enc.Read<string[]>    (arrStr));          AreEqual(JsonEvent.EOF, enc.parser.Event);
                    {
                        var inOut = new string[1];
                        IsTrue(enc.ReadTo(arrStr, inOut));
                        AreEqual(JsonEvent.EOF, enc.parser.Event);
                        AreEqual(new[] {"hello"}, inOut);
                    }
                    // --- array non nullable
                    AreEqual(new [] {1,2,3},      enc.Read   <long[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(new [] {1,2,3},      enc.Read    <int[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(new [] {1,2,3},      enc.Read  <short[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(new [] {1,2,3},      enc.Read   <byte[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.parser.Event);
                            
                    AreEqual(new [] {1,2,3},      enc.Read <double[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(new [] {1,2,3},      enc.Read  <float[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.parser.Event);
                            
                    AreEqual(new [] {true, false},enc.Read   <bool[]>    (arrBln));          AreEqual(JsonEvent.EOF, enc.parser.Event);
                    // --- array nullable
                    AreEqual(new [] {1,2,3},      enc.Read  <long?[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(new [] {1,2,3},      enc.Read   <int?[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(new [] {1,2,3},      enc.Read <short?[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(new [] {1,2,3},      enc.Read  <byte?[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.parser.Event);
                            
                    AreEqual(new [] {1,2,3},      enc.Read<double?[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(new [] {1,2,3},      enc.Read <float?[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.parser.Event);
                            
                    AreEqual(new [] {true, false},enc.Read <bool?[]>     (arrBln));          AreEqual(JsonEvent.EOF, enc.parser.Event);
                    // array nullable - with null
                    AreEqual(new   long?[] {null},  enc.Read   <long?[]>   (arrNull));         AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(new    int?[] {null},  enc.Read    <int?[]>   (arrNull));         AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(new  short?[] {null},  enc.Read  <short?[]>   (arrNull));         AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(new   byte?[] {null},  enc.Read   <byte?[]>   (arrNull));         AreEqual(JsonEvent.EOF, enc.parser.Event);

                    AreEqual(new double?[] {null},  enc.Read <double?[]>   (arrNull));         AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(new  float?[] {null},  enc.Read   <byte?[]>   (arrNull));         AreEqual(JsonEvent.EOF, enc.parser.Event);

                    AreEqual(new   bool?[] {null},  enc.Read   <bool?[]>   (arrNull));         AreEqual(JsonEvent.EOF, enc.parser.Event);

                    {
                        Dictionary<string, int>[] expect = {new Dictionary<string, int> {{"key", 42}}};
                        AreEqual(expect, enc.Read<Dictionary<string, int>[]>(arrObj));
                        AreEqual(JsonEvent.EOF, enc.parser.Event);
                    }

                    // --- array of array
                    {
                        int[][] expect = {new []{1, 2, 3}};
                        AreEqual(expect, enc.Read<int[][]>(arrArrNum));
                        AreEqual(JsonEvent.EOF, enc.parser.Event);
                    }
                    {
                        Dictionary<string, int>[][] expect = {new []{ new Dictionary<string, int> {{"key", 42}}}};
                        AreEqual(expect, enc.Read<Dictionary<string, int>[][]>(arrArrObj));
                        AreEqual(JsonEvent.EOF, enc.parser.Event);
                    }
                    // --- multi dimensional arrays
                    {
                        // int[,] expect = {{1, 2, 3}};
                        var e = Assert.Throws<NotSupportedException>(() => enc.Read<int[,]>(arrArrNum));
                        AreEqual("Type not supported. Type: System.Int32[,]", e.Message);              
                    }
                    
                    // ------------------------------------- List<T> -------------------------------------
                    AreEqual(null, enc.Read<List<int>>    (@null));           AreEqual(JsonEvent.EOF, enc.parser.Event);
                    {
                        List<int> expect = new List<int> {1, 2, 3};
                        AreEqual(expect, enc.Read<List<int>> (arrNum));
                        AreEqual(expect, enc.Read<List<int?>>(arrNum));
                        AreEqual(JsonEvent.EOF, enc.parser.Event);
                    } {
                        List<bool> expect = new List<bool> {true, false};
                        AreEqual(expect, enc.Read<List<bool>>(arrBln));
                        AreEqual(JsonEvent.EOF, enc.parser.Event);
                    }
                    // --- list of list
                    {
                        List<List<int>> expect = new List<List<int>> {new List<int> {1, 2, 3}};
                        AreEqual(expect, enc.Read<List<List<int>>>(arrArrNum));
                    }
                    
                    enc.Read<List<int>>(arrNull);
                    StringAssert.Contains("Cannot assign null to List element. Expect:", enc.Error.msg.ToString());
                    
                    enc.Read<List<int>>(arrStr);
                    StringAssert.Contains("Cannot assign string to List element. Expect:", enc.Error.msg.ToString());
                    
                    enc.Read<List<string>>(arrNum);
                    StringAssert.Contains("Cannot assign number to List element. Expect:", enc.Error.msg.ToString());

                    enc.Read<List<string>>(arrBln);
                    StringAssert.Contains("Cannot assign bool to List element. Expect:", enc.Error.msg.ToString());
                    
                    enc.Read<List<string>>(mapStr);
                    StringAssert.Contains("Cannot assign object to array. Expect:", enc.Error.msg.ToString());

                    // --------------------------------- Dictionary<K,V> ---------------------------------
                    enc.Read<Dictionary<string, long>>(arrNull);
                    StringAssert.Contains("Cannot assign array to object. Expect:", enc.Error.msg.ToString());
                        
                    enc.Read<Dictionary<string, long>>(arrStr);
                    StringAssert.Contains("Cannot assign array to object. Expect:", enc.Error.msg.ToString());
                        
                    enc.Read<Dictionary<string, string>>(arrNum);
                    StringAssert.Contains("Cannot assign array to object. Expect:", enc.Error.msg.ToString());
                        
                    enc.Read<Dictionary<string, string>>(arrBln);
                    StringAssert.Contains("Cannot assign array to object. Expect:", enc.Error.msg.ToString());
                    {
                        var e = Assert.Throws<NotSupportedException>(() => enc.Read<Dictionary<int, string>>(mapStr));
                        AreEqual("Dictionary only support string as key type. Type: System.Collections.Generic.Dictionary`2[System.Int32,System.String]", e.Message);              
                    }
                    
                    // --- maps - value type: integral 
                    {
                        var expect = new Dictionary<string, long> {{"key", 42}};
                        AreEqual(expect, enc.Read<Dictionary<string, long>>(mapNum));
                    } {
                        var expect = new Dictionary<string, int> {{"key", 42}};
                        AreEqual(expect, enc.Read<Dictionary<string, int>>(mapNum));
                    } {
                        var expect = new Dictionary<string, short> {{"key", 42}};
                        AreEqual(expect, enc.Read<Dictionary<string, short>>(mapNum));
                    } {
                        var expect = new Dictionary<string, byte> {{"key", 42}};
                        AreEqual(expect, enc.Read<Dictionary<string, byte>>(mapNum));
                    }
                    // --- maps - value type: floating point
                    {
                        var expect = new Dictionary<string, double> {{"key", 42}};
                        AreEqual(expect, enc.Read<Dictionary<string, double>>(mapNum));
                    } {
                        var expect = new Dictionary<string, float> {{"key", 42}};
                        AreEqual(expect, enc.Read<Dictionary<string, float>>(mapNum));
                    }
                    
                    // --- map - value type: string
                    {
                        var expect = new Dictionary<string, string> {{"key", "value" }};
                        AreEqual(expect, enc.Read<Dictionary<string, string>>(mapStr));
                    }
                    // --- map - value type: bool
                    {
                        var expect = new Dictionary<string, bool> {{"key", true }};
                        AreEqual(expect, enc.Read<Dictionary<string, bool>>(mapBool));
                    }
                    // --- map - value type: map
                    {
                        var expect = new Dictionary<string, Dictionary<string, int>>() {{"key", new Dictionary<string, int> {{"key", 42 }} }};
                        AreEqual(expect, enc.Read<Dictionary<string, Dictionary<string, int>>>(mapMapNum));
                    }
                    // --- map derivations                
                    {
                        var expect = new Dictionary<string, long> {{"key", 42}};
                        AreEqual(expect, enc.Read<ConcurrentDictionary<string, long>>(mapNum));
                        // AreEqual(expect, enc.Read<ReadOnlyDictionary<string, long>>(mapNum));
                    }
                    
                    // ---- BigInteger ---
                    AreEqual(new TestStruct{ key = 42 },      enc.ReadValue<TestStruct>    (mapNum));
                    AreEqual(default(TestStruct), enc.ReadValue<TestStruct>(@null));
                    StringAssert.Contains("Cannot assign null to object. Expect:", enc.Error.msg.ToString());
                    {
                        BigInteger expect = BigInteger.Parse(bigInt.ToString());
                        AreEqual(expect, enc.ReadValue<BigInteger>(bigIntStr));
                        AreEqual(JsonEvent.EOF, enc.parser.Event);
                        
                        AreEqual(expect, enc.ReadValue<BigInteger>(bigIntStrN));
                        AreEqual(JsonEvent.EOF, enc.parser.Event);
                        
                        AreEqual(expect, enc.ReadValue<BigInteger>(bigInt));
                        AreEqual(JsonEvent.EOF, enc.parser.Event);
                        
                        write.Write(expect);
                        AreEqual(bigIntStr.ToString(), write.Output.ToString());
                    }
                    enc.ReadValue<BigInteger>(hello);
                    StringAssert.Contains("Failed parsing BigInt. value:", enc.Error.msg.ToString());
                    
                    // --- DateTime ---
                    {
                        DateTime expect = DateTime.Parse(dateTime.ToString());
                        DateTime value = enc.ReadValue<DateTime>(dateTimeStr);
                        AreEqual(expect, value);
                        
                        write.Write(expect);
                        AreEqual(dateTimeStr.ToString(), write.Output.ToString());   
                    }
                    enc.ReadValue<DateTime>(hello);
                    StringAssert.Contains("Failed parsing DateTime. value:", enc.Error.msg.ToString());

                    // Ensure minimum required type lookups
                    if (n > 0) {
#if !UNITY_EDITOR
                        AreEqual(120, enc.typeCache.LookupCount);
#endif
                        AreEqual( 0, enc.typeCache.StoreLookupCount);
                        AreEqual( 0, enc.typeCache.TypeCreationCount);
                    }
                    enc.typeCache.ClearCounts();
                }
            }

        }
    }
}

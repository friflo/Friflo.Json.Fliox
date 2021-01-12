using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Managed;
using Friflo.Json.Managed.Utils;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest
{
    public class TestReaderWriter : LeakTestsFixture
    {
        private TypeStore createStore() {
            TypeStore      typeStore = new TypeStore();
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
        
        private Object EncodeJsonTo(Bytes json, Object obj, TypeStore typeStore) {
            Object ret = null;
            using (JsonReader enc = new JsonReader(typeStore)) {
                // StopWatch stopwatch = new StopWatch();
                for (int n = 0; n < num2; n++) {
                    ret = enc.ReadTo(json, obj);
                    if (ret == null)
                        throw new FrifloException(enc.Error.msg.ToString());
                }
                AreEqual(0, enc.SkipInfo.Sum);
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
                obj = (JsonComplex) EncodeJsonTo(bytes, obj, typeStore);
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
        public void ReadPrimitive()
        {
            using (TypeStore typeStore = createStore())
            using (JsonReader enc = new JsonReader(typeStore))
            using (var hello =      new Bytes ("\"hello\""))
            using (var @double =    new Bytes ("12.5"))
            using (var @long =      new Bytes ("42"))
            using (var @true =      new Bytes ("true"))
            using (var @null =      new Bytes ("null"))
            using (var arrNum =     new Bytes ("[1,2,3]"))
            using (var arrArrNum =  new Bytes ("[[1,2,3]]"))
            using (var arrArrObj =  new Bytes ("[[{\"key\":42}]]"))
            using (var mapNum =     new Bytes ("{\"key\":42}"))
            using (var mapBool =    new Bytes ("{\"key\":true}"))
            using (var mapStr =     new Bytes ("{\"key\":\"value\"}"))
            using (var mapMapNum =  new Bytes ("{\"key\":{\"key\":42}}"))
            using (var invalid =    new Bytes ("invalid"))
            {
                AreEqual("hello",   enc.Read<string>(hello));
                AreEqual(12.5,      enc.Read<double>(@double));
                AreEqual(12.5,      enc.Read<float>(@double));
                
                AreEqual(42,        enc.Read<long>(@long));
                AreEqual(42,        enc.Read<int>(@long));
                AreEqual(42,        enc.Read<short>(@long));
                AreEqual(42,        enc.Read<byte>(@long));
                
                AreEqual(true,      enc.Read<bool>(@true));
                
                AreEqual(null,      enc.Read<object>(@null));
                AreEqual(false,     enc.Error.ErrSet);
                
                AreEqual(null,      enc.Read<object>(invalid));
                AreEqual(true,      enc.Error.ErrSet);
                AreEqual("JsonParser/JSON error: unexpected character while reading value. Found: i path: '(root)' at position: 1",     enc.Error.msg.ToString());

                AreEqual(new [] {1,2,3},      enc.Read<long[]>(arrNum));
                AreEqual(new [] {1,2,3},      enc.Read<int[]>(arrNum));
                AreEqual(new [] {1,2,3},      enc.Read<short[]>(arrNum));
                AreEqual(new [] {1,2,3},      enc.Read<byte[]>(arrNum));
                
                AreEqual(new [] {1,2,3},      enc.Read<double[]>(arrNum));
                AreEqual(new [] {1,2,3},      enc.Read<float[]>(arrNum));

                // --- array of array
                {
                    int[][] expect = {new []{1, 2, 3}};
                    AreEqual(expect, enc.Read<int[][]>(arrArrNum));
                }
                {
                    Dictionary<string, int>[][] expect = {new []{ new Dictionary<string, int> {{"key", 42}}}};
                    AreEqual(expect, enc.Read<Dictionary<string, int>[][]>(arrArrObj));
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

            }
        }
    }
}

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Mapper;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;

using static NUnit.Framework.Assert;
// using static Friflo.Json.Tests.Common.UnitTest.NoCheck;

namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    
    public class TestJsonComplex : LeakTestsFixture
    {
        private TypeStore createStore() {
            TypeStore      typeStore = new TypeStore(new StoreConfig());
            return typeStore;
        }

        String JsonSimpleObj = "{\"val\":5}";
        
        [Test]
        public void EncodeJsonSimple()  {
            using (TypeStore typeStore = createStore())
            using (Bytes bytes = new Bytes(JsonSimpleObj))
            {
                JsonSimple obj = EncodeJson<JsonSimple>(bytes, typeStore);
                AreEqual(5L, obj.val);
            }
        }
        
        int                 num2 =              2;
        
        private T EncodeJson<T>(Bytes json, TypeStore typeStore) {
            T ret = default;
            using (var enc = new JsonReader(typeStore, JsonReader.NoThrow)) {
                // StopWatch stopwatch = new StopWatch();
                for (int n = 0; n < num2; n++) {
                    ret = enc.Read<T>(json);
                    if (ret == null)
                        throw new InvalidOperationException(enc.Error.msg.ToString());
                }
                AreEqual(0, enc.SkipInfo.Sum);
                // FFLog.log("EncodeJson: " + json + " : " + stopwatch.Time());
            }
            return ret;
        }
        
        private bool EncodeJsonTo<T>(Bytes json, T obj, TypeStore typeStore) {
            using (JsonReader enc = new JsonReader(typeStore, JsonReader.NoThrow)) {
                // StopWatch stopwatch = new StopWatch();
                for (int n = 0; n < num2; n++) {
                    enc.ReadTo(json, obj);
                    if (!enc.Success)
                        throw new InvalidOperationException(enc.Error.msg.ToString());
                }
                AreEqual(0, enc.SkipInfo.Sum); // 2 => discriminator: "$type" is skipped, there is simply no field for a discriminator
                // FFLog.log("EncodeJsonTo: " + json + " : " + stopwatch.Time());
                return enc.Success;
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
                JsonComplex obj = EncodeJson<JsonComplex>(bytes, typeStore);
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
        public void Pretty()   {
            using (var typeStore = createStore())
            using (var reader   = new JsonReader(typeStore))
            using (var writer   = new JsonWriter(typeStore))
            using (var json     = new TestBytes())
            using (Bytes bytes = CommonUtils.FromFile("assets/codec/complex.json")) {
                var complex = reader.Read<JsonComplex>(bytes);
                writer.Pretty = true;
                writer.Write(complex, ref json.bytes);
                CommonUtils.ToFile("assets/output/complexPrettyReflect.json", json.bytes);
            }
        }
        
        [Test]
        public void WriteJsonComplex()
        {
            using (TypeStore    typeStore   = createStore())
            using (var          dst         = new TestBytes())
            {
                JsonComplex obj = new JsonComplex();
                SetComplex(obj);
                using (JsonWriter writer = new JsonWriter(typeStore)) {
                    writer.Write(obj, ref dst.bytes);

                    using (JsonReader enc = new JsonReader(typeStore, JsonReader.NoThrow)) {
                        JsonComplex res = enc.Read<JsonComplex>(dst.bytes);
                        if (res == null)
                            Fail(enc.Error.msg.ToString());
                        CheckJsonComplex(res);
                    }
                }
            }
        }

    }
}

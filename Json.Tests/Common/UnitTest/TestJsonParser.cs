using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Managed;
using Friflo.Json.Managed.Prop;
using Friflo.Json.Managed.Utils;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest
{

    public struct TestParserImpl
    {
        public static void BasicJsonParser() {
            JsonParser parser = new JsonParser();
            
            using (var bytes = CommonUtils.FromString("{}")) {
                parser.InitParser(bytes);
                AreEqual(JsonEvent.ObjectStart, parser.NextEvent());
                AreEqual(JsonEvent.ObjectEnd, parser.NextEvent());
                AreEqual(0, parser.Level);
                AreEqual(JsonEvent.EOF, parser.NextEvent());
                AreEqual(JsonEvent.Error, parser.NextEvent());
                AreEqual("JsonParser error - Parsing already finished path: '(root)' at position: 2", parser.error.msg.ToString());
            }
            using (var bytes = CommonUtils.FromString("{'test':'hello'}")) {
                parser.InitParser(bytes);
                AreEqual(JsonEvent.ObjectStart, parser.NextEvent());
                AreEqual(JsonEvent.ValueString, parser.NextEvent());
                AreEqual("test", parser.key.ToString());
                AreEqual("hello", parser.value.ToString());
                AreEqual(JsonEvent.ObjectEnd, parser.NextEvent());
                AreEqual(0, parser.Level);
                AreEqual(JsonEvent.EOF, parser.NextEvent());
                AreEqual(JsonEvent.Error, parser.NextEvent());
            }
            using (var bytes = CommonUtils.FromString("{'a':'b','abc':123,'x':'ab\\r\\nc'}")) {
                parser.InitParser(bytes);
                AreEqual(JsonEvent.ObjectStart, parser.NextEvent());
                AreEqual(JsonEvent.ValueString, parser.NextEvent());
                AreEqual(JsonEvent.ValueNumber, parser.NextEvent());
                AreEqual("abc", parser.key.ToString());
                AreEqual("123", parser.value.ToString());
                AreEqual(JsonEvent.ValueString, parser.NextEvent());
                AreEqual("ab\r\nc", parser.value.ToString());
                AreEqual(JsonEvent.ObjectEnd, parser.NextEvent());
                AreEqual(0, parser.Level);
                AreEqual(JsonEvent.EOF, parser.NextEvent());
                AreEqual(JsonEvent.Error, parser.NextEvent());
            }
            using (var bytes = CommonUtils.FromString("[]")) {
                parser.InitParser(bytes);
                AreEqual(JsonEvent.ArrayStart, parser.NextEvent());
                AreEqual(JsonEvent.ArrayEnd, parser.NextEvent());
                AreEqual(0, parser.Level);
                AreEqual(JsonEvent.EOF, parser.NextEvent());
                AreEqual(JsonEvent.Error, parser.NextEvent());
            }
            
            // --------------- primitives on root level --------------- 
            using (var bytes = CommonUtils.FromString("'str'")) {
                parser.InitParser(bytes);
                AreEqual(JsonEvent.ValueString, parser.NextEvent());
                AreEqual("str", parser.value.ToString());
                AreEqual(JsonEvent.EOF, parser.NextEvent());
                AreEqual(JsonEvent.Error, parser.NextEvent());
            }
            using (var bytes = CommonUtils.FromString("42")) {
                parser.InitParser(bytes);
                AreEqual(JsonEvent.ValueNumber, parser.NextEvent());
                AreEqual("42", parser.value.ToString());
                AreEqual(JsonEvent.EOF, parser.NextEvent());
                AreEqual(JsonEvent.Error, parser.NextEvent());
            }
            using (var bytes = CommonUtils.FromString("true")) {
                parser.InitParser(bytes);
                AreEqual(JsonEvent.ValueBool, parser.NextEvent());
                AreEqual(true, parser.boolValue);
                AreEqual(JsonEvent.EOF, parser.NextEvent());
                AreEqual(JsonEvent.Error, parser.NextEvent());
            }
            using (var bytes = CommonUtils.FromString("null")) {
                parser.InitParser(bytes);
                AreEqual(JsonEvent.ValueNull, parser.NextEvent());
                AreEqual(JsonEvent.EOF, parser.NextEvent());
                AreEqual(JsonEvent.Error, parser.NextEvent());
            }
            
            // --------------- invalid strings on root ---------------
            using (var bytes = CommonUtils.FromString("")) { // empty string is not valid JSON
                parser.InitParser(bytes);
                AreEqual(JsonEvent.Error, parser.NextEvent());
                AreEqual("JsonParser error - unexpected EOF on root path: '(root)' at position: 0", parser.error.msg.ToString());
            }
            using (var bytes = CommonUtils.FromString("str")) {
                parser.InitParser(bytes);
                AreEqual(false, parser.error.ErrSet);       // ensure error is cleared
                AreEqual("", parser.error.msg.ToString());  // ensure error message is cleared
                AreEqual(JsonEvent.Error, parser.NextEvent());
                AreEqual("JsonParser error - unexpected character while reading value. Found: s path: '(root)' at position: 1", parser.error.msg.ToString());
            }
            using (var bytes = CommonUtils.FromString("tx")) { // start as a bool (true)
                parser.InitParser(bytes);
                AreEqual(JsonEvent.Error, parser.NextEvent());
                AreEqual("JsonParser error - invalid value: tx path: '(root)' at position: 2", parser.error.msg.ToString());
            }
            using (var bytes = CommonUtils.FromString("1a")) { // start as a number
                parser.InitParser(bytes);
                AreEqual(JsonEvent.Error, parser.NextEvent());
                AreEqual("JsonParser error - unexpected character while reading number. Found : a path: '(root)' at position: 1", parser.error.msg.ToString());
                AreEqual(JsonEvent.Error, parser.NextEvent());
            }
            parser.Dispose();
        }


        public static void TestParseFile(Bytes bytes) {
            JsonParser parser = new JsonParser();
            try {
                parser.InitParser (bytes);                                  CheckPath(ref parser, "(root)");
                AreEqual(JsonEvent.ObjectStart, parser.NextEvent());        CheckPath(ref parser, "(root)");
                AreEqual(JsonEvent.ValueString, parser.NextEvent());        CheckPath(ref parser, "eur");
                AreEqual(">€<",                 parser.value.ToString());
                AreEqual(JsonEvent.ValueString, parser.NextEvent());        CheckPath(ref parser, "eur2");
                AreEqual("[€]",                 parser.value.ToString());   
                
                AreEqual(JsonEvent.ValueNull,   parser.NextEvent());        CheckPath(ref parser, "null");
                AreEqual(JsonEvent.ValueBool,   parser.NextEvent());        CheckPath(ref parser, "true");
                AreEqual(true,                  parser.boolValue);
                AreEqual(JsonEvent.ValueBool,   parser.NextEvent());        CheckPath(ref parser, "false");
                AreEqual(false,                 parser.boolValue);
                
                AreEqual(JsonEvent.ObjectStart, parser.NextEvent());        CheckPath(ref parser, "empty");
                AreEqual("empty",               parser.key.ToString());
                AreEqual(JsonEvent.ObjectEnd,   parser.NextEvent());        CheckPath(ref parser, "empty");
                
                AreEqual(JsonEvent.ObjectStart, parser.NextEvent());        CheckPath(ref parser, "obj");
                AreEqual(JsonEvent.ValueNumber, parser.NextEvent());        CheckPath(ref parser, "obj.val");
            //  AreEqual(11,                    parser.number.ParseInt(parseCx));
                AreEqual(JsonEvent.ObjectEnd,   parser.NextEvent());        CheckPath(ref parser, "obj");
                
                AreEqual(JsonEvent.ArrayStart,  parser.NextEvent());        CheckPath(ref parser, "arr0[]");
                AreEqual("arr0",                parser.key.ToString());
                AreEqual(JsonEvent.ArrayEnd,    parser.NextEvent());        CheckPath(ref parser, "arr0");
                
                AreEqual(JsonEvent.ArrayStart,  parser.NextEvent());        CheckPath(ref parser, "arr1[]");
                AreEqual("arr1",                parser.key.ToString());
                AreEqual(JsonEvent.ValueNumber, parser.NextEvent());        CheckPath(ref parser, "arr1[0]");
                AreEqual(JsonEvent.ArrayEnd,    parser.NextEvent());        CheckPath(ref parser, "arr1");
                
                AreEqual(JsonEvent.ArrayStart,  parser.NextEvent());        CheckPath(ref parser, "arr2[]");
                AreEqual("arr2",                parser.key.ToString());
                AreEqual(JsonEvent.ValueNumber, parser.NextEvent());        CheckPath(ref parser, "arr2[0]");
                AreEqual(JsonEvent.ValueNumber, parser.NextEvent());        CheckPath(ref parser, "arr2[1]");
                AreEqual(JsonEvent.ArrayEnd,    parser.NextEvent());        CheckPath(ref parser, "arr2");
                
                AreEqual(JsonEvent.ArrayStart,  parser.NextEvent());        CheckPath(ref parser, "arr3[]");
                AreEqual("arr3",                parser.key.ToString());
                AreEqual(JsonEvent.ObjectStart, parser.NextEvent());        CheckPath(ref parser, "arr3[0]");
                AreEqual(JsonEvent.ValueNumber, parser.NextEvent());        CheckPath(ref parser, "arr3[0].val");
                AreEqual(JsonEvent.ObjectEnd,   parser.NextEvent());        CheckPath(ref parser, "arr3[0]");       
                AreEqual(JsonEvent.ObjectStart, parser.NextEvent());        CheckPath(ref parser, "arr3[1]");
                AreEqual(JsonEvent.ValueNumber, parser.NextEvent());        CheckPath(ref parser, "arr3[1].val");
                AreEqual(JsonEvent.ObjectEnd,   parser.NextEvent());        CheckPath(ref parser, "arr3[1]");
                AreEqual(JsonEvent.ArrayEnd,    parser.NextEvent());        CheckPath(ref parser, "arr3");
                
                AreEqual(JsonEvent.ValueString, parser.NextEvent());        CheckPath(ref parser, "str");
                AreEqual(JsonEvent.ValueNumber, parser.NextEvent());        CheckPath(ref parser, "int32");
                AreEqual(JsonEvent.ValueNumber, parser.NextEvent());        CheckPath(ref parser, "dbl");
                
                AreEqual(JsonEvent.ObjectEnd,   parser.NextEvent());        CheckPath(ref parser, "(root)");
                AreEqual(JsonEvent.EOF,         parser.NextEvent());        CheckPath(ref parser, "(root)");
                AreEqual(JsonEvent.Error,       parser.NextEvent());        CheckPath(ref parser, "(root)");
            
                parser.InitParser(bytes);
                for (int n = 0; n < 32; n++)
                    parser.NextEvent();
                AreEqual(JsonEvent.EOF, parser.NextEvent());
            } finally {
                parser.Dispose();
            }
        }
        
        public static void CheckPath (ref JsonParser parser, String path)
        {
            AreEqual(path,      parser.GetPath());
        }

        

    }

    public class TestJsonParser : LeakTestsFixture
    {
        private PropType.Store createStore()
        {
            PropType.Store      store = new PropType.Store();
            store.RegisterType("Sub", typeof( Sub ));
            return store;
        }
        
        [Test]
        public void TestParser() {
            TestParserImpl.BasicJsonParser();
        }
        
        [Test]
        public void TestParserPath() {
            JsonParser parser = new JsonParser();
            try {
                using (var bytes = CommonUtils.FromString("{ err")) {
                    parser.InitParser(bytes);
                    parser.SkipTree();
                    AreEqual("(root)", parser.GetPath());
                }
                using (var bytes = CommonUtils.FromString("{'m' err")) {
                    parser.InitParser(bytes);
                    parser.SkipTree();
                    AreEqual("m", parser.GetPath());
                }
                using (var bytes = CommonUtils.FromString("[err")) {
                    parser.InitParser(bytes);
                    parser.SkipTree();
                    AreEqual("[0]", parser.GetPath());
                }
                using (var bytes = CommonUtils.FromString("[1, err")) {
                    parser.InitParser(bytes);
                    parser.SkipTree();
                    AreEqual("[1]", parser.GetPath());
                }
                using (var bytes = CommonUtils.FromString("err")) {
                    parser.InitParser(bytes);
                    parser.SkipTree();
                    AreEqual("(root)", parser.GetPath());
                }
            }
            finally {
                parser.Dispose();
            }
        }
        
        [Test]
        public void TestSkipping() {
            JsonParser parser = new JsonParser();
            try {
                using (var bytes = CommonUtils.FromString("{}")) {
                    parser.InitParser(bytes);
                    IsTrue(parser.SkipTree());
                    AreEqual(1, parser.skipInfo.objects);
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                }
                using (var bytes = CommonUtils.FromString("{'a':'A'}")) {
                    parser.InitParser(bytes);
                    AreEqual(JsonEvent.ObjectStart, parser.NextEvent());
                    IsTrue(parser.SkipTree());
                    AreEqual(1, parser.skipInfo.objects);
                    AreEqual(1, parser.skipInfo.strings);
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                }
                using (var bytes = CommonUtils.FromString("{'a':'A','b':'B'}")) {
                    parser.InitParser(bytes);
                    AreEqual(JsonEvent.ObjectStart, parser.NextEvent());
                    AreEqual(JsonEvent.ValueString, parser.NextEvent()); // consume first property
                    IsTrue(parser.SkipTree());
                    AreEqual(1, parser.skipInfo.objects);
                    AreEqual(1, parser.skipInfo.strings);
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                }
                using (var bytes = CommonUtils.FromString("[]")) {
                    parser.InitParser(bytes);
                    IsTrue(parser.SkipTree());
                    AreEqual(1, parser.skipInfo.arrays);
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                }
                using (var bytes = CommonUtils.FromString("'str'")) {
                    parser.InitParser(bytes);
                    IsTrue(parser.SkipTree());
                    AreEqual(1, parser.skipInfo.strings);
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                }
                using (var bytes = CommonUtils.FromString("42")) {
                    parser.InitParser(bytes);
                    IsTrue(parser.SkipTree());
                    AreEqual(1, parser.skipInfo.integers);
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                }
                using (var bytes = CommonUtils.FromString("true")) {
                    parser.InitParser(bytes);
                    IsTrue(parser.SkipTree());
                    AreEqual(1, parser.skipInfo.booleans);
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                }
                using (var bytes = CommonUtils.FromString("null")) {
                    parser.InitParser(bytes);
                    IsTrue(parser.SkipTree());
                    AreEqual(1, parser.skipInfo.nulls);
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                }

                // --------------- skipping skipping invalid cases
                using (var bytes = CommonUtils.FromString("[")) {
                    parser.InitParser(bytes);
                    IsFalse(parser.SkipTree());
                    AreEqual(JsonEvent.Error, parser.NextEvent());
                    IsFalse(parser.SkipTree()); // parser state is not changed
                }
                using (var bytes = CommonUtils.FromString("{")) {
                    parser.InitParser(bytes);
                    IsFalse(parser.SkipTree());
                    AreEqual(JsonEvent.Error, parser.NextEvent());
                }
                using (var bytes = CommonUtils.FromString("a")) {
                    parser.InitParser(bytes);
                    IsFalse(parser.SkipTree());
                    AreEqual(JsonEvent.Error, parser.NextEvent());
                }
                using (var bytes = CommonUtils.FromString("")) {
                    parser.InitParser(bytes);
                    IsFalse(parser.SkipTree());
                    AreEqual(JsonEvent.Error, parser.NextEvent());
                }
                using (var bytes = CommonUtils.FromString("42")) {
                    parser.InitParser(bytes);
                    AreEqual(JsonEvent.ValueNumber, parser.NextEvent());
                    IsFalse(parser.SkipTree()); // parser state is not changed
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                }
                using (var bytes = CommonUtils.FromString("{}")) {
                    parser.InitParser(bytes);
                    AreEqual(JsonEvent.ObjectStart, parser.NextEvent());
                    AreEqual(JsonEvent.ObjectEnd, parser.NextEvent());
                    IsFalse(parser.SkipTree()); // parser state is not changed
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                }
            }
            finally {
                parser.Dispose();    
            }
        }

        [Test]
        public void TestParseFile() {
            using (Bytes bytes = CommonUtils.FromFile("assets/codec/parse.json")) {
                TestParserImpl.TestParseFile(bytes);
            }
        }
        
        String JsonSimpleObj = "{'val':5}";
        
        [Test]
        public void EncodeJsonSimple()  {
            using (PropType.Store store = createStore())
            using (Bytes bytes = CommonUtils.FromString(JsonSimpleObj))
            {
                JsonSimple obj = (JsonSimple) EncodeJson(bytes, typeof(JsonSimple), store);
                AreEqual(5L, obj.val);
            }
        }

        [Test]
        public void ParseJsonComplex()  {
            using (Bytes bytes = CommonUtils.FromFile("assets/codec/complex.json")) {
                ParseJson(bytes);
            }
        }

        int                 num2 =              2;
        
        private void ParseJson(Bytes json) {
            var memLog = new MemoryLogger(100, 1000, MemoryLog.Enabled);
            // 170 ms - 20000, Release | Any CPU, target framework: net5.0, complex.json: 1134 bytes => 133 MB/sec
            using (JsonParser parser = new JsonParser()) {
                // StopWatch stopwatch = new StopWatch();
                for (int n = 0; n < 20000; n++) {
                    memLog.Snapshot();
                    parser.InitParser(json);
                    parser.SkipTree();
                    if (parser.NextEvent() != JsonEvent.EOF)
                        Fail("Expected EOF");
                }
            }
            memLog.AssertNoAllocations();
            // TestContext.Out.WriteLine(memLog.MemorySnapshots());
            // FFLog.log("ParseJson: " + json + " : " + stopwatch.Time());
        }
        
        private Object EncodeJson(Bytes json, Type type, PropType.Store store)
        {
            Object ret = null;
            using (var enc = new JsonReader(store)) {
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
        
        private Object EncodeJsonTo(Bytes json, Object obj, PropType.Store store)
        {
            Object ret = null;
            using (JsonReader enc = new JsonReader(store)) {
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
        
        private static void CheckMap (IDictionary <String, JsonSimple> map)
        {
            AreEqual (2, map. Count);
            JsonSimple key1 = map [ "key1" ];
            AreEqual (         1L, key1.val);
            JsonSimple key2 = map [ "key2" ];
            AreEqual (       null, key2);
        }
        
        private static void CheckList (IList <Sub> list)
        {
            AreEqual (           4, list. Count);
            AreEqual (         11L, list [0] .i64);
            AreEqual (        null, list [1] );
            AreEqual (         13L, list [2] .i64);
            AreEqual (         14L, list [3] .i64);
        }

        private static void CheckJsonComplex (JsonComplex obj)
        {
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
            CheckMap (obj.map);
            CheckMap (obj.map2);
            CheckMap (obj.map3);
            CheckMap (obj.map4);
            CheckMap (obj.mapDerived);
            CheckMap (obj.mapDerivedNull);
            AreEqual (      "str1", obj.map5 [ "key1" ]);
            AreEqual (        null, obj.map5 [ "key2" ]);
        }
        
        private static void SetMap (IDictionary <String, JsonSimple> map)
        {
            // order not defined for HashMap
            map [ "key1" ]= new  JsonSimple(1L) ;
            map [ "key2" ]= null ;
        }
        
        private static void SetList (IList <Sub> list)
        {
            list. Add (new Sub(11L));
            list. Add (null);
            list. Add (new Sub(13L));
            list. Add (new Sub(14L));
        }
        
        private static void SetComplex (JsonComplex obj)
        {
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
            obj.map = new Dictionary <String, JsonSimple>();
            SetMap (obj.map);
            SetMap (obj.map2);
            obj.map3 = new Dictionary <String, JsonSimple>();
            SetMap (obj.map3);
            SetMap (obj.map4);
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
            using (PropType.Store store = createStore())
            using (Bytes bytes = CommonUtils.FromFile("assets/codec/complex.json")) {
                JsonComplex obj = (JsonComplex) EncodeJson(bytes, typeof(JsonComplex), store);
                CheckJsonComplex(obj);
            }
        }
        
        [Test]
        public void EncodeJsonToComplex()   {
            using (PropType.Store store = createStore())
            using (Bytes bytes = CommonUtils.FromFile("assets/codec/complex.json")) {
                JsonComplex obj = new JsonComplex();
                obj = (JsonComplex) EncodeJsonTo(bytes, obj, store);
                CheckJsonComplex(obj);
            }
        }
        
        [Test]
        public void WriteJsonComplex()
        {
            using (PropType.Store store = createStore()) {
                JsonComplex obj = new JsonComplex();
                SetComplex(obj);
                using (JsonWriter writer = new JsonWriter(store)) {
                    writer.Write(obj);

                    using (JsonReader enc = new JsonReader(store)) {
                        JsonComplex res = (JsonComplex) enc.Read(writer.Output, typeof(JsonComplex));
                        if (res == null)
                            Fail(enc.Error.msg.ToString());
                        CheckJsonComplex(res);
                    }
                }
            }
        }
    }
}
using System;
using Friflo.Json.Burst;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Burst
{

    public struct TestParserImpl
    {
        public static void BasicJsonParser() {
            using (var p = new Local<JsonParser>())
            {
                ref var parser = ref p.instance;
            
                using (var bytes = new Bytes("{}")) {
                    parser.InitParser(bytes);
                    AreEqual(JsonEvent.ObjectStart, parser.NextEvent());
                    AreEqual(JsonEvent.ObjectEnd, parser.NextEvent());
                    AreEqual(0, parser.Level);
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                    AreEqual(JsonEvent.Error, parser.NextEvent());
                    AreEqual("JsonParser/JSON error: Parsing already finished path: '(root)' at position: 2", parser.error.msg.ToString());
                }
                using (var bytes = new Bytes("{\"test\":\"hello\"}")) {
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
                using (var bytes = new Bytes("{\"a\":\"b\",\"abc\":123,\"x\":\"ab\\r\\nc\"}")) {
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
                using (var bytes = new Bytes("[]")) {
                    parser.InitParser(bytes);
                    AreEqual(JsonEvent.ArrayStart, parser.NextEvent());
                    AreEqual(JsonEvent.ArrayEnd, parser.NextEvent());
                    AreEqual(0, parser.Level);
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                    AreEqual(JsonEvent.Error, parser.NextEvent());
                }
                
                // --------------- primitives on root level --------------- 
                using (var bytes = new Bytes("\"str\"")) {
                    parser.InitParser(bytes);
                    AreEqual(JsonEvent.ValueString, parser.NextEvent());
                    AreEqual("str", parser.value.ToString());
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                    AreEqual(JsonEvent.Error, parser.NextEvent());
                }
                using (var bytes = new Bytes("42")) {
                    parser.InitParser(bytes);
                    AreEqual(JsonEvent.ValueNumber, parser.NextEvent());
                    AreEqual("42", parser.value.ToString());
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                    AreEqual(JsonEvent.Error, parser.NextEvent());
                }
                using (var bytes = new Bytes("true")) {
                    parser.InitParser(bytes);
                    AreEqual(JsonEvent.ValueBool, parser.NextEvent());
                    AreEqual(true, parser.boolValue);
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                    AreEqual(JsonEvent.Error, parser.NextEvent());
                }
                using (var bytes = new Bytes("null")) {
                    parser.InitParser(bytes);
                    AreEqual(JsonEvent.ValueNull, parser.NextEvent());
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                    AreEqual(JsonEvent.Error, parser.NextEvent());
                }
                
                // --------------- invalid strings on root ---------------
                using (var bytes = new Bytes("")) { // empty string is not valid JSON
                    parser.InitParser(bytes);
                    AreEqual(JsonEvent.Error, parser.NextEvent());
                    AreEqual("JsonParser/JSON error: unexpected EOF on root path: '(root)' at position: 0", parser.error.msg.ToString());
                }
                using (var bytes = new Bytes("str")) {
                    parser.InitParser(bytes);
                    AreEqual(false, parser.error.ErrSet);       // ensure error is cleared
                    AreEqual("", parser.error.msg.ToString());  // ensure error message is cleared
                    AreEqual(JsonEvent.Error, parser.NextEvent());
                    AreEqual("JsonParser/JSON error: unexpected character while reading value. Found: s path: '(root)' at position: 1", parser.error.msg.ToString());
                    AreEqual(1, parser.error.Pos);              // ensuring code coverage
                }
                using (var bytes = new Bytes("tx")) { // start as a bool (true)
                    parser.InitParser(bytes);
                    AreEqual(JsonEvent.Error, parser.NextEvent());
                    AreEqual("JsonParser/JSON error: invalid value: tx path: '(root)' at position: 2", parser.error.msg.ToString());
                    AreEqual(2, parser.error.Pos);
                }
                using (var bytes = new Bytes("1a")) { // start as a number
                    parser.InitParser(bytes);
                    AreEqual(JsonEvent.Error, parser.NextEvent());
                    AreEqual("JsonParser/JSON error: unexpected character while reading number. Found : a path: '(root)' at position: 1", parser.error.msg.ToString());
                    AreEqual(JsonEvent.Error, parser.NextEvent());
                }
            }
        }


        public static void TestNextEvent(Bytes bytes) {
            using (var p = new Local<JsonParser>())
            {
                ref var parser = ref p.instance;
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
            }
        }
        
        public static void CheckPath (ref JsonParser parser, String path) {
            AreEqual(path,      parser.GetPath());
        }
    }

    public class TestParser : LeakTestsFixture
    {
        [Test]
        public void BasicParser() {
            TestParserImpl.BasicJsonParser();
        }
        
        [Test]
        public void TestParserPath() {
            using (var p = new Local<JsonParser>())
            {
                ref var parser = ref p.instance;
                using (var bytes = new Bytes("{ err")) {
                    parser.InitParser(bytes);
                    parser.SkipTree();
                    AreEqual("(root)", parser.GetPath());
                }
                using (var bytes = new Bytes("{\"m\" err")) {
                    parser.InitParser(bytes);
                    parser.SkipTree();
                    AreEqual("m", parser.GetPath());
                }
                using (var bytes = new Bytes("[err")) {
                    parser.InitParser(bytes);
                    parser.SkipTree();
                    AreEqual("[0]", parser.GetPath());
                }
                using (var bytes = new Bytes("[1, err")) {
                    parser.InitParser(bytes);
                    parser.SkipTree();
                    AreEqual("[1]", parser.GetPath());
                }
                using (var bytes = new Bytes("err")) {
                    parser.InitParser(bytes);
                    parser.SkipTree();
                    AreEqual("(root)", parser.GetPath());
                }
            }
        }
        
        [Test]
        public void TestSkipping() {
            using (var p = new Local<JsonParser>())
            {
                ref var parser = ref p.instance;
                using (var bytes = new Bytes("{}")) {
                    parser.InitParser(bytes);
                    IsTrue(parser.SkipTree());
                    AreEqual(1, parser.skipInfo.objects);
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                }
                using (var bytes = new Bytes("{\"a\":\"A\"}")) {
                    parser.InitParser(bytes);
                    AreEqual(JsonEvent.ObjectStart, parser.NextEvent());
                    IsTrue(parser.SkipTree());
                    AreEqual(1, parser.skipInfo.objects);
                    AreEqual(1, parser.skipInfo.strings);
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                }
                using (var bytes = new Bytes("{\"a\":\"A\",\"b\":\"B\"}")) {
                    parser.InitParser(bytes);
                    AreEqual(JsonEvent.ObjectStart, parser.NextEvent());
                    AreEqual(JsonEvent.ValueString, parser.NextEvent()); // consume first property
                    IsTrue(parser.SkipTree());
                    AreEqual(1, parser.skipInfo.objects);
                    AreEqual(1, parser.skipInfo.strings);
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                }
                using (var bytes = new Bytes("[]")) {
                    parser.InitParser(bytes);
                    IsTrue(parser.SkipTree());
                    AreEqual(1, parser.skipInfo.arrays);
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                }
                using (var bytes = new Bytes("\"str\"")) {
                    parser.InitParser(bytes);
                    IsTrue(parser.SkipTree());
                    AreEqual(1, parser.skipInfo.strings);
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                }
                using (var bytes = new Bytes("42")) {
                    parser.InitParser(bytes);
                    IsTrue(parser.SkipTree());
                    AreEqual(1, parser.skipInfo.integers);
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                }
                using (var bytes = new Bytes("true")) {
                    parser.InitParser(bytes);
                    IsTrue(parser.SkipTree());
                    AreEqual(1, parser.skipInfo.booleans);
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                }
                using (var bytes = new Bytes("null")) {
                    parser.InitParser(bytes);
                    IsTrue(parser.SkipTree());
                    AreEqual(1, parser.skipInfo.nulls);
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                }

                // --------------- skipping skipping invalid cases
                using (var bytes = new Bytes("[")) {
                    parser.InitParser(bytes);
                    IsFalse(parser.SkipTree());
                    AreEqual(JsonEvent.Error, parser.NextEvent());
                    IsFalse(parser.SkipTree()); // parser state is not changed
                }
                using (var bytes = new Bytes("{")) {
                    parser.InitParser(bytes);
                    IsFalse(parser.SkipTree());
                    AreEqual(JsonEvent.Error, parser.NextEvent());
                }
                using (var bytes = new Bytes("a")) {
                    parser.InitParser(bytes);
                    IsFalse(parser.SkipTree());
                    AreEqual(JsonEvent.Error, parser.NextEvent());
                }
                using (var bytes = new Bytes("")) {
                    parser.InitParser(bytes);
                    IsFalse(parser.SkipTree());
                    AreEqual(JsonEvent.Error, parser.NextEvent());
                }
                using (var bytes = new Bytes("42")) {
                    parser.InitParser(bytes);
                    AreEqual(JsonEvent.ValueNumber, parser.NextEvent());
                    IsFalse(parser.SkipTree()); // parser state is not changed
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                }
                using (var bytes = new Bytes("{}")) {
                    parser.InitParser(bytes);
                    AreEqual(JsonEvent.ObjectStart, parser.NextEvent());
                    AreEqual(JsonEvent.ObjectEnd, parser.NextEvent());
                    IsFalse(parser.SkipTree()); // parser state is not changed
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                }
            }
        }

        [Test]
        public void TestNextEvent() {
            using (Bytes bytes = CommonUtils.FromFile("assets/codec/parse.json")) {
                TestParserImpl.TestNextEvent(bytes);
            }
        }
        
        [Test]
        public void ParseJsonComplex()  {
            using (Bytes bytes = CommonUtils.FromFile("assets/codec/complex.json")) {
                ParseJson(bytes);
            }
        }

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

#pragma warning disable 618 // Performance degradation by string copy
        [Test]
        public void TestAutoSkip() {
            using (var p = new Local<JsonParser>()) {
                ref var parser = ref p.instance;
                using (var json = new Bytes("{}")) {
                    parser.InitParser(json);
                    parser.NextEvent();
                    var obj = parser.GetObjectIterator ();
                    while (obj.NextObjectMember(ref parser, Skip.Auto)) {
                        Fail("Expect no members in empty object");
                    }
                    AreEqual(JsonEvent.ObjectEnd, parser.Event);
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                }
                using (var json = new Bytes("{\"arr\":[]}")) {
                    parser.InitParser(json);
                    parser.NextEvent();
                    int arrCount = 0;
                    var obj = parser.GetObjectIterator ();
                    while (obj.NextObjectMember(ref parser, Skip.Auto)) {
                        if (parser.UseMemberArr (ref obj, "arr")) {
                            arrCount++;
                            var arr = parser.GetArrayIterator();
                            while (arr.NextArrayElement(ref parser, Skip.Auto))
                                Fail("Expect no array elements");
                        }
                    }
                    AreEqual(JsonEvent.ObjectEnd, parser.Event);
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                    AreEqual(1, arrCount);
                }
                using (var json = new Bytes("[]")) {
                    parser.InitParser(json);
                    parser.NextEvent();
                    
                    var arr = parser.GetArrayIterator();
                    while (arr.NextArrayElement(ref parser, Skip.Auto))
                        Fail("Expect no elements in empty array");
                    AreEqual(JsonEvent.ArrayEnd, parser.Event);
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                }
                
                // -------------------- test application exceptions -----------------------
#if DEBUG
                using (var json = new Bytes("[]")) {
                    var e = Throws<InvalidOperationException>(() => {
                        p.instance.InitParser(json);
                        p.instance.NextEvent();
                                        
                        var obj = p.instance.GetObjectIterator ();
                        obj.NextObjectMember(ref p.instance, Skip.Auto);
                    });
                    AreEqual("Expect ObjectStart in GetObjectIterator()", e.Message);
                }
                using (var json = new Bytes("{}")) {
                    var e = Throws<InvalidOperationException>(() => {
                        p.instance.InitParser(json);
                        p.instance.NextEvent();
                        var arr = p.instance.GetArrayIterator();
                        arr.NextArrayElement(ref p.instance, Skip.Auto);
                    });
                    AreEqual("Expect ArrayStart in GetArrayIterator()", e.Message);
                }
                using (var json = new Bytes("{\"key\":42}")) {
                    var e = Throws<InvalidOperationException>(() => {
                        p.instance.InitParser(json);
                        p.instance.NextEvent();

                        var obj = p.instance.GetObjectIterator();
                        obj.NextObjectMember(ref p.instance, Skip.Auto);
                        AreEqual(JsonEvent.ValueNumber, p.instance.Event);

                        // call to NextObjectMember() would return false
                        AreEqual(JsonEvent.ObjectEnd, p.instance.NextEvent());
                        IsFalse(obj.NextObjectMember(ref p.instance, Skip.Auto));
                    });
                    AreEqual("Unexpected iterator level in NextObjectMember()", e.Message);
                }
                using (var json = new Bytes("[42]")) {
                    var e = Throws<InvalidOperationException>(() => {
                        p.instance.InitParser(json);
                        p.instance.NextEvent();

                        var arr = p.instance.GetArrayIterator();
                        arr.NextArrayElement(ref p.instance, Skip.Auto);
                        AreEqual(JsonEvent.ValueNumber, p.instance.Event);

                        // call to NextArrayElement() would return false
                        AreEqual(JsonEvent.ArrayEnd, p.instance.NextEvent());
                        IsFalse(arr.NextArrayElement(ref p.instance, Skip.Auto));
                    });
                    AreEqual("Unexpected iterator level in NextArrayElement()", e.Message);
                }
                using (var json = new Bytes("[{}]")) {
                    var e = Throws<InvalidOperationException>(() => {
                        p.instance.InitParser(json);
                        p.instance.NextEvent();

                        var arr = p.instance.GetArrayIterator();
                        arr.NextArrayElement(ref p.instance, Skip.Auto);
                        AreEqual(JsonEvent.ObjectStart, p.instance.Event);
                        AreEqual(true, p.instance.UseElementObj(ref arr)); // used object without skipping
                        IsFalse(arr.NextArrayElement(ref p.instance, Skip.Auto));
                    });
                    AreEqual("unexpected ObjectEnd in NextArrayElement()", e.Message);
                }
                using (var json = new Bytes("{\"arr\":[]}")) {
                    var e = Throws<InvalidOperationException>(() => {
                        p.instance.InitParser(json);
                        p.instance.NextEvent();

                        var obj = p.instance.GetObjectIterator();
                        obj.NextObjectMember(ref p.instance, Skip.Auto);
                        AreEqual(JsonEvent.ArrayStart, p.instance.Event);
                        AreEqual(true, p.instance.UseMemberArr (ref obj, "arr")); // used array without skipping
                        IsFalse(obj.NextObjectMember(ref p.instance, Skip.Auto));
                    });
                    AreEqual("unexpected ArrayEnd in NextObjectMember()", e.Message);
                }
#endif
            }
        }

        [Test]
        public void TestMaxDepth() {
            using (JsonParser parser = new JsonParser())
            using (var jsonDepth1 = new Bytes("[]"))
            using (var jsonDepth2 = new Bytes("[[]]"))
            {
                parser.InitParser(jsonDepth1);
                parser.SetMaxDepth (1);
                while (true) {
                    var ev = parser.NextEvent();
                    if (ev == JsonEvent.EOF) 
                        break; // expected
                    if (ev == JsonEvent.Error)
                        Fail(parser.error.msg.ToString());
                }
                parser.InitParser(jsonDepth2);
                parser.SetMaxDepth (1);
                while (true) {
                    var ev = parser.NextEvent();
                    if (ev == JsonEvent.Error) {
                        AreEqual("JsonParser/JSON error: nesting in JSON document exceed maxDepth: 1 path: '[0]' at position: 2", parser.error.msg.ToString());
                        break;
                    }
                }
            }
        }
    }
}
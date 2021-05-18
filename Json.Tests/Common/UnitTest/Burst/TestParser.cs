// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
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
            using (var parser = new Local<JsonParser>())
            {
                ref var p = ref parser.value;
            
                using (var bytes = new Bytes("{}")) {
                    p.InitParser(bytes);
                    AreEqual(JsonEvent.ObjectStart, p.NextEvent());
                    AreEqual(JsonEvent.ObjectEnd, p.NextEvent());
                    AreEqual(0, p.Level);
                    AreEqual(JsonEvent.EOF, p.NextEvent());
                    AreEqual(JsonEvent.Error, p.NextEvent());
                    AreEqual("JsonParser/JSON error: Parsing already finished path: '(root)' at position: 2", p.error.msg.ToString());
                }
                using (var bytes = new Bytes("{\"test\":\"hello\"}")) {
                    p.InitParser(bytes);
                    AreEqual(JsonEvent.ObjectStart, p.NextEvent());
                    AreEqual(JsonEvent.ValueString, p.NextEvent());
                    AreEqual("test", p.key.ToString());
                    AreEqual("hello", p.value.ToString());
                    AreEqual(JsonEvent.ObjectEnd, p.NextEvent());
                    AreEqual(0, p.Level);
                    AreEqual(JsonEvent.EOF, p.NextEvent());
                    AreEqual(JsonEvent.Error, p.NextEvent());
                }
                using (var bytes = new Bytes("{\"a\":\"b\",\"abc\":123,\"x\":\"ab\\r\\nc\"}")) {
                    p.InitParser(bytes);
                    AreEqual(JsonEvent.ObjectStart, p.NextEvent());
                    AreEqual(JsonEvent.ValueString, p.NextEvent());
                    AreEqual(JsonEvent.ValueNumber, p.NextEvent());
                    AreEqual("abc", p.key.ToString());
                    AreEqual("123", p.value.ToString());
                    AreEqual(JsonEvent.ValueString, p.NextEvent());
                    AreEqual("ab\r\nc", p.value.ToString());
                    AreEqual(JsonEvent.ObjectEnd, p.NextEvent());
                    AreEqual(0, p.Level);
                    AreEqual(JsonEvent.EOF, p.NextEvent());
                    AreEqual(JsonEvent.Error, p.NextEvent());
                }
                using (var bytes = new Bytes("[]")) {
                    p.InitParser(bytes);
                    AreEqual(JsonEvent.ArrayStart, p.NextEvent());
                    AreEqual(JsonEvent.ArrayEnd, p.NextEvent());
                    AreEqual(0, p.Level);
                    AreEqual(JsonEvent.EOF, p.NextEvent());
                    AreEqual(JsonEvent.Error, p.NextEvent());
                }
                
                // --------------- primitives on root level --------------- 
                using (var bytes = new Bytes("\"str\"")) {
                    p.InitParser(bytes);
                    AreEqual(JsonEvent.ValueString, p.NextEvent());
                    AreEqual("str", p.value.ToString());
                    AreEqual(JsonEvent.EOF, p.NextEvent());
                    AreEqual(JsonEvent.Error, p.NextEvent());
                }
                using (var bytes = new Bytes("42")) {
                    p.InitParser(bytes);
                    AreEqual(JsonEvent.ValueNumber, p.NextEvent());
                    AreEqual("42", p.value.ToString());
                    AreEqual(JsonEvent.EOF, p.NextEvent());
                    AreEqual(JsonEvent.Error, p.NextEvent());
                }
                using (var bytes = new Bytes("true")) {
                    p.InitParser(bytes);
                    AreEqual(JsonEvent.ValueBool, p.NextEvent());
                    AreEqual(true, p.boolValue);
                    AreEqual(JsonEvent.EOF, p.NextEvent());
                    AreEqual(JsonEvent.Error, p.NextEvent());
                }
                using (var bytes = new Bytes("null")) {
                    p.InitParser(bytes);
                    AreEqual(JsonEvent.ValueNull, p.NextEvent());
                    AreEqual(JsonEvent.EOF, p.NextEvent());
                    AreEqual(JsonEvent.Error, p.NextEvent());
                }
                
                // --------------- invalid strings on root ---------------
                using (var bytes = new Bytes("")) { // empty string is not valid JSON
                    p.InitParser(bytes);
                    AreEqual(JsonEvent.Error, p.NextEvent());
                    AreEqual("JsonParser/JSON error: unexpected EOF on root path: '(root)' at position: 0", p.error.msg.ToString());
                }
                using (var bytes = new Bytes("str")) {
                    p.InitParser(bytes);
                    AreEqual(false, p.error.ErrSet);       // ensure error is cleared
                    AreEqual("", p.error.msg.ToString());  // ensure error message is cleared
                    AreEqual(JsonEvent.Error, p.NextEvent());
                    AreEqual("JsonParser/JSON error: unexpected character while reading value. Found: s path: '(root)' at position: 1", p.error.msg.ToString());
                    AreEqual(1, p.error.Pos);              // ensuring code coverage
                }
                using (var bytes = new Bytes("tx")) { // start as a bool (true)
                    p.InitParser(bytes);
                    AreEqual(JsonEvent.Error, p.NextEvent());
                    AreEqual("JsonParser/JSON error: invalid value: tx path: '(root)' at position: 2", p.error.msg.ToString());
                    AreEqual(2, p.error.Pos);
                }
                using (var bytes = new Bytes("1a")) { // start as a number
                    p.InitParser(bytes);
                    AreEqual(JsonEvent.Error, p.NextEvent());
                    AreEqual("JsonParser/JSON error: unexpected character while reading number. Found : a path: '(root)' at position: 1", p.error.msg.ToString());
                    AreEqual(JsonEvent.Error, p.NextEvent());
                }
            }
        }


        public static void TestNextEvent(Bytes bytes) {
            using (var parser = new Local<JsonParser>())
            {
                ref var p = ref parser.value;
                p.InitParser (bytes);                                  CheckPath(ref p, "(root)");
                AreEqual(JsonEvent.ObjectStart, p.NextEvent());        CheckPath(ref p, "(root)");
                AreEqual(JsonEvent.ValueString, p.NextEvent());        CheckPath(ref p, "eur");
                AreEqual(">€<",                 p.value.ToString());
                AreEqual(JsonEvent.ValueString, p.NextEvent());        CheckPath(ref p, "eur2");
                AreEqual("[€]",                 p.value.ToString());   
                
                AreEqual(JsonEvent.ValueNull,   p.NextEvent());        CheckPath(ref p, "null");
                AreEqual(JsonEvent.ValueBool,   p.NextEvent());        CheckPath(ref p, "true");
                AreEqual(true,                  p.boolValue);
                AreEqual(JsonEvent.ValueBool,   p.NextEvent());        CheckPath(ref p, "false");
                AreEqual(false,                 p.boolValue);
                
                AreEqual(JsonEvent.ObjectStart, p.NextEvent());        CheckPath(ref p, "empty");
                AreEqual("empty",               p.key.ToString());
                AreEqual(JsonEvent.ObjectEnd,   p.NextEvent());        CheckPath(ref p, "empty");
                
                AreEqual(JsonEvent.ObjectStart, p.NextEvent());        CheckPath(ref p, "obj");
                AreEqual(JsonEvent.ValueNumber, p.NextEvent());        CheckPath(ref p, "obj.val");
            //  AreEqual(11,                    parser.number.ParseInt(parseCx));
                AreEqual(JsonEvent.ObjectEnd,   p.NextEvent());        CheckPath(ref p, "obj");
                
                AreEqual(JsonEvent.ArrayStart,  p.NextEvent());        CheckPath(ref p, "arr0[]");
                AreEqual("arr0",                p.key.ToString());
                AreEqual(JsonEvent.ArrayEnd,    p.NextEvent());        CheckPath(ref p, "arr0");
                
                AreEqual(JsonEvent.ArrayStart,  p.NextEvent());        CheckPath(ref p, "arr1[]");
                AreEqual("arr1",                p.key.ToString());
                AreEqual(JsonEvent.ValueNumber, p.NextEvent());        CheckPath(ref p, "arr1[0]");
                AreEqual(JsonEvent.ArrayEnd,    p.NextEvent());        CheckPath(ref p, "arr1");
                
                AreEqual(JsonEvent.ArrayStart,  p.NextEvent());        CheckPath(ref p, "arr2[]");
                AreEqual("arr2",                p.key.ToString());
                AreEqual(JsonEvent.ValueNumber, p.NextEvent());        CheckPath(ref p, "arr2[0]");
                AreEqual(JsonEvent.ValueNumber, p.NextEvent());        CheckPath(ref p, "arr2[1]");
                AreEqual(JsonEvent.ArrayEnd,    p.NextEvent());        CheckPath(ref p, "arr2");
                
                AreEqual(JsonEvent.ArrayStart,  p.NextEvent());        CheckPath(ref p, "arr3[]");
                AreEqual("arr3",                p.key.ToString());
                AreEqual(JsonEvent.ObjectStart, p.NextEvent());        CheckPath(ref p, "arr3[0]");
                AreEqual(JsonEvent.ValueNumber, p.NextEvent());        CheckPath(ref p, "arr3[0].val");
                AreEqual(JsonEvent.ObjectEnd,   p.NextEvent());        CheckPath(ref p, "arr3[0]");       
                AreEqual(JsonEvent.ObjectStart, p.NextEvent());        CheckPath(ref p, "arr3[1]");
                AreEqual(JsonEvent.ValueNumber, p.NextEvent());        CheckPath(ref p, "arr3[1].val");
                AreEqual(JsonEvent.ObjectEnd,   p.NextEvent());        CheckPath(ref p, "arr3[1]");
                AreEqual(JsonEvent.ArrayEnd,    p.NextEvent());        CheckPath(ref p, "arr3");
                
                AreEqual(JsonEvent.ValueString, p.NextEvent());        CheckPath(ref p, "str");
                AreEqual(JsonEvent.ValueNumber, p.NextEvent());        CheckPath(ref p, "int32");
                AreEqual(JsonEvent.ValueNumber, p.NextEvent());        CheckPath(ref p, "dbl");
                
                AreEqual(JsonEvent.ObjectEnd,   p.NextEvent());        CheckPath(ref p, "(root)");
                AreEqual(JsonEvent.EOF,         p.NextEvent());        CheckPath(ref p, "(root)");
                AreEqual(JsonEvent.Error,       p.NextEvent());        CheckPath(ref p, "(root)");
            
                p.InitParser(bytes);
                for (int n = 0; n < 32; n++)
                    p.NextEvent();
                AreEqual(JsonEvent.EOF, p.NextEvent());
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
            using (var parser = new Local<JsonParser>())
            {
                ref var p = ref parser.value;
                using (var bytes = new Bytes("{ err")) {
                    p.InitParser(bytes);
                    p.SkipTree();
                    AreEqual("(root)", p.GetPath());
                }
                using (var bytes = new Bytes("{\"m\" err")) {
                    p.InitParser(bytes);
                    p.SkipTree();
                    AreEqual("m", p.GetPath());
                }
                using (var bytes = new Bytes("[err")) {
                    p.InitParser(bytes);
                    p.SkipTree();
                    AreEqual("[0]", p.GetPath());
                }
                using (var bytes = new Bytes("[1, err")) {
                    p.InitParser(bytes);
                    p.SkipTree();
                    AreEqual("[1]", p.GetPath());
                }
                using (var bytes = new Bytes("err")) {
                    p.InitParser(bytes);
                    p.SkipTree();
                    AreEqual("(root)", p.GetPath());
                }
            }
        }
        
        [Test]
        public void TestSkipping() {
            using (var parser = new Local<JsonParser>())
            {
                ref var p = ref parser.value;
                using (var bytes = new Bytes("{}")) {
                    p.InitParser(bytes);
                    IsTrue(p.SkipTree());
                    AreEqual(1, p.skipInfo.objects);
                    AreEqual(JsonEvent.EOF, p.NextEvent());
                }
                using (var bytes = new Bytes("{\"a\":\"A\"}")) {
                    p.InitParser(bytes);
                    AreEqual(JsonEvent.ObjectStart, p.NextEvent());
                    IsTrue(p.SkipTree());
                    AreEqual(1, p.skipInfo.objects);
                    AreEqual(1, p.skipInfo.strings);
                    AreEqual(JsonEvent.EOF, p.NextEvent());
                }
                using (var bytes = new Bytes("{\"a\":\"A\",\"b\":\"B\"}")) {
                    p.InitParser(bytes);
                    AreEqual(JsonEvent.ObjectStart, p.NextEvent());
                    AreEqual(JsonEvent.ValueString, p.NextEvent()); // consume first property
                    IsTrue(p.SkipTree());
                    AreEqual(1, p.skipInfo.objects);
                    AreEqual(1, p.skipInfo.strings);
                    AreEqual(JsonEvent.EOF, p.NextEvent());
                }
                using (var bytes = new Bytes("[]")) {
                    p.InitParser(bytes);
                    IsTrue(p.SkipTree());
                    AreEqual(1, p.skipInfo.arrays);
                    AreEqual(JsonEvent.EOF, p.NextEvent());
                }
                using (var bytes = new Bytes("\"str\"")) {
                    p.InitParser(bytes);
                    IsTrue(p.SkipTree());
                    AreEqual(1, p.skipInfo.strings);
                    AreEqual(JsonEvent.EOF, p.NextEvent());
                }
                using (var bytes = new Bytes("42")) {
                    p.InitParser(bytes);
                    IsTrue(p.SkipTree());
                    AreEqual(1, p.skipInfo.integers);
                    AreEqual(JsonEvent.EOF, p.NextEvent());
                }
                using (var bytes = new Bytes("true")) {
                    p.InitParser(bytes);
                    IsTrue(p.SkipTree());
                    AreEqual(1, p.skipInfo.booleans);
                    AreEqual(JsonEvent.EOF, p.NextEvent());
                }
                using (var bytes = new Bytes("null")) {
                    p.InitParser(bytes);
                    IsTrue(p.SkipTree());
                    AreEqual(1, p.skipInfo.nulls);
                    AreEqual(JsonEvent.EOF, p.NextEvent());
                }

                // --------------- skipping skipping invalid cases
                using (var bytes = new Bytes("[")) {
                    p.InitParser(bytes);
                    IsFalse(p.SkipTree());
                    AreEqual(JsonEvent.Error, p.NextEvent());
                    IsFalse(p.SkipTree()); // parser state is not changed
                }
                using (var bytes = new Bytes("{")) {
                    p.InitParser(bytes);
                    IsFalse(p.SkipTree());
                    AreEqual(JsonEvent.Error, p.NextEvent());
                }
                using (var bytes = new Bytes("a")) {
                    p.InitParser(bytes);
                    IsFalse(p.SkipTree());
                    AreEqual(JsonEvent.Error, p.NextEvent());
                }
                using (var bytes = new Bytes("")) {
                    p.InitParser(bytes);
                    IsFalse(p.SkipTree());
                    AreEqual(JsonEvent.Error, p.NextEvent());
                }
                using (var bytes = new Bytes("42")) {
                    p.InitParser(bytes);
                    AreEqual(JsonEvent.ValueNumber, p.NextEvent());
                    IsFalse(p.SkipTree()); // parser state is not changed
                    AreEqual(JsonEvent.EOF, p.NextEvent());
                }
                using (var bytes = new Bytes("{}")) {
                    p.InitParser(bytes);
                    AreEqual(JsonEvent.ObjectStart, p.NextEvent());
                    AreEqual(JsonEvent.ObjectEnd, p.NextEvent());
                    IsFalse(p.SkipTree()); // parser state is not changed
                    AreEqual(JsonEvent.EOF, p.NextEvent());
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
            using (JsonParser p = new JsonParser()) {
                // StopWatch stopwatch = new StopWatch();
                for (int n = 0; n < 20000; n++) {
                    memLog.Snapshot();
                    p.InitParser(json);
                    p.SkipTree();
                    if (p.NextEvent() != JsonEvent.EOF)
                        Fail("Expected EOF");
                }
            }
            memLog.AssertNoAllocations();
            // TestContext.Out.WriteLine(memLog.MemorySnapshots());
            // FFLog.log("ParseJson: " + json + " : " + stopwatch.Time());
        }

        [Test]
        public void TestAutoSkip() {
            using (var parser = new Local<JsonParser>()) {
                ref var p = ref parser.value;
                using (var json = new Bytes("{}")) {
                    p.InitParser(json);
                    p.NextEvent();
                    p.ReadRootObject(out JObj obj);
                    while (obj.NextObjectMember(ref p)) {
                        Fail("Expect no members in empty object");
                    }
                    AreEqual(JsonEvent.ObjectEnd, p.Event);
                    AreEqual(JsonEvent.EOF, p.NextEvent());
                }
                using (var json = new Bytes("{\"arr\":[]}")) {
                    p.InitParser(json);
                    p.NextEvent();
                    int arrCount = 0;
                    p.ReadRootObject(out JObj obj);
                    while (obj.NextObjectMember(ref p)) {
                        if (obj.UseMemberArr (ref p, "arr", out JArr arr)) {
                            arrCount++;
                            while (arr.NextArrayElement(ref p))
                                Fail("Expect no array elements");
                        }
                    }
                    AreEqual(JsonEvent.ObjectEnd, p.Event);
                    AreEqual(JsonEvent.EOF, p.NextEvent());
                    AreEqual(1, arrCount);
                }
                using (var json = new Bytes("[]")) {
                    p.InitParser(json);
                    p.ExpectRootArray(out JArr arr);
                    while (arr.NextArrayElement(ref p))
                        Fail("Expect no elements in empty array");
                    AreEqual(JsonEvent.ArrayEnd, p.Event);
                    AreEqual(JsonEvent.EOF, p.NextEvent());
                }
                
                // -------------------- test application exceptions -----------------------
#if DEBUG
                using (var json = new Bytes("{}")) {
                    var e = Throws<InvalidOperationException>(() => {
                        parser.value.InitParser(json);
                        parser.value.ReadRootObject(out JObj _);
                    });
                    AreEqual("ReadRootObject() must be called after JsonParser.NextEvent()", e.Message);
                }
                using (var json = new Bytes("[]")) {
                    var e = Throws<InvalidOperationException>(() => {
                        parser.value.InitParser(json);
                        parser.value.ReadRootArray(out JArr _);
                    });
                    AreEqual("ReadRootArray() must be called after JsonParser.NextEvent()", e.Message);
                }
                using (var json = new Bytes("[]")) {
                    var e = Throws<InvalidOperationException>(() => {
                        parser.value.InitParser(json);
                        parser.value.NextEvent();
                                        
                        parser.value.ReadRootObject(out JObj _);
                    });
                    AreEqual("ReadRootObject() expect JsonParser.Event == ObjectStart", e.Message);
                }
                using (var json = new Bytes("{}")) {
                    var e = Throws<InvalidOperationException>(() => {
                        parser.value.InitParser(json);
                        parser.value.NextEvent();
                        parser.value.ReadRootArray(out JArr _);
                    });
                    AreEqual("ReadRootArray() expect JsonParser.Event == ArrayStart", e.Message);
                }
                using (var json = new Bytes("{\"key\":42}")) {
                    var e = Throws<InvalidOperationException>(() => {
                        parser.value.InitParser(json);
                        parser.value.NextEvent();

                        parser.value.ReadRootObject(out JObj obj);
                        obj.NextObjectMember(ref parser.value);
                        AreEqual(JsonEvent.ValueNumber, parser.value.Event);

                        // call to NextObjectMember() would return false
                        AreEqual(JsonEvent.ObjectEnd, parser.value.NextEvent());
                        IsFalse(obj.NextObjectMember(ref parser.value));
                    });
                    AreEqual("Unexpected iterator level in NextObjectMember()", e.Message);
                }
                using (var json = new Bytes("[42]")) {
                    var e = Throws<InvalidOperationException>(() => {
                        parser.value.InitParser(json);
                        parser.value.NextEvent();

                        parser.value.ReadRootArray(out JArr arr);
                        arr.NextArrayElement(ref parser.value);
                        AreEqual(JsonEvent.ValueNumber, parser.value.Event);

                        // call to NextArrayElement() would return false
                        AreEqual(JsonEvent.ArrayEnd, parser.value.NextEvent());
                        IsFalse(arr.NextArrayElement(ref parser.value));
                    });
                    AreEqual("Unexpected iterator level in NextArrayElement()", e.Message);
                }
                using (var json = new Bytes("[{}]")) {
                    var e = Throws<InvalidOperationException>(() => {
                        parser.value.InitParser(json);
                        parser.value.NextEvent();

                        parser.value.ReadRootArray(out JArr arr);
                        arr.NextArrayElement(ref parser.value);
                        AreEqual(JsonEvent.ObjectStart, parser.value.Event);
                        AreEqual(true, arr.UseElementObj(ref parser.value, out JObj _)); // used object without skipping
                        IsFalse(arr.NextArrayElement(ref parser.value));
                    });
                    AreEqual("unexpected ObjectEnd in NextArrayElement()", e.Message);
                }
                using (var json = new Bytes("{\"arr\":[]}")) {
                    var e = Throws<InvalidOperationException>(() => {
                        parser.value.InitParser(json);
                        parser.value.NextEvent();

                        parser.value.ReadRootObject(out JObj obj);
                        obj.NextObjectMember(ref parser.value);
                        AreEqual(JsonEvent.ArrayStart, parser.value.Event);
                        AreEqual(true, obj.UseMemberArr (ref parser.value, "arr", out JArr _)); // used array without skipping
                        IsFalse(obj.NextObjectMember(ref parser.value));
                    });
                    AreEqual("unexpected ArrayEnd in NextObjectMember()", e.Message);
                }
                using (var json = new Bytes("{}")) {
                    var e = Throws<InvalidOperationException>(() => {
                        parser.value.InitParser(json);
                        parser.value.NextEvent();

                        parser.value.ReadRootObject(out JObj obj);
                        obj.UseMemberObj(ref parser.value, "test", out JObj _);
                    });
                    AreEqual("Must call UseMember...() only after NextObjectMember()", e.Message);
                }
                using (var json = new Bytes("[]")) {
                    var e = Throws<InvalidOperationException>(() => {
                        parser.value.InitParser(json);
                        parser.value.NextEvent();

                        parser.value.ReadRootArray(out JArr arr);
                        arr.UseElementObj(ref parser.value, out JObj _);
                    });
                    AreEqual("Must call UseElement...() only after NextArrayElement()", e.Message);
                }
#endif
            }
        }

        [Test]
        public void TestMaxDepth() {
            using (JsonParser p = new JsonParser())
            using (var jsonDepth1 = new Bytes("[]"))
            using (var jsonDepth2 = new Bytes("[[]]"))
            {
                p.InitParser(jsonDepth1);
                p.SetMaxDepth (1);
                while (true) {
                    var ev = p.NextEvent();
                    if (ev == JsonEvent.EOF) 
                        break; // expected
                    if (ev == JsonEvent.Error)
                        Fail(p.error.msg.ToString());
                }
                p.InitParser(jsonDepth2);
                p.SetMaxDepth (1);
                while (true) {
                    var ev = p.NextEvent();
                    if (ev == JsonEvent.Error) {
                        AreEqual("JsonParser/JSON error: nesting in JSON document exceed maxDepth: 1 path: '[0]' at position: 2", p.error.msg.ToString());
                        break;
                    }
                }
            }
        }
    }
}
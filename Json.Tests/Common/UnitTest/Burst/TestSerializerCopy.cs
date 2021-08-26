// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using Friflo.Json.Burst;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Burst
{
    public class TestSerializerCopy  : LeakTestsFixture
    {
        [Test]
        public void TestManualBuilder() {
            using (Bytes bytes = CommonUtils.FromFile("assets~/Burst/codec/complex.json")) {
                RunManualBuilder(bytes, 1, MemoryLog.Disabled);
            }
        }
        
        [Test]
        public void TestManualNoLeaks() {
            using (Bytes bytes = CommonUtils.FromFile("assets~/Burst/codec/complex.json")) {
                RunManualBuilder(bytes, 10000, MemoryLog.Enabled);
            }
        }
        
        void RunManualBuilder(Bytes bytes, int iterations, MemoryLog memoryLog) {
            var memLog = new MemoryLogger(100, 1000, memoryLog);
            
            using (var parser = new Local<JsonParser>())
            using (var s = new Local<JsonSerializer>())
            {
                ref var p = ref parser.value;
                ref var ser = ref s.value;
                ser.SetPretty(true);

                {
                    memLog.Reset();
                    for (int i = 0; i < iterations; i++) {
                        p.InitParser(bytes);
                        ser.InitSerializer();
                        p.NextEvent(); // ObjectStart
                        ser.ObjectStart();
                        ser.WriteObject(ref p);
                        memLog.Snapshot();
                    }
                    memLog.AssertNoAllocations();
                }
                CommonUtils.ToFile("assets~/Burst/output/writeManual.json", ser.json);
                if (p.error.ErrSet)
                    Fail(p.error.msg.ToString());
                AreEqual(JsonEvent.EOF, p.NextEvent());   // Important to ensure absence of application errors
                
                p.InitParser(bytes);
                p.SkipTree();
                SkipInfo srcSkipInfo = p.skipInfo;

                // validate generated JSON
                p.InitParser(ser.json);
                p.SkipTree();
                AreEqual(JsonEvent.EOF, p.NextEvent());
                IsTrue(p.skipInfo.IsEqual(srcSkipInfo));
            }
        }
        
        [Test]
        public void TestCopyTree() {
            using (var parser = new Local<JsonParser>())
            using (var s = new Local<JsonSerializer>())
            {
                ref var p = ref parser.value;
                ref var ser = ref s.value;
                
                using (var bytes = new Bytes("{}")) {
                    p.InitParser(bytes);
                    ser.InitSerializer();
                    p.NextEvent();
                    IsTrue(ser.WriteTree(ref p));
                    AreEqual(0, p.skipInfo.Sum);
                    AreEqual(JsonEvent.EOF, p.NextEvent());
                    AreEqual("{}", ser.json.ToString());
                }

                using (var bytes = new Bytes("[]")) {
                    p.InitParser(bytes);
                    ser.InitSerializer();
                    p.NextEvent();
                    IsTrue(ser.WriteTree(ref p));
                    AreEqual(0, p.skipInfo.Sum);
                    AreEqual(JsonEvent.EOF, p.NextEvent());
                    AreEqual("[]", ser.json.ToString());
                }

                using (var bytes = new Bytes("\"abc\"")) {
                    p.InitParser(bytes);
                    ser.InitSerializer();
                    p.NextEvent();
                    IsTrue(ser.WriteTree(ref p));
                    AreEqual(0, p.skipInfo.Sum);
                    AreEqual(JsonEvent.EOF, p.NextEvent());
                    AreEqual("\"abc\"", ser.json.ToString());
                }

                using (var bytes = new Bytes("123")) {
                    p.InitParser(bytes);
                    ser.InitSerializer();
                    p.NextEvent();
                    IsTrue(ser.WriteTree(ref p));
                    AreEqual(0, p.skipInfo.Sum);
                    AreEqual(JsonEvent.EOF, p.NextEvent());
                    AreEqual("123", ser.json.ToString());
                }

                using (var bytes = new Bytes("true")) {
                    p.InitParser(bytes);
                    ser.InitSerializer();
                    p.NextEvent();
                    IsTrue(ser.WriteTree(ref p));
                    AreEqual(0, p.skipInfo.Sum);
                    AreEqual(JsonEvent.EOF, p.NextEvent());
                    AreEqual("true", ser.json.ToString());
                }

                using (var bytes = new Bytes("null")) {
                    p.InitParser(bytes);
                    ser.InitSerializer();
                    p.NextEvent();
                    IsTrue(ser.WriteTree(ref p));
                    AreEqual(0, p.skipInfo.Sum);
                    AreEqual(JsonEvent.EOF, p.NextEvent());
                    AreEqual("null", ser.json.ToString());
                }

                // --- some error cases
                using (var bytes = new Bytes("[")) {
                    p.InitParser(bytes);
                    ser.InitSerializer();
                    p.NextEvent();
                    IsFalse(ser.WriteTree(ref p));
                    AreEqual(JsonEvent.Error, p.NextEvent());
                    AreEqual("JsonParser/JSON error: unexpected EOF while reading value path: '[0]' at position: 1", p.error.msg.ToString());
                }

                using (var bytes = new Bytes("{")) {
                    p.InitParser(bytes);
                    ser.InitSerializer();
                    p.NextEvent();
                    IsFalse(ser.WriteTree(ref p));
                    AreEqual(JsonEvent.Error, p.NextEvent());
                    AreEqual("JsonParser/JSON error: unexpected EOF > expect key path: '(root)' at position: 1", p.error.msg.ToString());
                }

                using (var bytes = new Bytes("")) {
                    p.InitParser(bytes);
                    ser.InitSerializer();
                    p.NextEvent();
                    IsFalse(ser.WriteTree(ref p));
                    AreEqual(JsonEvent.Error, p.NextEvent());
                    AreEqual("JsonParser/JSON error: unexpected EOF on root path: '(root)' at position: 0", p.error.msg.ToString());
                }

                using (var bytes = new Bytes("a")) {
                    p.InitParser(bytes);
                    ser.InitSerializer();
                    p.NextEvent();
                    IsFalse(ser.WriteTree(ref p));
                    AreEqual(JsonEvent.Error, p.NextEvent());
                    AreEqual("JsonParser/JSON error: unexpected character while reading value. Found: a path: '(root)' at position: 1", p.error.msg.ToString());
                }
            }
        }
    }
}
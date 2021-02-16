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
            using (Bytes bytes = CommonUtils.FromFile("assets/codec/complex.json")) {
                RunManualBuilder(bytes, 1, MemoryLog.Disabled);
            }
        }
        
        [Test]
        public void TestManualNoLeaks() {
            using (Bytes bytes = CommonUtils.FromFile("assets/codec/complex.json")) {
                RunManualBuilder(bytes, 10000, MemoryLog.Enabled);
            }
        }
        
        void RunManualBuilder(Bytes bytes, int iterations, MemoryLog memoryLog) {
            var memLog = new MemoryLogger(100, 1000, memoryLog);
            var ser = new JsonSerializer();
            var parser = new JsonParser();
            try
            {
                {
                    memLog.Reset();
                    for (int i = 0; i < iterations; i++) {
                        parser.InitParser(bytes);
                        ser.InitSerializer();
                        parser.NextEvent(); // ObjectStart
                        ser.ObjectStart();
                        ser.WriteObject(ref parser);
                        memLog.Snapshot();
                    }
                    memLog.AssertNoAllocations();
                }
                CommonUtils.ToFile("assets/output/writeManual.json", ser.dst);
                if (parser.error.ErrSet)
                    Fail(parser.error.msg.ToString());
                AreEqual(JsonEvent.EOF, parser.NextEvent());   // Important to ensure absence of application errors
                
                parser.InitParser(bytes);
                parser.SkipTree();
                SkipInfo srcSkipInfo = parser.skipInfo;

                // validate generated JSON
                parser.InitParser(ser.dst);
                parser.SkipTree();
                AreEqual(JsonEvent.EOF, parser.NextEvent());
                IsTrue(parser.skipInfo.IsEqual(srcSkipInfo));
            }
            finally {
                parser.Dispose();
                ser.Dispose();
            }
        }
        
        [Test]
        public void TestCopyTree() {
            var parser = new JsonParser();
            var ser = new JsonSerializer();
            try {
                using (var bytes = new Bytes("{}")) {
                    parser.InitParser(bytes);
                    ser.InitSerializer();
                    IsTrue(ser.WriteTree(ref parser));
                    AreEqual(0, parser.skipInfo.Sum);
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                    AreEqual("{}", ser.dst.ToString());
                }

                using (var bytes = new Bytes("[]")) {
                    parser.InitParser(bytes);
                    ser.InitSerializer();
                    IsTrue(ser.WriteTree(ref parser));
                    AreEqual(0, parser.skipInfo.Sum);
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                    AreEqual("[]", ser.dst.ToString());
                }

                using (var bytes = new Bytes("\"abc\"")) {
                    parser.InitParser(bytes);
                    ser.InitSerializer();
                    IsTrue(ser.WriteTree(ref parser));
                    AreEqual(0, parser.skipInfo.Sum);
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                    AreEqual("\"abc\"", ser.dst.ToString());
                }

                using (var bytes = new Bytes("123")) {
                    parser.InitParser(bytes);
                    ser.InitSerializer();
                    IsTrue(ser.WriteTree(ref parser));
                    AreEqual(0, parser.skipInfo.Sum);
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                    AreEqual("123", ser.dst.ToString());
                }

                using (var bytes = new Bytes("true")) {
                    parser.InitParser(bytes);
                    ser.InitSerializer();
                    IsTrue(ser.WriteTree(ref parser));
                    AreEqual(0, parser.skipInfo.Sum);
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                    AreEqual("true", ser.dst.ToString());
                }

                using (var bytes = new Bytes("null")) {
                    parser.InitParser(bytes);
                    ser.InitSerializer();
                    IsTrue(ser.WriteTree(ref parser));
                    AreEqual(0, parser.skipInfo.Sum);
                    AreEqual(JsonEvent.EOF, parser.NextEvent());
                    AreEqual("null", ser.dst.ToString());
                }

                // --- some error cases
                using (var bytes = new Bytes("[")) {
                    parser.InitParser(bytes);
                    ser.InitSerializer();
                    IsFalse(ser.WriteTree(ref parser));
                    AreEqual(JsonEvent.Error, parser.NextEvent());
                    AreEqual("JsonParser/JSON error: unexpected EOF while reading value path: '[0]' at position: 1", parser.error.msg.ToString());
                }

                using (var bytes = new Bytes("{")) {
                    parser.InitParser(bytes);
                    ser.InitSerializer();
                    IsFalse(ser.WriteTree(ref parser));
                    AreEqual(JsonEvent.Error, parser.NextEvent());
                    AreEqual("JsonParser/JSON error: unexpected EOF > expect key path: '(root)' at position: 1", parser.error.msg.ToString());
                }

                using (var bytes = new Bytes("")) {
                    parser.InitParser(bytes);
                    ser.InitSerializer();
                    IsFalse(ser.WriteTree(ref parser));
                    AreEqual(JsonEvent.Error, parser.NextEvent());
                    AreEqual("JsonParser/JSON error: unexpected EOF on root path: '(root)' at position: 0", parser.error.msg.ToString());
                }

                using (var bytes = new Bytes("a")) {
                    parser.InitParser(bytes);
                    ser.InitSerializer();
                    IsFalse(ser.WriteTree(ref parser));
                    AreEqual(JsonEvent.Error, parser.NextEvent());
                    AreEqual("JsonParser/JSON error: unexpected character while reading value. Found: a path: '(root)' at position: 1", parser.error.msg.ToString());
                }
            }
            finally {
                parser.Dispose();
                ser.Dispose();
            }
        }
    }
}
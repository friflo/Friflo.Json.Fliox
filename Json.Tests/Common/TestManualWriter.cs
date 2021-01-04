using Friflo.Json.Burst;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;

using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common
{
    public class TestManualWriter  : LeakTestsFixture
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
            {
                memLog.Reset();
                for (int i = 0; i < iterations; i++) {
                    parser.InitParser(bytes);
                    ser.InitEncoder();
                    parser.NextEvent(); // ObjectStart
                    ser.WriteObject(ref parser);
                    memLog.Snapshot();
                }
                memLog.AssertNoAllocations();
            }
            CommonUtils.ToFile("assets/output/writeManual.json", ser.dst);
            if (parser.error.ErrSet)
                Fail(parser.error.Msg.ToString());
            
            parser.InitParser(bytes);
            parser.SkipTree();
            SkipInfo srcSkipInfo = parser.skipInfo;
            
            // validate generated JSON
            parser.InitParser(ser.dst);
            parser.SkipTree();
            AreEqual(JsonEvent.EOF, parser.NextEvent());
            IsTrue(parser.skipInfo.IsEqual(srcSkipInfo));
            
            parser.Dispose();
            ser.Dispose();
        }
        
        [Test]
        public void TestCopyTree() {
            var parser = new JsonParser();
            var ser = new JsonSerializer();
            using (var bytes = CommonUtils.FromString("{}")) {
                parser.InitParser(bytes);
                ser.InitEncoder();
                IsTrue(ser.WriteTree(ref parser));
                AreEqual(0, parser.skipInfo.Sum);
                AreEqual(JsonEvent.EOF, parser.NextEvent());
                AreEqual("{}", ser.dst.ToString());
            }
            using (var bytes = CommonUtils.FromString("[]")) {
                parser.InitParser(bytes);
                ser.InitEncoder();
                IsTrue(ser.WriteTree(ref parser));
                AreEqual(0, parser.skipInfo.Sum);
                AreEqual(JsonEvent.EOF, parser.NextEvent());
                AreEqual("[]", ser.dst.ToString());
            }
            using (var bytes = CommonUtils.FromString("'abc'")) {
                parser.InitParser(bytes);
                ser.InitEncoder();
                IsTrue(ser.WriteTree(ref parser));
                AreEqual(0, parser.skipInfo.Sum);
                AreEqual(JsonEvent.EOF, parser.NextEvent());
                AreEqual("\"abc\"", ser.dst.ToString());
            }
            using (var bytes = CommonUtils.FromString("123")) {
                parser.InitParser(bytes);
                ser.InitEncoder();
                IsTrue(ser.WriteTree(ref parser));
                AreEqual(0, parser.skipInfo.Sum);
                AreEqual(JsonEvent.EOF, parser.NextEvent());
                AreEqual("123", ser.dst.ToString());
            }
            using (var bytes = CommonUtils.FromString("true")) {
                parser.InitParser(bytes);
                ser.InitEncoder();
                IsTrue(ser.WriteTree(ref parser));
                AreEqual(0, parser.skipInfo.Sum);
                AreEqual(JsonEvent.EOF, parser.NextEvent());
                AreEqual("true", ser.dst.ToString());
            }
            using (var bytes = CommonUtils.FromString("null")) {
                parser.InitParser(bytes);
                ser.InitEncoder();
                IsTrue(ser.WriteTree(ref parser));
                AreEqual(0, parser.skipInfo.Sum);
                AreEqual(JsonEvent.EOF, parser.NextEvent());
                AreEqual("null", ser.dst.ToString());
            }
            
            // --- some error cases
            using (var bytes = CommonUtils.FromString("[")) {
                parser.InitParser(bytes);
                ser.InitEncoder();
                IsFalse(ser.WriteTree(ref parser));
                AreEqual(JsonEvent.Error, parser.NextEvent());
            }
            using (var bytes = CommonUtils.FromString("{")) {
                parser.InitParser(bytes);
                ser.InitEncoder();
                IsFalse(ser.WriteTree(ref parser));
                AreEqual(JsonEvent.Error, parser.NextEvent());
            }
            using (var bytes = CommonUtils.FromString("")) {
                parser.InitParser(bytes);
                ser.InitEncoder();
                IsFalse(ser.WriteTree(ref parser));
                AreEqual(JsonEvent.Error, parser.NextEvent());
            }
            using (var bytes = CommonUtils.FromString("a")) {
                parser.InitParser(bytes);
                ser.InitEncoder();
                IsFalse(ser.WriteTree(ref parser));
                AreEqual(JsonEvent.Error, parser.NextEvent());
            }
            parser.Dispose();
            ser.Dispose();
        }


    }
}
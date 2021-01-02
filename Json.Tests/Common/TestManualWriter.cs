using Friflo.Json.Burst;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;

using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common
{
    public class TestManualWriter  : ECSLeakTestsFixture
    {
        [Test]
        public void TestManualBuilder() {
            using (Bytes bytes = CommonUtils.FromFile("assets/codec/complex.json")) {
                RunManualBuilder(bytes, 1, MemoryLog.Disabled);
            }
        }

        void RunManualBuilder(Bytes bytes, int iterations, MemoryLog memoryLog) {
            var memLog = new MemoryLogger(100, 100, memoryLog);
            var dst = new Bytes(1);
            var ser = new JsonSerializer();
            ser.InitEncoder();
            var parser = new JsonParser();
            {
                memLog.Reset();
                for (int i = 0; i < iterations; i++) {
                    parser.InitParser(bytes);
                    parser.NextEvent(); // ObjectStart
                    WriteObject(ref ser, ref dst, ref parser);
                    memLog.Snapshot();
                }
                memLog.AssertNoAllocations();
            }
            CommonUtils.ToFile("assets/output/writeManual.json", dst);
            if (parser.error.ErrSet)
                Fail(parser.error.Msg.ToString());
            
            parser.InitParser(bytes);
            parser.SkipTree();
            SkipInfo srcSkipInfo = parser.skipInfo;
            
            // validate generated JSON
            parser.InitParser(dst);
            parser.SkipTree();
            AreEqual(JsonEvent.EOF, parser.NextEvent());
            IsTrue(parser.skipInfo.IsEqual(srcSkipInfo));
            
            parser.Dispose();
            ser.Dispose();
            dst.Dispose();
        }

        void WriteObject(ref JsonSerializer ser, ref Bytes dst, ref JsonParser p) {
            ser.ObjectStart(ref dst);
            JsonEvent ev;
            do {
                ev = p.NextEvent();
                switch (ev) {
                    case JsonEvent.ArrayStart:
                        ser.PropertyArray(ref dst, ref p.key);
                        WriteArray(ref ser, ref dst, ref p);
                        break;
                    case JsonEvent.ObjectStart:
                        ser.PropertyObject(ref dst, ref p.key);
                        WriteObject(ref ser, ref dst, ref p);
                        break;
                    case JsonEvent.ValueString:
                        ser.PropertyString(ref dst, ref p.key, ref p.value);
                        break;
                    case JsonEvent.ValueNumber:
                        if (p.isFloat)
                            ser.PropertyDouble(ref dst, ref p.key, p.ValueAsDouble(out _));
                        else
                            ser.PropertyLong(ref dst, ref p.key, p.ValueAsLong(out _));
                        break;
                    case JsonEvent.ValueBool:
                        ser.PropertyBool(ref dst, ref p.key, p.boolValue);
                        break;
                    case JsonEvent.ValueNull:
                        ser.PropertyNull(ref dst, ref p.key);
                        break;
                    case JsonEvent.ObjectEnd:
                        ser.ObjectEnd(ref dst);
                        return;
                    case JsonEvent.ArrayEnd:
                        Fail("unreachable");
                        return;
                    case JsonEvent.Error:
                    case JsonEvent.EOF:
                        return;
                }
            }
            while (p.ContinueObject(ev));
        }
        
        void WriteArray(ref JsonSerializer ser, ref Bytes dst, ref JsonParser p) {
            ser.ArrayStart(ref dst);
            JsonEvent ev;
            do {
                ev = p.NextEvent();
                switch (ev) {
                    case JsonEvent.ArrayStart:
                        WriteArray(ref ser, ref dst, ref p);
                        break;
                    case JsonEvent.ObjectStart:
                        WriteObject(ref ser, ref dst, ref p);
                        break;
                    case JsonEvent.ValueString:
                        ser.ElementString(ref dst, ref p.value);
                        break;
                    case JsonEvent.ValueNumber:
                        if (p.isFloat)
                            ser.ElementDouble(ref dst, p.ValueAsDouble(out _));
                        else
                            ser.ElementLong(ref dst, p.ValueAsLong(out _));
                        break;
                    case JsonEvent.ValueBool:
                        ser.ElementBool(ref dst, p.boolValue);
                        break;
                    case JsonEvent.ValueNull:
                        ser.ElementNull(ref dst);
                        break;
                    case JsonEvent.ObjectEnd:
                        Fail("unreachable");
                        return;
                    case JsonEvent.ArrayEnd:
                        ser.ArrayEnd(ref dst);
                        return;
                    case JsonEvent.Error:
                    case JsonEvent.EOF:
                        return;
                }
            }
            while (p.ContinueArray(ev));
        }
    }
}
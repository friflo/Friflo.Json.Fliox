using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
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

        void RunManualBuilder(Bytes bytes, int iterations, MemoryLog memoryLog) {
            var memLog = new MemoryLogger(100, 100, memoryLog, Fail);
            var ser = new JsonSerializer();
            ser.InitEncoder();
            var parser = new JsonParser();
            {
                memLog.Reset();
                for (int i = 0; i < iterations; i++) {
                    parser.InitParser(bytes);
                    parser.NextEvent(); // ObjectStart
                    WriteObject(ref ser, ref parser);
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

        void WriteObject(ref JsonSerializer ser, ref JsonParser p) {
            ser.ObjectStart();
            JsonEvent ev;
            do {
                ev = p.NextEvent();
                switch (ev) {
                    case JsonEvent.ArrayStart:
                        ser.PropertyArray(ref p.key);
                        WriteArray(ref ser, ref p);
                        break;
                    case JsonEvent.ObjectStart:
                        ser.PropertyObject(ref p.key);
                        WriteObject(ref ser, ref p);
                        break;
                    case JsonEvent.ValueString:
                        ser.PropertyString(ref p.key, ref p.value);
                        break;
                    case JsonEvent.ValueNumber:
                        if (p.isFloat)
                            ser.PropertyDouble(ref p.key, p.ValueAsDouble(out _));
                        else
                            ser.PropertyLong(ref p.key, p.ValueAsLong(out _));
                        break;
                    case JsonEvent.ValueBool:
                        ser.PropertyBool(ref p.key, p.boolValue);
                        break;
                    case JsonEvent.ValueNull:
                        ser.PropertyNull(ref p.key);
                        break;
                    case JsonEvent.ObjectEnd:
                        ser.ObjectEnd();
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
        
        void WriteArray(ref JsonSerializer ser, ref JsonParser p) {
            ser.ArrayStart();
            JsonEvent ev;
            do {
                ev = p.NextEvent();
                switch (ev) {
                    case JsonEvent.ArrayStart:
                        WriteArray(ref ser, ref p);
                        break;
                    case JsonEvent.ObjectStart:
                        WriteObject(ref ser, ref p);
                        break;
                    case JsonEvent.ValueString:
                        ser.ElementString(ref p.value);
                        break;
                    case JsonEvent.ValueNumber:
                        if (p.isFloat)
                            ser.ElementDouble(p.ValueAsDouble(out _));
                        else
                            ser.ElementLong(p.ValueAsLong(out _));
                        break;
                    case JsonEvent.ValueBool:
                        ser.ElementBool(p.boolValue);
                        break;
                    case JsonEvent.ValueNull:
                        ser.ElementNull();
                        break;
                    case JsonEvent.ObjectEnd:
                        Fail("unreachable");
                        return;
                    case JsonEvent.ArrayEnd:
                        ser.ArrayEnd();
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
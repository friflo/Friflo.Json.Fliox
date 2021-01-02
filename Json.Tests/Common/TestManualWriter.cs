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
            var enc = new JsonEncoder();
            enc.InitEncoder();
            var parser = new JsonParser();
            {
                memLog.Reset();
                for (int i = 0; i < iterations; i++) {
                    parser.InitParser(bytes);
                    parser.NextEvent();
                    WriteObject(ref enc, ref dst, ref parser);
                    memLog.Snapshot();
                }
                memLog.AssertNoAllocations();
            }
            parser.Dispose();
            enc.Dispose();
            CommonUtils.ToFile("assets/output/writeManual.json", dst);
            dst.Dispose();
            if (parser.error.ErrSet)
                Fail(parser.error.Msg.ToString());
        }

        void WriteObject(ref JsonEncoder enc, ref Bytes dst, ref JsonParser p) {
            enc.ObjectStart(ref dst);
            JsonEvent ev;
            do {
                ev = p.NextEvent();
                switch (ev) {
                    case JsonEvent.ArrayStart:
                        enc.PropertyArray(ref dst, ref p.key);
                        WriteArray(ref enc, ref dst, ref p);
                        break;
                    case JsonEvent.ObjectStart:
                        enc.PropertyObject(ref dst, ref p.key);
                        WriteObject(ref enc, ref dst, ref p);
                        break;
                    case JsonEvent.ValueString:
                        enc.PropertyString(ref dst, ref p.key, ref p.value);
                        break;
                    case JsonEvent.ValueNumber:
                        if (p.isFloat)
                            enc.PropertyDouble(ref dst, ref p.key, p.ValueAsDouble(out _));
                        else
                            enc.PropertyLong(ref dst, ref p.key, p.ValueAsLong(out _));
                        break;
                    case JsonEvent.ValueBool:
                        enc.PropertyBool(ref dst, ref p.key, p.boolValue);
                        break;
                    case JsonEvent.ValueNull:
                        enc.PropertyNull(ref dst, ref p.key);
                        break;
                    case JsonEvent.ObjectEnd:
                        enc.ObjectEnd(ref dst);
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
        
        void WriteArray(ref JsonEncoder enc, ref Bytes dst, ref JsonParser p) {
            enc.ArrayStart(ref dst);
            JsonEvent ev;
            do {
                ev = p.NextEvent();
                switch (ev) {
                    case JsonEvent.ArrayStart:
                        WriteArray(ref enc, ref dst, ref p);
                        break;
                    case JsonEvent.ObjectStart:
                        WriteObject(ref enc, ref dst, ref p);
                        break;
                    case JsonEvent.ValueString:
                        enc.ElementString(ref dst, ref p.value);
                        break;
                    case JsonEvent.ValueNumber:
                        if (p.isFloat)
                            enc.ElementDouble(ref dst, p.ValueAsDouble(out _));
                        else
                            enc.ElementLong(ref dst, p.ValueAsLong(out _));
                        break;
                    case JsonEvent.ValueBool:
                        enc.ElementBool(ref dst, p.boolValue);
                        break;
                    case JsonEvent.ValueNull:
                        enc.ElementNull(ref dst);
                        break;
                    case JsonEvent.ObjectEnd:
                        Fail("unreachable");
                        return;
                    case JsonEvent.ArrayEnd:
                        enc.ArrayEnd(ref dst);
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
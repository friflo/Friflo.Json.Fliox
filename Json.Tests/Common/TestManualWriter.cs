using Friflo.Json.Burst;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common
{
    public class TestManualWriter
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
                    WriteObject(ref enc, ref dst, ref parser);
                    memLog.Snapshot();
                }
                memLog.AssertNoAllocations();
            }
        }

        void WriteObject(ref JsonEncoder enc, ref Bytes dst, ref JsonParser p) {
            enc.ObjectStart(ref dst);
            var read = new ReadObject();
            while (read.NextEvent(ref p)) {
                switch (read.ev) {
                    case JsonEvent.ArrayStart:
                        WriteArray(ref enc, ref dst, ref p);
                        continue;
                    case JsonEvent.ObjectStart:
                        WriteObject(ref enc, ref dst, ref p);
                        continue;
                    case JsonEvent.ValueString:
                        enc.PropertyString(ref dst, ref p.key, ref p.value);
                        continue;
                    case JsonEvent.ValueNumber:
                        if (p.isFloat)
                            enc.PropertyDouble(ref dst, ref p.key, p.ValueAsDouble(out _));
                        else
                            enc.PropertyLong(ref dst, ref p.key, p.ValueAsLong(out _));
                        continue;
                    case JsonEvent.ValueBool:
                        enc.PropertyBool(ref dst, ref p.key, p.boolValue);
                        continue;
                    case JsonEvent.ValueNull:
                        enc.PropertyNull(ref dst, ref p.key);
                        continue;
                    case JsonEvent.Error:
                    case JsonEvent.EOF:
                        return;
                }
                // unreachable
            }
            enc.ObjectEnd(ref dst);
        }
        
        void WriteArray(ref JsonEncoder enc, ref Bytes dst, ref JsonParser p) {
            enc.ArrayStart(ref dst);
            var read = new ReadObject();
            while (read.NextEvent(ref p)) {
                switch (read.ev) {
                    case JsonEvent.ArrayStart:
                        WriteArray(ref enc, ref dst, ref p);
                        continue;
                    case JsonEvent.ObjectStart:
                        WriteObject(ref enc, ref dst, ref p);
                        continue;
                    case JsonEvent.ValueString:
                        enc.ElementString(ref dst, ref p.value);
                        continue;
                    case JsonEvent.ValueNumber:
                        if (p.isFloat)
                            enc.ElementDouble(ref dst, p.ValueAsDouble(out _));
                        else
                            enc.ElementLong(ref dst, p.ValueAsLong(out _));
                        continue;
                    case JsonEvent.ValueBool:
                        enc.ElementBool(ref dst, p.boolValue);
                        continue;
                    case JsonEvent.ValueNull:
                        enc.ElementNull(ref dst);
                        continue;
                    case JsonEvent.Error:
                    case JsonEvent.EOF:
                        return;
                }
                // unreachable
            }
            enc.ArrayEnd(ref dst);
        }
    }
}

// ReSharper disable InconsistentNaming
using System;
using Friflo.Json.Fliox.MsgPack;
using Gen.Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Fliox.MsgPack.MsgPackUtils;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack.Perf
{
    public static class TestMsgWrite
    {
        private const long Count = 1; // 100_000_000L;

        [Test]
        public static void Perf_Write()
        {
            var writer = new MsgWriter(new byte[10], true);
            var sample = new Sample { x = int.MaxValue };
            writer.WriteMsg(ref sample);
            AreEqual(15, writer.Length);
            AreEqual(HexNorm("82 A1 78 CE 7F FF FF FF A5 63 68 69 6C 64 C0"), writer.DataHex);
            
            // MsgWrite<Sample> write =  Gen_Sample.WriteBin;
            MsgWrite<Sample> write = Gen_Sample.WriteMsg;
            for (long n = 0; n < Count; n++) {
                writer.Init();
                writer.WriteMsg(ref sample);
                // write(ref sample, ref writer);
            }
        }
        
        [Test]
        public static void Perf_Write_2() { Perf_Write(); }
        
        [Test]
        public static void Perf_MapperWrite()
        {
            var sample = new Sample { x = int.MaxValue };
            var mapper = new MsgPackMapper();
            var data = mapper.Write(sample);
            AreEqual(15, data.Length);
            
            for (long n = 0; n < Count; n++) {
                mapper.Write(sample);
            }
        }
        
        [Test]
        public static void Perf_MapperWriteStatic()
        {
            var sample = new Sample { x = int.MaxValue };
            var data = MsgPackMapper.Serialize(sample);
            AreEqual(15, data.Length);
            
            long start = GC.GetAllocatedBytesForCurrentThread();
            for (long n = 0; n < Count; n++) {
                MsgPackMapper.Serialize(sample);
            }
            long diff = GC.GetAllocatedBytesForCurrentThread() - start;
            AreEqual(0, diff);
        }
    }
}
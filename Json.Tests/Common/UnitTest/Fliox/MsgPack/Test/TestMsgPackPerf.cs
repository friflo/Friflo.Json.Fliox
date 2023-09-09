// ReSharper disable InconsistentNaming

using System;
using Friflo.Json.Fliox.MsgPack;
using Gen.Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Fliox.MsgPack.MsgPackUtils;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack.Test
{
    public static class TestMsgPackPerf {
        private const long Count = 1; // 100_000_000L;

        [Test]
        public static void Perf_Write()
        {
            var writer = new MsgWriter(new byte[10], true);
            var sample = new Sample { x = int.MaxValue };
            Gen_Sample.WriteMsg(ref sample, ref writer);
            AreEqual(15, writer.Length);
            AreEqual(HexNorm("82 A1 78 CE 7F FF FF FF A5 63 68 69 6C 64 C0"), writer.DataHex);
            
            // MsgWrite<Sample> write =  Gen_Sample.WriteBin;
            MsgWrite<Sample> write = Gen_Sample.WriteMsg;
            for (long n = 0; n < Count; n++) {
                writer.Init();
                Gen_Sample.WriteMsg(ref sample, ref writer);
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
        
        [Test]
        public static void Perf_MapperRead()
        {
            var sample = new Sample { x = int.MaxValue };
            var data = Sample.Test;
            MsgPackMapper.DeserializeTo(data, ref sample);
            AreEqual(int.MaxValue, sample.x);
            
            long start = GC.GetAllocatedBytesForCurrentThread();
            for (long n = 0; n < Count; n++) {
                MsgPackMapper.DeserializeTo(data, ref sample);
            }
            long diff = GC.GetAllocatedBytesForCurrentThread() - start;
            AreEqual(0, diff);
        }
        
        [Test]
        public static void Perf_Read_2()
        {
            Perf_Read();
        }
        
        [Test]
        public static void Perf_Read()
        {
            var data = Sample.Test;
            var reader = new MsgReader();
            var sample = new Sample();
            reader.Init(data);
            Gen_Sample.ReadMsg(ref sample, ref reader);
            AreEqual(int.MaxValue, sample.x);
            
            for (long n = 0; n < Count; n++) {
                reader.Init(data);
                Gen_Sample.ReadMsg(ref sample, ref reader);
            }
        }
    }
}
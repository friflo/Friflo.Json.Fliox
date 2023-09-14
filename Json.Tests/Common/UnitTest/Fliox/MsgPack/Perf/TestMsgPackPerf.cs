// ReSharper disable InconsistentNaming

using System;
using Friflo.Json.Fliox.MsgPack;
using Gen.Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack.Perf
{
    public static class TestMsgRead
    {
        private const long Count = 1; // 100_000_000L;

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
            reader.ReadMsg(ref sample);
            AreEqual(int.MaxValue, sample.x);
            
            for (long n = 0; n < Count; n++) {
                reader.Init(data);
                reader.ReadMsg(ref sample);
            }
        }
    }
}
using Friflo.Json.Fliox.MsgPack;
using Gen.Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable CommentTypo
// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack.Test
{

    public static class TestMsgWriter
    {
        [Test]
        public static void Write_Child_map()
        {
            var sample = new Sample { x = int.MaxValue, child = new Child { y = 42 } };
            var writer = new MsgWriter(new byte[10], true);
            Gen_Sample.WriteMsg(ref sample, ref writer);
            AreEqual(18, writer.Length);
            AreEqual("82 A1 78 D2 7F FF FF FF A5 63 68 69 6C 64 81 A1 79 2A", writer.DataHex);
        }
        
        [Test]
        public static void Write_FixInt_0()
        {
            var sample = new Sample { x = 0 };
            var writer = new MsgWriter(new byte[10], false);
            Gen_Sample.WriteMsg(ref sample, ref writer);
            AreEqual(4, writer.Length);
            AreEqual("81 A1 78 00", writer.DataHex);
        }
        
        [Test]
        public static void Write_FixInt_127()
        {
            var sample = new Sample { x = 127 };
            var writer = new MsgWriter(new byte[10], false);
            Gen_Sample.WriteMsg(ref sample, ref writer);
            AreEqual(4, writer.Length);
            AreEqual("81 A1 78 7F", writer.DataHex);
        }
        
        [Test]
        public static void Write_FixInt_128()
        {
            var sample = new Sample { x = 128 };
            var writer = new MsgWriter(new byte[10], false);
            Gen_Sample.WriteMsg(ref sample, ref writer);
            AreEqual(5, writer.Length);
            AreEqual("81 A1 78 CC 80", writer.DataHex);
        }
        
        [Test]
        public static void Write_FixInt_255()
        {
            var sample = new Sample { x = 255 };
            var writer = new MsgWriter(new byte[10], false);
            Gen_Sample.WriteMsg(ref sample, ref writer);
            AreEqual(5, writer.Length);
            AreEqual("81 A1 78 CC FF", writer.DataHex);
        }
        
        [Test]
        public static void Write_FixInt_neg_32()
        {
            var sample = new Sample { x = -32 };
            var writer = new MsgWriter(new byte[10], false);
            Gen_Sample.WriteMsg(ref sample, ref writer);
            AreEqual(4, writer.Length);
            AreEqual("81 A1 78 E0", writer.DataHex);
        }
        
        [Test]
        public static void Write_FixInt_neg_33()
        {
            var sample = new Sample { x = -33 };
            var writer = new MsgWriter(new byte[10], false);
            Gen_Sample.WriteMsg(ref sample, ref writer);
            AreEqual(5, writer.Length);
            AreEqual("81 A1 78 D0 DF", writer.DataHex);
        }
        
        [Test]
        public static void Write_FixInt_neg_128()
        {
            var sample = new Sample { x = sbyte.MinValue };
            var writer = new MsgWriter(new byte[10], false);
            Gen_Sample.WriteMsg(ref sample, ref writer);
            AreEqual(5, writer.Length);
            AreEqual("81 A1 78 D0 80", writer.DataHex);
        }
        
        [Test]
        public static void Write_FixInt_neg_129()
        {
            var sample = new Sample { x = -129 };
            var writer = new MsgWriter(new byte[10], false);
            Gen_Sample.WriteMsg(ref sample, ref writer);
            AreEqual(6, writer.Length);
            AreEqual("81 A1 78 D1 FF 7F", writer.DataHex);
        }
        
        [Test]
        public static void Write_FixInt_neg_32768()
        {
            var sample = new Sample { x = short.MinValue };
            var writer = new MsgWriter(new byte[10], false);
            Gen_Sample.WriteMsg(ref sample, ref writer);
            AreEqual(6, writer.Length);
            AreEqual("81 A1 78 D1 80 00", writer.DataHex);
        }
        
        [Test]
        public static void Write_FixInt_neg_32769()
        {
            var sample = new Sample { x = -32769 };
            var writer = new MsgWriter(new byte[10], false);
            Gen_Sample.WriteMsg(ref sample, ref writer);
            AreEqual(8, writer.Length);
            AreEqual("81 A1 78 D2 FF FF 7F FF", writer.DataHex);
        }
        
        [Test]
        public static void Write_FixInt_neg_2147483648()
        {
            var sample = new Sample { x = int.MinValue };
            var writer = new MsgWriter(new byte[10], false);
            Gen_Sample.WriteMsg(ref sample, ref writer);
            AreEqual(8, writer.Length);
            AreEqual("81 A1 78 D2 80 00 00 00", writer.DataHex);
        }
        
        [Test]
        public static void Write_FixInt_neg_2147483649()
        {
            var sample = new Sample { x = -2147483649 };
            var writer = new MsgWriter(new byte[10], false);
            Gen_Sample.WriteMsg(ref sample, ref writer);
            AreEqual(12, writer.Length);
            AreEqual("81 A1 78 D3 FF FF FF FF 7F FF FF FF", writer.DataHex);
        }
        
        [Test]
        public static void Write_FixInt_neg_long_min()
        {
            var sample = new Sample { x = long.MinValue };
            var writer = new MsgWriter(new byte[10], false);
            Gen_Sample.WriteMsg(ref sample, ref writer);
            AreEqual(12, writer.Length);
            AreEqual("81 A1 78 D3 80 00 00 00 00 00 00 00", writer.DataHex);
        }
    }
}
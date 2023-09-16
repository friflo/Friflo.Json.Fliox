using Friflo.Json.Fliox.MsgPack;
using Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack.Test.Reader;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Fliox.MsgPack.MsgPackUtils;

// ReSharper disable IdentifierTypo
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack.Test.Writer
{

    public static class TestMsgWriterFloat
    {
        // --- float
        [Test]
        public static void Write_Float32()
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteFloat64(12.5);
            
            AreEqual(HexNorm("CA 41 48 00 00"), writer.DataHex);
            AreEqual((byte)MsgFormat.float32, writer.Data[0]);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        // --- double
        [Test]
        public static void Write_double_float64()
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteFloat64(1222333444.5);
            
            AreEqual(HexNorm("CB 41 D2 36 D5 01 20 00 00"), writer.DataHex);
            AreEqual((byte)MsgFormat.float64, writer.Data[0]);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [Test]
        public static void Write_double_float32()
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteFloat64(12.5);
            
            AreEqual(HexNorm("CA 41 48 00 00"), writer.DataHex);
            AreEqual((byte)MsgFormat.float32, writer.Data[0]);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [Test]
        public static void Write_double_uint16()
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteFloat64(65535);
            
            AreEqual(HexNorm("CD FF FF"), writer.DataHex);
            AreEqual((byte)MsgFormat.uint16, writer.Data[0]);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [Test]
        public static void Write_double_uint8()
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteFloat64(255);
            
            AreEqual(HexNorm("CC FF"), writer.DataHex);
            AreEqual((byte)MsgFormat.uint8, writer.Data[0]);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [Test]
        public static void Write_double_fixint()
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteFloat64(127);
            
            AreEqual(HexNorm("7F"), writer.DataHex);
            AreEqual((byte)MsgFormat.fixintPos + 127, writer.Data[0]);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [Test]
        public static void Write_double_fixint_negative()
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteFloat64(-32);
            
            AreEqual(HexNorm("E0"), writer.DataHex);
            AreEqual((byte)MsgFormat.fixintNeg, writer.Data[0]);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [Test]
        public static void Write_double_int8()
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteFloat64(-128);
            
            AreEqual(HexNorm("D0 80"), writer.DataHex);
            AreEqual((byte)MsgFormat.int8, writer.Data[0]);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [Test]
        public static void Write_double_int16()
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteFloat64(-32768);
            
            AreEqual(HexNorm("D1 80 00"), writer.DataHex);
            AreEqual((byte)MsgFormat.int16, writer.Data[0]);
            TestMsgReader.AssertSkip(writer.Data);
        }
    }
}
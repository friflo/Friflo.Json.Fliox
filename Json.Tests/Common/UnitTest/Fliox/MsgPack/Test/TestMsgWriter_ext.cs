using System;
using Friflo.Json.Fliox.MsgPack;
using Gen.Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Fliox.MsgPack.MsgPackUtils;
using static Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack.Test.KeyType;

// ReSharper disable InconsistentNaming
// ReSharper disable CommentTypo
// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack.Test
{
    public static class TestMsgWriter_ext
    {
        private const               long    X       = 0x78;
        private static  readonly    byte[]  XArr    = new byte[] { (byte)'x'};
        
        [TestCase(FixStr)] [TestCase(Str8)]
        public static void Write_key_fixext1(KeyType keyType)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            switch (keyType) {
                case FixStr:    writer.WriteKeyExt1(1, X,  1, 0xff);    break;
                case Str8:      writer.WriteKeyExt1 (XArr, 1, 0xff);    break;
            }
            writer.WriteMapFixEnd(0, 1);
            
            AreEqual(HexNorm("81 A1 78 D4 01 FF"), writer.DataHex);
            AreEqual((byte)MsgFormat.fixext1, writer.Data[3]);
            
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [TestCase(FixStr)] [TestCase(Str8)]
        public static void Write_key_fixext2(KeyType keyType)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            switch (keyType) {
                case FixStr:    writer.WriteKeyExt2(1, X,  1, 0x2211);    break;
                case Str8:      writer.WriteKeyExt2 (XArr, 1, 0x2211);    break;
            }
            writer.WriteMapFixEnd(0, 1);
            
            AreEqual(HexNorm("81 A1 78 D5 01 11 22"), writer.DataHex);
            AreEqual((byte)MsgFormat.fixext2, writer.Data[3]);
            
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [TestCase(FixStr)] [TestCase(Str8)]
        public static void Write_key_fixext4(KeyType keyType)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            switch (keyType) {
                case FixStr:    writer.WriteKeyExt4(1, X,  1, 0x4433_2211);    break;
                case Str8:      writer.WriteKeyExt4 (XArr, 1, 0x4433_2211);    break;
            }
            writer.WriteMapFixEnd(0, 1);
            
            AreEqual(HexNorm("81 A1 78 D6 01 11 22 33 44"), writer.DataHex);
            AreEqual((byte)MsgFormat.fixext4, writer.Data[3]);
            
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [TestCase(FixStr)] [TestCase(Str8)]
        public static void Write_key_fixext8(KeyType keyType)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            switch (keyType) {
                case FixStr:    writer.WriteKeyExt8(1, X,  1, 0x0077_6655_4433_2211);    break;
                case Str8:      writer.WriteKeyExt8 (XArr, 1, 0x0077_6655_4433_2211);    break;
            }
            writer.WriteMapFixEnd(0, 1);
            
            AreEqual(HexNorm("81 A1 78 D7 01 11 22 33 44 55 66 77 00"), writer.DataHex);
            AreEqual((byte)MsgFormat.fixext8, writer.Data[3]);
            
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [TestCase(FixStr)] [TestCase(Str8)]
        public static void Write_key_fixext16(KeyType keyType)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            switch (keyType) {
                case FixStr:    writer.WriteKeyExt16(1, X,  1, 0x4444_3333_2222_1111, 0x0888_7777_6666_5555);    break;
                case Str8:      writer.WriteKeyExt16 (XArr, 1, 0x4444_3333_2222_1111, 0x0888_7777_6666_5555);    break;
            }
            writer.WriteMapFixEnd(0, 1);
            
            AreEqual(HexNorm("81 A1 78 D8 01 11 11 22 22 33 33 44 44 55 55 66 66 77 77 88 08"), writer.DataHex);
            AreEqual((byte)MsgFormat.fixext16, writer.Data[3]);
            
            TestMsgReader.AssertSkip(writer.Data);
        }
    }
}
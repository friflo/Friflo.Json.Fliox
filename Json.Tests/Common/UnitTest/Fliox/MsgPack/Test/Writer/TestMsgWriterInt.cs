using Friflo.Json.Fliox.MsgPack;
using Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack.Test.Reader;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Fliox.MsgPack.MsgPackUtils;
using static Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack.Test.Writer.IntType;

// ReSharper disable CommentTypo
// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack.Test.Writer
{
    public enum IntType
    {
        Byte,
        Int16,
        Int32,
        Int64
    }

    public static class TestMsgWriterInt
    {
        // --------------------------- Key (fixstr) / Value (+) integer ---------------------------------
        private const   long     X            = 0x78;
        
        [TestCase(Byte)] [TestCase(Int16)] [TestCase(Int32)] [TestCase(Int64)]
        public static void Write_int_fix_0(IntType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            switch (type) {
                case Byte:  writer.WriteKeyByte (1, X, 0); break;
                case Int16: writer.WriteKeyInt16(1, X, 0); break;
                case Int32: writer.WriteKeyInt32(1, X, 0); break;
                case Int64: writer.WriteKeyInt64(1, X, 0); break;
            }
            writer.WriteMapFixEnd(0, 1);
            
            AreEqual(HexNorm("81 A1 78  00"), writer.DataHex);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [TestCase(Byte)] [TestCase(Int16)] [TestCase(Int32)] [TestCase(Int64)]
        public static void Write_int_fix_127(IntType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            switch (type) {
                case Byte:  writer.WriteKeyByte (1, X, 127); break;
                case Int16: writer.WriteKeyInt16(1, X, 127); break;
                case Int32: writer.WriteKeyInt32(1, X, 127); break;
                case Int64: writer.WriteKeyInt64(1, X, 127); break;
            }
            writer.WriteMapFixEnd(0, 1);
            
            AreEqual(HexNorm("81 A1 78  7F"), writer.DataHex);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [TestCase(Byte)] [TestCase(Int16)] [TestCase(Int32)] [TestCase(Int64)]
        public static void Write_int_128(IntType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            switch (type) {
                case Byte:  writer.WriteKeyByte (1, X, 128); break;
                case Int16: writer.WriteKeyInt16(1, X, 128); break;
                case Int32: writer.WriteKeyInt32(1, X, 128); break;;
                case Int64: writer.WriteKeyInt64(1, X, 128); break;
            }
            writer.WriteMapFixEnd(0, 1);
            
            AreEqual(HexNorm("81 A1 78  CC  80"), writer.DataHex);
            AreEqual((byte)MsgFormat.uint8, writer.Data[3]);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [TestCase(Byte)] [TestCase(Int16)] [TestCase(Int32)] [TestCase(Int64)]
        public static void Write_int_255(IntType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            switch (type) {
                case Byte:  writer.WriteKeyByte (1, X, 255); break;
                case Int16: writer.WriteKeyInt16(1, X, 255); break;
                case Int32: writer.WriteKeyInt32(1, X, 255); break;
                case Int64: writer.WriteKeyInt64(1, X, 255); break;
            }
            writer.WriteMapFixEnd(0, 1);
            
            AreEqual(HexNorm("81 A1 78  CC  FF"), writer.DataHex);
            AreEqual((byte)MsgFormat.uint8, writer.Data[3]);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [TestCase(Int16)] [TestCase(Int32)] [TestCase(Int64)]
        public static void Write_int_256(IntType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            switch (type) {
                case Int16: writer.WriteKeyInt16(1, X, 256); break;
                case Int32: writer.WriteKeyInt32(1, X, 256); break;
                case Int64: writer.WriteKeyInt64(1, X, 256); break;
            }
            writer.WriteMapFixEnd(0, 1);
            
            AreEqual(HexNorm("81 A1 78  CD  01 00"), writer.DataHex);
            AreEqual((byte)MsgFormat.uint16, writer.Data[3]);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [TestCase(Int32)] [TestCase(Int64)]
        public static void Write_int_65535(IntType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            switch (type) {
                case Int32: writer.WriteKeyInt32(1, X, ushort.MaxValue); break;
                case Int64: writer.WriteKeyInt64(1, X, ushort.MaxValue); break;
            }
            writer.WriteMapFixEnd(0, 1);
            
            AreEqual(HexNorm("81 A1 78  CD  FF FF"), writer.DataHex);
            AreEqual((byte)MsgFormat.uint16, writer.Data[3]);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [TestCase(Int32)] [TestCase(Int64)]
        public static void Write_Int_65536(IntType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            switch (type) {
                case Int32: writer.WriteKeyInt32(1, X, 65536); break;
                case Int64: writer.WriteKeyInt64(1, X, 65536); break;
            }
            writer.WriteMapFixEnd(0, 1);
            
            AreEqual(HexNorm("81 A1 78  CE  00 01 00 00"), writer.DataHex);
            AreEqual((byte)MsgFormat.uint32, writer.Data[3]);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [TestCase(Int64)]
        public static void Write_int_4294967295(IntType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            switch (type) {
                case Int64: writer.WriteKeyInt64(1, X, uint.MaxValue); break;
            }
            writer.WriteMapFixEnd(0, 1);
            
            AreEqual(HexNorm("81 A1 78  CE  FF FF FF FF"), writer.DataHex);
            AreEqual((byte)MsgFormat.uint32, writer.Data[3]);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [TestCase(Int64)]
        public static void Write_int_4294967296(IntType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            switch (type) {
                case Int64: writer.WriteKeyInt64(1, X, 4294967296); break;
            }
            writer.WriteMapFixEnd(0, 1);
            
            AreEqual(HexNorm("81 A1 78  CF  00 00 00 01  00 00 00 00"), writer.DataHex);
            AreEqual((byte)MsgFormat.uint64, writer.Data[3]);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [TestCase(Int64)]
        public static void Write_int_long_max(IntType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            switch (type) {
                case Int64: writer.WriteKeyInt64(1, X, long.MaxValue); break;
            }
            writer.WriteMapFixEnd(0, 1);
            
            AreEqual(HexNorm("81 A1 78  CF  7F FF FF FF  FF FF FF FF"), writer.DataHex);
            AreEqual((byte)MsgFormat.uint64, writer.Data[3]);
            TestMsgReader.AssertSkip(writer.Data);
        }
        

        
        // --------------------------------- Key (fixstr) / Value (-) integer ---------------------------------
        [TestCase(Int16)] [TestCase(Int32)] [TestCase(Int64)]
        public static void Write_FixInt_neg_32(IntType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            switch (type) {
                case Int16: writer.WriteKeyInt16(1, X, -32); break;
                case Int32: writer.WriteKeyInt32(1, X, -32); break;
                case Int64: writer.WriteKeyInt64(1, X, -32); break;
            }
            writer.WriteMapFixEnd(0, 1);
            
            AreEqual(HexNorm("81 A1 78  E0"), writer.DataHex);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [TestCase(Int16)] [TestCase(Int32)] [TestCase(Int64)]
        public static void Write_FixInt_neg_33(IntType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            switch (type) {
                case Int16: writer.WriteKeyInt16(1, X, -33); break;
                case Int32: writer.WriteKeyInt32(1, X, -33); break;
                case Int64: writer.WriteKeyInt64(1, X, -33); break;
            }
            writer.WriteMapFixEnd(0, 1);
            
            AreEqual(HexNorm("81 A1 78  D0  DF"), writer.DataHex);
            AreEqual((byte)MsgFormat.int8, writer.Data[3]);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [TestCase(Int16)] [TestCase(Int32)] [TestCase(Int64)]
        public static void Write_FixInt_neg_128(IntType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            switch (type) {
                case Int16: writer.WriteKeyInt16(1, X, sbyte.MinValue); break;
                case Int32: writer.WriteKeyInt32(1, X, sbyte.MinValue); break;
                case Int64: writer.WriteKeyInt64(1, X, sbyte.MinValue); break;
            }
            writer.WriteMapFixEnd(0, 1);
            
            AreEqual(HexNorm("81 A1 78  D0  80"), writer.DataHex);
            AreEqual((byte)MsgFormat.int8, writer.Data[3]);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [TestCase(Int16)] [TestCase(Int32)] [TestCase(Int64)]
        public static void Write_FixInt_neg_129(IntType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            switch (type) {
                case Int16: writer.WriteKeyInt16(1, X, -129); break;
                case Int32: writer.WriteKeyInt32(1, X, -129); break;
                case Int64: writer.WriteKeyInt64(1, X, -129); break;
            }
            writer.WriteMapFixEnd(0, 1);
            
            AreEqual(HexNorm("81 A1 78  D1  FF 7F"), writer.DataHex);
            AreEqual((byte)MsgFormat.int16, writer.Data[3]);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [TestCase(Int16)] [TestCase(Int32)] [TestCase(Int64)]
        public static void Write_FixInt_neg_32768(IntType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            switch (type) {
                case Int16: writer.WriteKeyInt16(1, X, short.MinValue); break;
                case Int32: writer.WriteKeyInt32(1, X, short.MinValue); break;
                case Int64: writer.WriteKeyInt64(1, X, short.MinValue); break;
            }
            writer.WriteMapFixEnd(0, 1);
            
            AreEqual(HexNorm("81 A1 78  D1  80 00"), writer.DataHex);
            AreEqual((byte)MsgFormat.int16, writer.Data[3]);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [TestCase(Int32)] [TestCase(Int64)]
        public static void Write_FixInt_neg_32769(IntType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            switch (type) {
                case Int32: writer.WriteKeyInt32(1, X, -32769); break;
                case Int64: writer.WriteKeyInt64(1, X, -32769); break;
            }
            writer.WriteMapFixEnd(0, 1);
            
            AreEqual(HexNorm("81 A1 78  D2  FF FF 7F FF"), writer.DataHex);
            AreEqual((byte)MsgFormat.int32, writer.Data[3]);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [TestCase(Int32)] [TestCase(Int64)]
        public static void Write_FixInt_neg_2147483648(IntType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            switch (type) {
                case Int32: writer.WriteKeyInt32(1, X, int.MinValue); break;
                case Int64: writer.WriteKeyInt64(1, X, int.MinValue); break;
            }
            writer.WriteMapFixEnd(0, 1);
            
            AreEqual(HexNorm("81 A1 78  D2  80 00 00 00"), writer.DataHex);
            AreEqual((byte)MsgFormat.int32, writer.Data[3]);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [TestCase(Int64)]
        public static void Write_FixInt_neg_2147483649(IntType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            switch (type) {
                case Int64: writer.WriteKeyInt64(1, X, -2147483649); break;
            }
            writer.WriteMapFixEnd(0, 1);
            
            AreEqual(HexNorm("81 A1 78  D3  FF FF FF FF 7F FF FF FF"), writer.DataHex);
            AreEqual((byte)MsgFormat.int64, writer.Data[3]);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [TestCase(Int64)]
        public static void Write_FixInt_neg_long_min(IntType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            switch (type) {
                case Int64: writer.WriteKeyInt64(1, X, long.MinValue); break;
            }
            writer.WriteMapFixEnd(0, 1);
            
            AreEqual(HexNorm("81 A1 78  D3  80 00 00 00 00 00 00 00"), writer.DataHex);
            AreEqual((byte)MsgFormat.int64, writer.Data[3]);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        // --------------------------------- Value integer ---------------------------------
        [Test]
        public static void Write_Byte()
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteByte(0);
            
            AreEqual(HexNorm("00"), writer.DataHex);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [Test]
        public static void Write_Int16()
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteInt16(32767);
            
            AreEqual(HexNorm("CD 7F FF"), writer.DataHex);
            AreEqual((byte)MsgFormat.uint16, writer.Data[0]);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [Test]
        public static void Write_Int32()
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteInt32(2147483647);
            
            AreEqual(HexNorm("CE 7F FF FF FF"), writer.DataHex);
            AreEqual((byte)MsgFormat.uint32, writer.Data[0]);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [Test]
        public static void Write_Int64()
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteInt64(9223372036854775807L);
            
            AreEqual(HexNorm("CF 7F FF FF FF FF FF FF FF"), writer.DataHex);
            AreEqual((byte)MsgFormat.uint64, writer.Data[0]);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        // --- float / double
        [Test]
        public static void Write_Float32()
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteFloat64(12.5);
            
            AreEqual(HexNorm("CA 41 48 00 00"), writer.DataHex);
            AreEqual((byte)MsgFormat.float32, writer.Data[0]);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [Test]
        public static void Write_Float64()
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteFloat64(1222333444.5);
            
            AreEqual(HexNorm("CB 41 D2 36 D5 01 20 00 00"), writer.DataHex);
            AreEqual((byte)MsgFormat.float64, writer.Data[0]);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        // --------------------------------- Key (str) / Value integer ---------------------------------
        private static readonly byte[]     XArr = new byte[] { (byte)'x'};
        
        [Test]
        public static void Write_KeyStr8_Byte()
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            writer.WriteKeyByte(XArr, 0);
            writer.WriteMapFixEnd(0, 1);
            
            AreEqual(HexNorm("81 A1 78 00"), writer.DataHex);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [Test]
        public static void Write_KeyStr8_Int16()
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            writer.WriteKeyInt16(XArr, 0);
            writer.WriteMapFixEnd(0, 1);
            
            AreEqual(HexNorm("81 A1 78 00"), writer.DataHex);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [Test]
        public static void Write_KeyStr8_Int32()
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            writer.WriteKeyInt32(XArr, 0);
            writer.WriteMapFixEnd(0, 1);
            
            AreEqual(HexNorm("81 A1 78 00"), writer.DataHex);
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [Test]
        public static void Write_KeyStr8_Int64()
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            writer.WriteKeyInt64(XArr, 0);
            writer.WriteMapFixEnd(0, 1);
            
            AreEqual(HexNorm("81 A1 78 00"), writer.DataHex);
            TestMsgReader.AssertSkip(writer.Data);
        }
    }
}
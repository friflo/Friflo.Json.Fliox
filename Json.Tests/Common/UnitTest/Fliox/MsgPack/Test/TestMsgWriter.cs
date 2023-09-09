using Friflo.Json.Fliox.MsgPack;
using Gen.Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Fliox.MsgPack.MsgPackUtils;
using static Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack.Test.DataType;

// ReSharper disable CommentTypo
// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack.Test
{
    public enum DataType
    {
        Byte,
        Int16,
        Int32,
        Int64
    }

    public static class TestMsgWriter
    {
        [Test]
        public static void Write_Child_map()
        {
            var sample = new Sample { x = int.MaxValue, child = new Child { y = 42 } };
            var writer = new MsgWriter(new byte[10], true);
            Gen_Sample.WriteMsg(ref sample, ref writer);
            AreEqual(18, writer.Length);
            AreEqual(HexNorm("82 A1 78 CE 7F FF FF FF A5 63 68 69 6C 64 81 A1 79 2A"), writer.DataHex);
        }
        
        // --------------------------------- (+) integer ---------------------------------
        private const   ulong     X            = 0x78;
        
        [TestCase(Byte)] [TestCase(Int64)]
        public static void Write_int_fix_0(DataType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFix();
            switch (type) {
                case Byte:  writer.WriteKeyByte (1, X, 0); break;
                case Int64: writer.WriteKeyInt64(1, X, 0); break;
            }
            writer.WriteMapFixCount(0, 1);
            
            AreEqual(4, writer.Length);
            AreEqual(HexNorm("81 A1 78  00"), writer.DataHex);
        }
        
        [TestCase(Byte)] [TestCase(Int64)]
        public static void Write_int_fix_127(DataType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFix();
            switch (type) {
                case Byte:  writer.WriteKeyByte (1, X, 127); break;
                case Int64: writer.WriteKeyInt64(1, X, 127); break;
            }
            writer.WriteMapFixCount(0, 1);
            
            AreEqual(4, writer.Length);
            AreEqual(HexNorm("81 A1 78  7F"), writer.DataHex);
        }
        
        [TestCase(Byte)] [TestCase(Int64)]
        public static void Write_int_128(DataType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFix();
            switch (type) {
                case Byte:  writer.WriteKeyByte (1, X, 128); break;
                case Int64: writer.WriteKeyInt64(1, X, 128); break;
            }
            writer.WriteMapFixCount(0, 1);
            
            AreEqual(5, writer.Length);
            AreEqual(HexNorm("81 A1 78  CC  80"), writer.DataHex);
            AreEqual((byte)MsgFormat.uint8, writer.Data[3]);
        }
        
        [TestCase(Byte)] [TestCase(Int64)]
        public static void Write_int_255(DataType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFix();
            switch (type) {
                case Byte:  writer.WriteKeyByte (1, X, 255); break;
                case Int64: writer.WriteKeyInt64(1, X, 255); break;
            }
            writer.WriteMapFixCount(0, 1);
            
            AreEqual(5, writer.Length);
            AreEqual(HexNorm("81 A1 78  CC  FF"), writer.DataHex);
            AreEqual((byte)MsgFormat.uint8, writer.Data[3]);
        }
        
        [TestCase(Int64)]
        public static void Write_int_256(DataType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFix();
            switch (type) {
                case Int64: writer.WriteKeyInt64(1, X, 256); break;
            }
            writer.WriteMapFixCount(0, 1);
            
            AreEqual(6, writer.Length);
            AreEqual(HexNorm("81 A1 78  CD  01 00"), writer.DataHex);
            AreEqual((byte)MsgFormat.uint16, writer.Data[3]);
        }
        
        [TestCase(Int64)]
        public static void Write_int_65535(DataType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFix();
            switch (type) {
                case Int64: writer.WriteKeyInt64(1, X, ushort.MaxValue); break;
            }
            writer.WriteMapFixCount(0, 1);
            
            AreEqual(6, writer.Length);
            AreEqual(HexNorm("81 A1 78  CD  FF FF"), writer.DataHex);
            AreEqual((byte)MsgFormat.uint16, writer.Data[3]);
        }
        
        [TestCase(Int64)]
        public static void Write_Int_65536(DataType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFix();
            switch (type) {
                case Int64: writer.WriteKeyInt64(1, X, 65536); break;
            }
            writer.WriteMapFixCount(0, 1);
            
            AreEqual(8, writer.Length);
            AreEqual(HexNorm("81 A1 78  CE  00 01 00 00"), writer.DataHex);
            AreEqual((byte)MsgFormat.uint32, writer.Data[3]);
        }
        
        [TestCase(Int64)]
        public static void Write_int_4294967295(DataType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFix();
            switch (type) {
                case Int64: writer.WriteKeyInt64(1, X, uint.MaxValue); break;
            }
            writer.WriteMapFixCount(0, 1);
            
            AreEqual(8, writer.Length);
            AreEqual(HexNorm("81 A1 78  CE  FF FF FF FF"), writer.DataHex);
            AreEqual((byte)MsgFormat.uint32, writer.Data[3]);
        }
        
        [TestCase(Int64)]
        public static void Write_int_4294967296(DataType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFix();
            switch (type) {
                case Int64: writer.WriteKeyInt64(1, X, 4294967296); break;
            }
            writer.WriteMapFixCount(0, 1);
            
            AreEqual(12, writer.Length);
            AreEqual(HexNorm("81 A1 78  CF  00 00 00 01  00 00 00 00"), writer.DataHex);
            AreEqual((byte)MsgFormat.uint64, writer.Data[3]);
        }
        
        [TestCase(Int64)]
        public static void Write_int_long_max(DataType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFix();
            switch (type) {
                case Int64: writer.WriteKeyInt64(1, X, long.MaxValue); break;
            }
            writer.WriteMapFixCount(0, 1);
            
            AreEqual(12, writer.Length);
            AreEqual(HexNorm("81 A1 78  CF  7F FF FF FF  FF FF FF FF"), writer.DataHex);
            AreEqual((byte)MsgFormat.uint64, writer.Data[3]);
        }
        

        
        // --------------------------------- (-) integer ---------------------------------
        [TestCase(Int64)]
        public static void Write_FixInt_neg_32(DataType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFix();
            switch (type) {
                case Int64: writer.WriteKeyInt64(1, X, -32); break;
            }
            writer.WriteMapFixCount(0, 1);
            
            AreEqual(4, writer.Length);
            AreEqual(HexNorm("81 A1 78  E0"), writer.DataHex);
        }
        
        [TestCase(Int64)]
        public static void Write_FixInt_neg_33(DataType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFix();
            switch (type) {
                case Int64: writer.WriteKeyInt64(1, X, -33); break;
            }
            writer.WriteMapFixCount(0, 1);
            
            AreEqual(5, writer.Length);
            AreEqual(HexNorm("81 A1 78  D0  DF"), writer.DataHex);
            AreEqual((byte)MsgFormat.int8, writer.Data[3]);

        }
        
        [TestCase(Int64)]
        public static void Write_FixInt_neg_128(DataType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFix();
            switch (type) {
                case Int64: writer.WriteKeyInt64(1, X, sbyte.MinValue); break;
            }
            writer.WriteMapFixCount(0, 1);
            
            AreEqual(5, writer.Length);
            AreEqual(HexNorm("81 A1 78  D0  80"), writer.DataHex);
            AreEqual((byte)MsgFormat.int8, writer.Data[3]);
        }
        
        [TestCase(Int64)]
        public static void Write_FixInt_neg_129(DataType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFix();
            switch (type) {
                case Int64: writer.WriteKeyInt64(1, X, -129); break;
            }
            writer.WriteMapFixCount(0, 1);
            
            AreEqual(6, writer.Length);
            AreEqual(HexNorm("81 A1 78  D1  FF 7F"), writer.DataHex);
            AreEqual((byte)MsgFormat.int16, writer.Data[3]);
        }
        
        [TestCase(Int64)]
        public static void Write_FixInt_neg_32768(DataType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFix();
            switch (type) {
                case Int64: writer.WriteKeyInt64(1, X, short.MinValue); break;
            }
            writer.WriteMapFixCount(0, 1);
            
            AreEqual(6, writer.Length);
            AreEqual(HexNorm("81 A1 78  D1  80 00"), writer.DataHex);
            AreEqual((byte)MsgFormat.int16, writer.Data[3]);
        }
        
        [TestCase(Int64)]
        public static void Write_FixInt_neg_32769(DataType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFix();
            switch (type) {
                case Int64: writer.WriteKeyInt64(1, X, -32769); break;
            }
            writer.WriteMapFixCount(0, 1);
            
            AreEqual(8, writer.Length);
            AreEqual(HexNorm("81 A1 78  D2  FF FF 7F FF"), writer.DataHex);
            AreEqual((byte)MsgFormat.int32, writer.Data[3]);
        }
        
        [TestCase(Int64)]
        public static void Write_FixInt_neg_2147483648(DataType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFix();
            switch (type) {
                case Int64: writer.WriteKeyInt64(1, X, int.MinValue); break;
            }
            writer.WriteMapFixCount(0, 1);
            
            AreEqual(8, writer.Length);
            AreEqual(HexNorm("81 A1 78  D2  80 00 00 00"), writer.DataHex);
            AreEqual((byte)MsgFormat.int32, writer.Data[3]);
        }
        
        [TestCase(Int64)]
        public static void Write_FixInt_neg_2147483649(DataType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFix();
            switch (type) {
                case Int64: writer.WriteKeyInt64(1, X, -2147483649); break;
            }
            writer.WriteMapFixCount(0, 1);
            
            AreEqual(12, writer.Length);
            AreEqual(HexNorm("81 A1 78  D3  FF FF FF FF 7F FF FF FF"), writer.DataHex);
            AreEqual((byte)MsgFormat.int64, writer.Data[3]);
        }
        
        [TestCase(Int64)]
        public static void Write_FixInt_neg_long_min(DataType type)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFix();
            switch (type) {
                case Int64: writer.WriteKeyInt64(1, X, long.MinValue); break;
            }
            writer.WriteMapFixCount(0, 1);
            
            AreEqual(12, writer.Length);
            AreEqual(HexNorm("81 A1 78  D3  80 00 00 00 00 00 00 00"), writer.DataHex);
            AreEqual((byte)MsgFormat.int64, writer.Data[3]);
        }
    }
}
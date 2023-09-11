using Friflo.Json.Fliox.MsgPack;
using Gen.Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Fliox.MsgPack.MsgPackUtils;
using static Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack.Test.KeyType;

// ReSharper disable CommentTypo
// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack.Test
{
    public enum KeyType
    {
        FixStr,
        Str8,
    }
    
    public static class TestMsgWriter
    {
        [Test]
        public static void Write_Child_map()
        {
            var sample = new Sample { x = int.MaxValue, child = new Child { y = 42 } };
            var writer = new MsgWriter(new byte[10], true);
            Gen_Sample.WriteMsg(ref writer, ref sample);
            AreEqual(18, writer.Length);
            AreEqual(HexNorm("82 A1 78 CE 7F FF FF FF A5 63 68 69 6C 64 81 A1 79 2A"), writer.DataHex);
        }
        
        private const               long    X       = 0x78;
        private static  readonly    byte[]  XArr    = new byte[] { (byte)'x'};
        
        [TestCase(FixStr)] [TestCase(Str8)]
        public static void Write_key_strfix(KeyType keyType)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFix();
            switch (keyType) {
                case FixStr:    writer.WriteKeyString (1, X, "abc");    break;
                case Str8:      writer.WriteKeyString (XArr, "abc");    break;
            }
            writer.WriteMapFixCount(0, 1);
            
            AreEqual(HexNorm("81 A1 78 A3 61 62 63"), writer.DataHex);
        }
        
       
        [TestCase(FixStr)] [TestCase(Str8)]
        public static void Write_key_str8(KeyType keyType)
        {
            var val = "_123456789_123456789_123456789_123456789";
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFix();
            switch (keyType) {
                case FixStr:    writer.WriteKeyString (1, X, val);      break;
                case Str8:      writer.WriteKeyString (XArr, val);      break;
            }
            writer.WriteMapFixCount(0, 1);
            
            AreEqual(HexNorm("81 A1 78 D9 28 5F 31 32 33 34 35 36 37 38 39 5F 31 32 33 34 35 36 37 38 39 5F 31 32 33 34 35 36 37 38 39 5F 31 32 33 34 35 36 37 38 39"), writer.DataHex);
        }
        
        [TestCase(FixStr)] [TestCase(Str8)]
        public static void Write_key_str16(KeyType keyType)
        {
            var val = new string('a', 300);
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFix();
            switch (keyType) {
                case FixStr:    writer.WriteKeyString (1, X, val);      break;
                case Str8:      writer.WriteKeyString (XArr, val);      break;
            }
            writer.WriteMapFixCount(0, 1);
            
            var reader = new MsgReader(writer.Data);
            reader.ReadObject(out _);
            reader.ReadKey();
            var x = reader.ReadString();
            AreEqual(300, x.Length);
            AreEqual(val, x);
        }
        
        [TestCase(FixStr)] [TestCase(Str8)]
        public static void Write_key_str32(KeyType keyType)
        {
            var val = new string('a', 70000);
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFix();
            switch (keyType) {
                case FixStr:    writer.WriteKeyString (1, X, val);      break;
                case Str8:      writer.WriteKeyString (XArr, val);      break;
            }
            writer.WriteMapFixCount(0, 1);
            
            var reader = new MsgReader(writer.Data);
            reader.ReadObject(out _);
            reader.ReadKey();
            var x = reader.ReadString();
            AreEqual(70000, x.Length);
            AreEqual(val, x);
        }
        
        [TestCase(FixStr)] [TestCase(Str8)]
        public static void Write_key_bool(KeyType keyType)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFix();
            switch (keyType) {
                case FixStr:    writer.WriteKeyBool (1, X, true);      break;
                case Str8:      writer.WriteKeyBool (XArr, true);      break;
            }
            writer.WriteMapFixCount(0, 1);
            
            AreEqual(HexNorm("81 A1 78 C3"), writer.DataHex);
        }
        
        [Test]
        public static void Write_array_fix()
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteArray(1);
            writer.WriteInt32(42);
            
            AreEqual(HexNorm("91 2A"), writer.DataHex);
            
            var reader = new MsgReader(writer.Data);
            reader.ReadArray(out int length);
            AreEqual(1, length);
            var item = reader.ReadInt32();
            AreEqual(42, item);
        }
        
        [Test]
        public static void Write_array_16()
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteArray(16);
            for (int n = 0; n < 16; n++) { 
                writer.WriteInt32(n);
            }
            
            AreEqual(HexNorm("DC 00 10 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F"), writer.DataHex);
            
            var reader = new MsgReader(writer.Data);
            reader.ReadArray(out int length);
            AreEqual(16, length);
            for (int n = 0; n < length; n++) {
                var item = reader.ReadInt32();
                AreEqual(n, item);
            }
        }
        
        [Test]
        public static void Write_array_32()
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteArray(0x10000);
            for (int n = 0; n < 0x10000; n++) { 
                writer.WriteInt32(n);
            }
            AreEqual(196229, writer.Data.Length);
            
            var reader = new MsgReader(writer.Data);
            reader.ReadArray(out int length);
            AreEqual(0x10000, length);
            for (int n = 0; n < length; n++) {
                var item = reader.ReadInt32();
                if (item != n) Fail("expected: {n}, was: {item}");
            }
        }
    }
}
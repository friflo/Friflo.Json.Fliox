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
            Gen_Sample.WriteMsg(ref sample, ref writer);
            AreEqual(18, writer.Length);
            AreEqual(HexNorm("82 A1 78 CE 7F FF FF FF A5 63 68 69 6C 64 81 A1 79 2A"), writer.DataHex);
        }
        
        private const               long    X       = 0x78;
        private static  readonly    byte[]  XArr    = new byte[] { (byte)'x'};
        
        [TestCase(FixStr)] [TestCase(Str8)]
        public static void Write_keyfix_strfix(KeyType keyType)
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
        public static void Write_keyfix_str8(KeyType keyType)
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
        public static void Write_keyfix_str16(KeyType keyType)
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
        public static void Write_keyfix_str32(KeyType keyType)
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
    }
}
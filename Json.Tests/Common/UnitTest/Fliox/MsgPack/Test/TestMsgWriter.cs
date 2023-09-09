using Friflo.Json.Fliox.MsgPack;
using Gen.Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Fliox.MsgPack.MsgPackUtils;
using static Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack.Test.IntType;

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
            AreEqual(HexNorm("82 A1 78 CE 7F FF FF FF A5 63 68 69 6C 64 81 A1 79 2A"), writer.DataHex);
        }
        
        private const               long    X       = 0x78;
        private static  readonly    byte[]  XArr    = new byte[] { (byte)'x'};
        
        [Test]
        public static void Write_keyfix_strfix()
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFix();
            writer.WriteKeyString (1, X, "abc");
            writer.WriteMapFixCount(0, 1);
            
            AreEqual(HexNorm("81 A1 78 A3 61 62 63"), writer.DataHex);
        }
        
        [Test]
        public static void Write_keystr_strfix()
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFix();
            writer.WriteKeyString (XArr, "abc");
            writer.WriteMapFixCount(0, 1);
            
            AreEqual(HexNorm("81 A1 78 A3 61 62 63"), writer.DataHex);
        }
        
    }
}
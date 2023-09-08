using Friflo.Json.Fliox.MsgPack;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Fliox.MsgPack.MsgFormatUtils;

// ReSharper disable RedundantExplicitArrayCreation
// ReSharper disable StringLiteralTypo
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack.Test
{
    public static class TestMsgPackHappy
    {
        [Test]
        public static void Test_ReadFixInt() {
            var data = HexToSpan("81 A1 78 2A"); // {"x": 42}
            var obj = MsgPackMapper.Deserialize<Sample>(data, out _);
            AreEqual(obj.x, 42);
        }
        
        [Test]
        public static void Test_ReadFixIntNegative() {
            var data = HexToSpan("81 A1 78 E0"); // {"x": -32}
            var obj = MsgPackMapper.Deserialize<Sample>(data, out _);
            AreEqual(obj.x, -32);
        }
    }
}
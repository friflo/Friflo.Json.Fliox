using System;
using Friflo.Json.Fliox.MsgPack;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable RedundantExplicitArrayCreation
// ReSharper disable StringLiteralTypo
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack
{
    public static class TestMsgPackHappy
    {
        [Test]
        public static void Test_ReadFixInt() {
            ReadOnlySpan<byte> data = new byte[] { 129, 161, 120, 42 }; // {"x": 42}
            var obj = MsgPackMapper.Deserialize<Sample>(data, out _);
            AreEqual(obj.x, 42);
        }
        
        [Test]
        public static void Test_ReadFixIntNegative() {
            ReadOnlySpan<byte> data = new byte[] { 129, 161, 120, 224 }; // {"x": -32}
            var obj = MsgPackMapper.Deserialize<Sample>(data, out _);
            AreEqual(obj.x, -32);
        }
    }
}
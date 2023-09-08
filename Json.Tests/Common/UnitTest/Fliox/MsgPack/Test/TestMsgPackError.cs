using Friflo.Json.Fliox.MsgPack;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Fliox.MsgPack.MsgPackUtils;

// ReSharper disable RedundantExplicitArrayCreation
// ReSharper disable StringLiteralTypo
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack.Test
{

    public static class TestMsgPackError
    {
        [Test]
        public static void TestError_ObjectWasFixInt() {
            var data = ByteToSpan(MsgFormat.fixintPos);
            MsgPackMapper.Deserialize<Sample>(data, out var error);
            AreEqual("MessagePack error - expect object or null. was: +fixint(0x0) pos: 0 (root)", error);
            
            data = ByteToSpan(MsgFormat.fixintPosMax);
            MsgPackMapper.Deserialize<Sample>(data, out error);
            AreEqual("MessagePack error - expect object or null. was: +fixint(0x7F) pos: 0 (root)", error);
        }
        
        [Test]
        public static void ObjectWasFixInt() {
            var data = ByteToSpan(MsgFormat.fixstr);
            MsgPackMapper.Deserialize<Sample>(data, out var error);
            AreEqual("MessagePack error - expect object or null. was: fixstr(0xA0) pos: 0 (root)", error);
            
            data = ByteToSpan(MsgFormat.fixstrMax);
            MsgPackMapper.Deserialize<Sample>(data, out error);
            AreEqual("MessagePack error - expect object or null. was: fixstr(0xBF) pos: 0 (root)", error);
        }
        
        [Test]
        public static void ObjectWasFixArray() {
            var data = ByteToSpan(MsgFormat.fixarray);
            MsgPackMapper.Deserialize<Sample>(data, out var error);
            AreEqual("MessagePack error - expect object or null. was: fixarray(0x90) pos: 0 (root)", error);
            
            data = ByteToSpan(MsgFormat.fixarrayMax);
            MsgPackMapper.Deserialize<Sample>(data, out error);
            AreEqual("MessagePack error - expect object or null. was: fixarray(0x9F) pos: 0 (root)", error);
        }
        
        [Test]
        public static void ObjectWasFixIntNegative() {
            var data = ByteToSpan(MsgFormat.fixintNeg);
            MsgPackMapper.Deserialize<Sample>(data, out var error);
            AreEqual("MessagePack error - expect object or null. was: -fixint(0xE0) pos: 0 (root)", error);
            
            data = ByteToSpan(MsgFormat.fixintNegMax);
            MsgPackMapper.Deserialize<Sample>(data, out error);
            AreEqual("MessagePack error - expect object or null. was: -fixint(0xFF) pos: 0 (root)", error);
        }
        
        [Test]
        public static void MemberError() {
            var data = HexToSpan("81 A1 63"); // [81 A1 63 0B] - { "c": 11 }
            MsgPackMapper.Deserialize<Sample>(data, out var error);
            AreEqual("MessagePack error - unexpected EOF. pos: 3 - last key: 'c'", error);
            
            data = HexToSpan("81 A1 78"); // [81 A1 78 2A] - { "x": 42 }
            MsgPackMapper.Deserialize<Sample>(data, out error);
            AreEqual("MessagePack error - unexpected EOF. pos: 3 - last key: 'x'", error);
        }
        
        [Test]
        public static void Int32_OutOfRange() {
            var data = HexToSpan("81 A1 78 CB 41 EF FF FF FF E0 00 00"); // { "x": 4294967295 }
            MsgPackMapper.Deserialize<Sample>(data, out var error);
            AreEqual("MessagePack error - value out of range. was: 4294967295 float64(0xCB) pos: 3 - last key: 'x'", error);
        }
    }
}
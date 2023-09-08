using System;
using Friflo.Json.Fliox.MsgPack;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable RedundantExplicitArrayCreation
// ReSharper disable StringLiteralTypo
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack.Test
{

    public static class TestMsgPackError
    {
        [Test]
        public static void TestError_ObjectWasFixInt() {
            ReadOnlySpan<byte> data = new byte[] { 0 };
            MsgPackMapper.Deserialize<Sample>(data, out var error);
            AreEqual("MessagePack error - expect object or null. was: +fixint(0x0) pos: 0 (root)", error);
            
            data = new byte[] { (byte)MsgFormat.fixintPosMax };
            MsgPackMapper.Deserialize<Sample>(data, out error);
            AreEqual("MessagePack error - expect object or null. was: +fixint(0x7F) pos: 0 (root)", error);
        }
        
        [Test]
        public static void ObjectWasFixInt() {
            ReadOnlySpan<byte> data = new byte[] { (byte)MsgFormat.fixstr };
            MsgPackMapper.Deserialize<Sample>(data, out var error);
            AreEqual("MessagePack error - expect object or null. was: fixstr(0xA0) pos: 0 (root)", error);
            
            data = new byte[] { (byte)MsgFormat.fixstrMax };
            MsgPackMapper.Deserialize<Sample>(data, out error);
            AreEqual("MessagePack error - expect object or null. was: fixstr(0xBF) pos: 0 (root)", error);
        }
        
        [Test]
        public static void ObjectWasFixArray() {
            ReadOnlySpan<byte> data = new byte[] { (byte)MsgFormat.fixarray };
            MsgPackMapper.Deserialize<Sample>(data, out var error);
            AreEqual("MessagePack error - expect object or null. was: fixarray(0x90) pos: 0 (root)", error);
            
            data = new byte[] { (byte)MsgFormat.fixarrayMax };
            MsgPackMapper.Deserialize<Sample>(data, out error);
            AreEqual("MessagePack error - expect object or null. was: fixarray(0x9F) pos: 0 (root)", error);
        }
        
        [Test]
        public static void ObjectWasFixIntNegative() {
            ReadOnlySpan<byte> data = new byte[] { (byte)MsgFormat.fixintNeg };
            MsgPackMapper.Deserialize<Sample>(data, out var error);
            AreEqual("MessagePack error - expect object or null. was: -fixint(0xE0) pos: 0 (root)", error);
            
            data = new byte[] { (byte)MsgFormat.fixintNegMax };
            MsgPackMapper.Deserialize<Sample>(data, out error);
            AreEqual("MessagePack error - expect object or null. was: -fixint(0xFF) pos: 0 (root)", error);
        }
        
        [Test]
        public static void MemberError() {
            ReadOnlySpan<byte> data = new byte[] { 129, 161, 99 }; // [129, 161, 99, 11] - {"c": 11}
            MsgPackMapper.Deserialize<Sample>(data, out var error);
            AreEqual("MessagePack error - unexpected EOF. pos: 3 - last key: 'c'", error);
            
            data = new byte[] { 129, 161, 120 }; // [129, 161, 120, 42] - {"x": 42}
            MsgPackMapper.Deserialize<Sample>(data, out error);
            AreEqual("MessagePack error - unexpected EOF. pos: 3 - last key: 'x'", error);
        }
        
        [Test]
        public static void Int32_OutOfRange() {
            ReadOnlySpan<byte> data = new byte[] { 129, 161, 120, 203, 65, 239, 255, 255, 255, 224, 0, 0 }; // { "x": 4294967295 }
            MsgPackMapper.Deserialize<Sample>(data, out var error);
            AreEqual("MessagePack error - value out of range. was: 4294967295 float64(0xCB) pos: 3 - last key: 'x'", error);
        }
    }
}
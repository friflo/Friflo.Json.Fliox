using System;
using Friflo.Json.Fliox.MsgPack;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Fliox.MsgPack.MsgFormatUtils;


// ReSharper disable CommentTypo
// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack.Test
{

    public static partial class TestMsgReader
    {
        [Test]
        public static void Read_float64()
        {
            var data = HexToSpan("cb 41 EF FF FF FF E0 00 00"); // 4294967295 (float64)
            // var value = BitConverter.DoubleToInt64Bits(4294967295);
            AreEqual((byte)MsgFormat.float64, data[0]);
            {
                var reader = new MsgReader(data);
                var x = reader.ReadFloat64();
                AreEqual(4294967295, x);
                AreEqual(data.Length, reader.Pos);
            }         {
                var reader = new MsgReader(data);
                var x = reader.ReadFloat32();
                AreEqual(4294967295, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadInt64();
                AreEqual(4294967295, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                reader.ReadInt32();
                AreEqual("MessagePack error - value out of range. was: 4294967295 float64(0xCB) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadInt16();
                AreEqual("MessagePack error - value out of range. was: 4294967295 float64(0xCB) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadByte();
                AreEqual("MessagePack error - value out of range. was: 4294967295 float64(0xCB) pos: 0 (root)", reader.Error);
            }
        }
        
        [Test]
        public static void Read_float64_max()
        {
            var data = HexToSpan("cb 7F EF FF FF FF FF FF FF"); // double.MaxValue (float64)
            var value = BitConverter.DoubleToInt64Bits(double.MaxValue);
            AreEqual((byte)MsgFormat.float64, data[0]);
            {
                var reader = new MsgReader(data);
                var x = reader.ReadFloat64();
                AreEqual(double.MaxValue, x);
            } {
                var reader = new MsgReader(data);
                reader.ReadFloat32();
                AreEqual("MessagePack error - value out of range. was: 1,7976931348623157E+308 float64(0xCB) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadInt64();
                AreEqual("MessagePack error - value out of range. was: 1,7976931348623157E+308 float64(0xCB) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadInt32();
                AreEqual("MessagePack error - value out of range. was: 1,7976931348623157E+308 float64(0xCB) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadInt16();
                AreEqual("MessagePack error - value out of range. was: 1,7976931348623157E+308 float64(0xCB) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadByte();
                AreEqual("MessagePack error - value out of range. was: 1,7976931348623157E+308 float64(0xCB) pos: 0 (root)", reader.Error);
            }
        }
    }
}
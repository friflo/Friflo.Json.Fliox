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
            }         {
                var reader = new MsgReader(data);
                var x = reader.ReadFloat32();
                AreEqual(4294967295, x);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadInt64();
                AreEqual(4294967295, x);
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
        public static void Read_float32()
        {
            var data = HexToSpan("ca 4A FF FF FE"); // 8388607 (float32)
            var value = BitConverter.SingleToInt32Bits(8388607);
            
            AreEqual((byte)MsgFormat.float32, data[0]);
            {
                var reader = new MsgReader(data);
                var x = reader.ReadFloat64();
                AreEqual(8388607, x);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadFloat32();
                AreEqual(8388607, x);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadInt64();
                AreEqual(8388607, x);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadInt32();
                AreEqual(8388607, x);
            } {
                var reader = new MsgReader(data);
                reader.ReadInt16();
                AreEqual("MessagePack error - value out of range. was: 8388607 float32(0xCA) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadByte();
                AreEqual("MessagePack error - value out of range. was: 8388607 float32(0xCA) pos: 0 (root)", reader.Error);
            }
        }
    }
}
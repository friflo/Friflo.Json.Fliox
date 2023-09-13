using System;
using Friflo.Json.Fliox.MsgPack;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Fliox.MsgPack.MsgPackUtils;


// ReSharper disable CommentTypo
// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack.Test
{

    public static partial class TestMsgReader
    {
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
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadFloat32();
                AreEqual(8388607, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadInt64();
                AreEqual(8388607, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadInt32();
                AreEqual(8388607, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                reader.ReadInt16();
                AreEqual("MessagePack error - value out of range / expect int16. was: 8388607 float32(0xCA) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadByte();
                AreEqual("MessagePack error - value out of range / expect uint8. was: 8388607 float32(0xCA) pos: 0 (root)", reader.Error);
            }
        }
        
        [Test]
        public static void Read_float32_max()
        {
            var data = HexToSpan("ca 7F 7F FF FF"); // float.MaxValue (float32)
            var value = BitConverter.SingleToInt32Bits(float.MaxValue);
            
            AreEqual((byte)MsgFormat.float32, data[0]);
            {
                var reader = new MsgReader(data);
                var x = reader.ReadFloat64();
                AreEqual(float.MaxValue, x);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadFloat32();
                AreEqual(float.MaxValue, x);
            } {
                var reader = new MsgReader(data);
                reader.ReadInt64();
                That(reader.Error, Does.StartWith("MessagePack error - value out of range / expect int64. was: 3.402823"));
            } {
                var reader = new MsgReader(data);
                reader.ReadInt32();
                That(reader.Error, Does.StartWith("MessagePack error - value out of range / expect int32. was: 3.402823"));
            } {
                var reader = new MsgReader(data);
                reader.ReadInt16();
                That(reader.Error, Does.StartWith("MessagePack error - value out of range / expect int16. was: 3.402823"));
            } {
                var reader = new MsgReader(data);
                reader.ReadByte();
                That(reader.Error, Does.StartWith("MessagePack error - value out of range / expect uint8. was: 3.402823"));
            }
        }
    }
}
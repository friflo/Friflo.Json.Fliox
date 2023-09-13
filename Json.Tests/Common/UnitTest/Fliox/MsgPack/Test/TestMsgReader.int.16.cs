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
        public static void Read_uint16_FFFF()
        {
            var data = HexToSpan("cd ff ff"); // 65535 (uint16)
            AreEqual((byte)MsgFormat.uint16, data[0]);
            {
                var reader = new MsgReader(data);
                var x = reader.ReadFloat64();
                AreEqual(ushort.MaxValue, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadFloat32();
                AreEqual(ushort.MaxValue, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadInt64();
                AreEqual(ushort.MaxValue, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadInt32();
                AreEqual(ushort.MaxValue, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                reader.ReadInt16();
                AreEqual("MessagePack error - value out of range / expect int16. was: 65535 uint16(0xCD) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadByte();
                AreEqual("MessagePack error - value out of range / expect uint8. was: 65535 uint16(0xCD) pos: 0 (root)", reader.Error);
            }
        }
        
        [Test]
        public static void Read_int16_7FFF()
        {
            var data = HexToSpan("d1 7f ff"); // 32767 (int16)
            AreEqual((byte)MsgFormat.int16, data[0]);
            {
                var reader = new MsgReader(data);
                var x = reader.ReadFloat64();
                AreEqual(short.MaxValue, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadFloat32();
                AreEqual(short.MaxValue, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadInt64();
                AreEqual(short.MaxValue, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadInt32();
                AreEqual(short.MaxValue, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadInt16();
                AreEqual(short.MaxValue, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                reader.ReadByte();
                AreEqual("MessagePack error - value out of range / expect uint8. was: 32767 int16(0xD1) pos: 0 (root)", reader.Error);
            }
        }
    }
}
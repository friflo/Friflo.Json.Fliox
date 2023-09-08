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
        public static void Read_uint32_FFFF_FFFF()
        {
            var data = HexToSpan("ce ff ff ff ff"); // 4294967295 (uint32)
            AreEqual((byte)MsgFormat.uint32, data[0]);
            {
                var reader = new MsgReader(data);
                var x = reader.ReadFloat64();
                AreEqual(4294967295, x);
                AreEqual(data.Length, reader.Pos);
            } {
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
                AreEqual("MessagePack error - value out of range. was: 4294967295 uint32(0xCE) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadInt16();
                AreEqual("MessagePack error - value out of range. was: 4294967295 uint32(0xCE) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadByte();
                AreEqual("MessagePack error - value out of range. was: 4294967295 uint32(0xCE) pos: 0 (root)", reader.Error);
            }
        }
        
        [Test]
        public static void Read_int32_7FFF_FFFF()
        {
            var data = HexToSpan("d2 7f ff ff ff"); // 2147483647 (int32)
            AreEqual((byte)MsgFormat.int32, data[0]);
            {
                var reader = new MsgReader(data);
                var x = reader.ReadFloat64();
                AreEqual(int.MaxValue, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadFloat32();
                AreEqual(int.MaxValue, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadInt64();
                AreEqual(int.MaxValue, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadInt32();
                AreEqual(int.MaxValue, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                reader.ReadInt16();
                AreEqual("MessagePack error - value out of range. was: 2147483647 int32(0xD2) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadByte();
                AreEqual("MessagePack error - value out of range. was: 2147483647 int32(0xD2) pos: 0 (root)", reader.Error);
            }
        }
    }
}
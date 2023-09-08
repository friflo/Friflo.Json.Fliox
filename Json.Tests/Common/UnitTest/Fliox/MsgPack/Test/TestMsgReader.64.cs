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
        public static void Read_uint64_FFFF_FFFF()
        {
            var data = HexToSpan("cf 00 00 00 00 ff ff ff ff"); // 4294967295 (uint64)
            AreEqual((byte)MsgFormat.uint64, data[0]);
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
                AreEqual("MessagePack error - value out of range. was: 4294967295 uint64(0xCF) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadInt16();
                AreEqual("MessagePack error - value out of range. was: 4294967295 uint64(0xCF) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadByte();
                AreEqual("MessagePack error - value out of range. was: 4294967295 uint64(0xCF) pos: 0 (root)", reader.Error);
            }
        }
        
        [Test]
        public static void Read_int64_FFFF_FFFF()
        {
            var data = HexToSpan("d3 00 00 00 00 ff ff ff ff"); // 4294967295 (int64)
            AreEqual((byte)MsgFormat.int64, data[0]);
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
                AreEqual("MessagePack error - value out of range. was: 4294967295 int64(0xD3) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadInt16();
                AreEqual("MessagePack error - value out of range. was: 4294967295 int64(0xD3) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadByte();
                AreEqual("MessagePack error - value out of range. was: 4294967295 int64(0xD3) pos: 0 (root)", reader.Error);
            }
        }
    }
}
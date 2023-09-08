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
        public static void Read_Eof()
        {
            var data = HexToSpan("");
            {
                var reader = new MsgReader(data);
                reader.ReadFloat64();
                AreEqual("MessagePack error - unexpected EOF. pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadFloat32();
                AreEqual("MessagePack error - unexpected EOF. pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadInt64();
                AreEqual("MessagePack error - unexpected EOF. pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadInt32();
                AreEqual("MessagePack error - unexpected EOF. pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadInt16();
                AreEqual("MessagePack error - unexpected EOF. pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadByte();
                AreEqual("MessagePack error - unexpected EOF. pos: 0 (root)", reader.Error);
            }
        }
        
        [Test]
        public static void Read_Eof_data()
        {
            {
                var data = HexToSpan("cb 7F EF FF FF FF FF FF"); // double.MaxValue (float64)
                AreEqual((byte)MsgFormat.float64, data[0]);
                var reader = new MsgReader(data);
                reader.ReadFloat64();
                AreEqual("MessagePack error - unexpected EOF. type: float64(0xCB) pos: 0 (root)", reader.Error);
            } {
                var data = HexToSpan("ca 7F 7F FF"); // float.MaxValue (float32)
                AreEqual((byte)MsgFormat.float32, data[0]);
                var reader = new MsgReader(data);
                reader.ReadFloat32();
                AreEqual("MessagePack error - unexpected EOF. type: float32(0xCA) pos: 0 (root)", reader.Error);
            } {
                var data = HexToSpan("d3 00 00 00 00 ff ff ff"); // 4294967295 (uint64)
                AreEqual((byte)MsgFormat.int64, data[0]);
                var reader = new MsgReader(data);
                reader.ReadInt64();
                AreEqual("MessagePack error - unexpected EOF. type: int64(0xD3) pos: 0 (root)", reader.Error);
            } {
                var data = HexToSpan("d2 7f ff ff"); // 2147483647 (int32)
                AreEqual((byte)MsgFormat.int32, data[0]);
                var reader = new MsgReader(data);
                reader.ReadInt32();
                AreEqual("MessagePack error - unexpected EOF. type: int32(0xD2) pos: 0 (root)", reader.Error);
            } {
                var data = HexToSpan("d1 7f"); // 32767 (int16)
                AreEqual((byte)MsgFormat.int16, data[0]);
                var reader = new MsgReader(data);
                reader.ReadInt16();
                AreEqual("MessagePack error - unexpected EOF. type: int16(0xD1) pos: 0 (root)", reader.Error);
            } {
                var data = HexToSpan("d0"); // 127 (int8)
                AreEqual((byte)MsgFormat.int8, data[0]);
                var reader = new MsgReader(data);
                reader.ReadByte();
                AreEqual("MessagePack error - unexpected EOF. type: int8(0xD0) pos: 0 (root)", reader.Error);
            }
        }
        
        [Test]
        public static void Read_str()
        {
            var data = HexToSpan("A3 61 62 63"); // "abc" (fixstr)
            {
                var reader = new MsgReader(data);
                reader.ReadFloat64();
                AreEqual("MessagePack error - expect float64 compatible type. was: fixstr(0xA3) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadFloat32();
                AreEqual("MessagePack error - expect float32 compatible type. was: fixstr(0xA3) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadInt64();
                AreEqual("MessagePack error - expect int64 compatible type. was: fixstr(0xA3) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadInt32();
                AreEqual("MessagePack error - expect int32 compatible type. was: fixstr(0xA3) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadInt16();
                AreEqual("MessagePack error - expect int16 compatible type. was: fixstr(0xA3) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadByte();
                AreEqual("MessagePack error - expect uint8 compatible type. was: fixstr(0xA3) pos: 0 (root)", reader.Error);
            }
        }
    }
}
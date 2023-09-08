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